using beatleader_analyzer;
using beatleader_analyzer.BeatmapScanner.Data;
using beatleader_parser;
using BeatmapScanner.UI;
using BeatmapScanner.Utils;
using HarmonyLib;
using SongDetailsCache.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeatmapScanner.HarmonyPatches
{
	[HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
	public static class BSPatch
	{
		// Wait this long before analyzing a newly selected map, so scrolling through maps only analyzes the last one
		private const int AnalysisDelayMs = 150;

		private static readonly Parse Parser = new();
		private static readonly Analyze Analyzer = new();
		// Indexed by GridViewController tile order
		private static readonly double[] Data = new double[GridViewController.TileCount];

		// Cancelled whenever a difficulty is selected: stops waiting on and displaying the previous selection
		private static CancellationTokenSource _selectionCts = new();
		// Cancelled when a different map or song speed is selected: stops analyzing the previous map
		private static CancellationTokenSource _mapCts = new();
		private static string _mapSessionId;
		private static bool _restOfMapStarted;
		// Analyses of the current map, by cache key, so the selection and the background pass never analyze a difficulty twice.
		// Only accessed from the Unity main thread.
		private static readonly Dictionary<string, Task<AnalysisResult>> MapAnalyses = new();

		/// <summary>Everything needed to analyze any difficulty of the selected map.</summary>
		private sealed class MapRequest
		{
			public StandardLevelDetailView View;
			public BeatmapLevel Level;
			public GameplayModifiers.SongSpeed SongSpeed;
			public string Hash;
			public bool Cacheable;
		}

		static async void Postfix(StandardLevelDetailView __instance)
		{
			if (!Settings.Instance.Enabled) return;

			// Cancel waiting on the previous selection and begin a new one
			var newCts = new CancellationTokenSource();
			var oldCts = Interlocked.Exchange(ref _selectionCts, newCts);
			oldCts.Cancel();
			oldCts.Dispose();
			var token = newCts.Token;

			try
			{
				Array.Clear(Data, 0, Data.Length);
				GridViewController.Apply(Data);
				UICreator.SwingDiffGraph?.ClearGraph();
				UICreator.SwingTechGraph?.ClearGraph();
				var beatmapLevel = __instance._beatmapLevel;
				var beatmapKey = __instance.beatmapKey;
				if (SongDetailsUtil.songDetails != null && beatmapKey.levelId.Contains("custom"))
				{
					var hash = BeatmapsUtil.GetHashOfLevel(beatmapLevel);
					var map = new MapRequest
					{
						View = __instance,
						Level = beatmapLevel,
						SongSpeed = __instance._playerData.gameplayModifiers.songSpeed,
						Hash = hash,
						Cacheable = AnalysisCache.IsCacheable(hash),
					};
					bool newMap = StartMapSession($"{beatmapLevel.levelID}|{map.SongSpeed}");

					var result = await GetResultAsync(map, beatmapKey, newMap, token);
					token.ThrowIfCancellationRequested();
					if (result == null) return; // Error already logged

					Array.Copy(result.Values, Data, Data.Length);
					UICreator.SwingDiffGraph?.SetData(result.Times, result.Diffs);
					UICreator.SwingTechGraph?.SetData(result.Times, result.Techs);
					// SS and BL star rating, always read live since they change when maps get (un)ranked
					if (hash != null && SongDetailsUtil.songDetails.instance.songs.FindByHash(hash, out var song))
					{
						song.GetDifficulty(out var difficulty, (MapDifficulty)beatmapKey.difficulty, beatmapKey.beatmapCharacteristic.serializedName);
						Data[GridViewController.SS] = Math.Round(difficulty.stars, 2);
						Data[GridViewController.BL] = Math.Round(difficulty.starsBeatleader, 2);
					}
					GridViewController.Apply(Data);
					UICreator.ShowScreens(true);

					if (map.Cacheable && !_restOfMapStarted)
					{
						_restOfMapStarted = true;
						AnalyzeRestOfMap(map, beatmapKey, _mapCts.Token);
					}
				}
				else if (!SongDetailsUtil.FinishedInitAttempt)
				{
					// Retry once SongDetailsCache has initialized (resumes on the Unity main thread)
					await SongDetailsUtil.TryGet();
					Postfix(__instance);
				}
				else // Not custom
				{
					StartMapSession(null);
					UICreator.ShowScreens(false);
				}
			}
			catch (OperationCanceledException)
			{
				// A newer difficulty was selected; silently discard this result
			}
			catch (Exception ex)
			{
				Plugin.Log.Error($"Error during analysis: {ex}");
			}
		}

		/// <summary>Switches to a new map session if the map or song speed changed, cancelling the previous map's analyses. Returns true if it switched.</summary>
		private static bool StartMapSession(string sessionId)
		{
			if (sessionId == _mapSessionId) return false;
			_mapSessionId = sessionId;
			_mapCts.Cancel();
			_mapCts.Dispose();
			_mapCts = new CancellationTokenSource();
			MapAnalyses.Clear();
			_restOfMapStarted = false;
			return true;
		}

		private static async Task<AnalysisResult> GetResultAsync(MapRequest map, BeatmapKey beatmapKey, bool newMap, CancellationToken token)
		{
			var cacheKey = MakeCacheKey(beatmapKey, map.SongSpeed);
			var cached = map.Cacheable ? await AnalysisCache.TryGetAsync(map.Hash, cacheKey) : null;
			token.ThrowIfCancellationRequested();
			if (cached != null && cached.Values.Length == GridViewController.TileCount) return cached;

			if (newMap) await Task.Delay(AnalysisDelayMs, token);
			// Stop waiting if another difficulty is selected, but let the analysis finish so it still gets cached
			var analysis = GetOrStartAnalysis(map, beatmapKey);
			await Task.WhenAny(analysis, Task.Delay(Timeout.Infinite, token));
			token.ThrowIfCancellationRequested();
			return await analysis;
		}

		/// <summary>Analyzes the map's other difficulties one at a time in the background, starting with the selected characteristic.</summary>
		private static async void AnalyzeRestOfMap(MapRequest map, BeatmapKey selected, CancellationToken mapToken)
		{
			var keys = map.Level.GetCharacteristics()
				.OrderBy(c => c == selected.beatmapCharacteristic ? 0 : 1)
				.SelectMany(c => map.Level.GetDifficulties(c).Select(d => new BeatmapKey(map.Level.levelID, c, d)))
				.Where(k => k.beatmapCharacteristic != selected.beatmapCharacteristic || k.difficulty != selected.difficulty)
				.ToList();

			foreach (var key in keys)
			{
				try
				{
					// Don't compete with gameplay for CPU; the next selection resumes the pass
					if (!map.View.isActiveAndEnabled)
					{
						if (!mapToken.IsCancellationRequested) _restOfMapStarted = false;
						return;
					}
					if (await AnalysisCache.TryGetAsync(map.Hash, MakeCacheKey(key, map.SongSpeed)) != null) continue;
					mapToken.ThrowIfCancellationRequested();
					await GetOrStartAnalysis(map, key);
				}
				catch (OperationCanceledException)
				{
					return; // Another map was selected
				}
				catch (Exception ex)
				{
					Plugin.Log.Error($"Error during background analysis of {key.beatmapCharacteristic.serializedName} {key.difficulty}: {ex}");
				}
			}
		}

		/// <summary>Returns the running (or finished) analysis of this difficulty in the current map session, starting it if needed.</summary>
		private static Task<AnalysisResult> GetOrStartAnalysis(MapRequest map, BeatmapKey beatmapKey)
		{
			var cacheKey = MakeCacheKey(beatmapKey, map.SongSpeed);
			if (MapAnalyses.TryGetValue(cacheKey, out var analysis) && !analysis.IsFaulted && !analysis.IsCanceled) return analysis;
			analysis = AnalyzeAndCacheAsync(map, beatmapKey, cacheKey, _mapCts.Token);
			MapAnalyses[cacheKey] = analysis;
			return analysis;
		}

		private static async Task<AnalysisResult> AnalyzeAndCacheAsync(MapRequest map, BeatmapKey beatmapKey, string cacheKey, CancellationToken token)
		{
			var result = await AnalyzeAsync(map, beatmapKey, token);
			if (result != null && map.Cacheable) AnalysisCache.Save(map.Hash, cacheKey, result);
			return result;
		}

		private static string MakeCacheKey(BeatmapKey beatmapKey, GameplayModifiers.SongSpeed songSpeed) =>
			AnalysisCache.MakeKey(beatmapKey.beatmapCharacteristic.serializedName, beatmapKey.difficulty.ToString(), songSpeed);

		/// <summary>Loads, parses and analyzes the difficulty. Returns null (after logging) if any step fails.</summary>
		private static async Task<AnalysisResult> AnalyzeAsync(MapRequest map, BeatmapKey beatmapKey, CancellationToken token)
		{
			var beatmapLevel = map.Level;
			var characteristic = beatmapKey.beatmapCharacteristic.serializedName;
			var difficultyName = beatmapKey.difficulty.ToString();
			var info = beatmapLevel.GetDifficultyBeatmapData(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty);
			var beatmapData = await map.View._beatmapLevelsModel.LoadBeatmapLevelDataAsync(beatmapLevel.levelID, BeatmapLevelDataVersion.Original, token);
			token.ThrowIfCancellationRequested();
			// BeatLeader-Parser
			if (!SongCore.Loader.CustomLevelLoader._loadedBeatmapSaveData.TryGetValue(beatmapKey.levelId, out var saveData))
			{
				Plugin.Log.Error("Error during LoadedSaveData fetch");
				return null;
			}
			var infoData = saveData.customLevelFolderInfo.levelInfoJsonString;
			var lightData = await beatmapData.beatmapLevelData.GetLightshowStringAsync(beatmapKey);
			token.ThrowIfCancellationRequested();
			var beatData = await beatmapData.beatmapLevelData.GetBeatmapStringAsync(beatmapKey);
			token.ThrowIfCancellationRequested();
			var audio = await beatmapData.beatmapLevelData.GetAudioDataStringAsync();
			token.ThrowIfCancellationRequested();
			var singleDiff = await Task.Run(() => Parser.TryLoadDifficulty(infoData, beatData, audio, lightData, beatmapLevel.beatsPerMinute, info.noteJumpMovementSpeed, characteristic, difficultyName).Difficulties[0].Data, token);
			token.ThrowIfCancellationRequested();
			if (singleDiff == null)
			{
				Plugin.Log.Error($"Error during Parser data load ({characteristic} {difficultyName})");
				return null;
			}

			var result = new AnalysisResult
			{
				Values = new double[GridViewController.TileCount],
				Times = [],
				Diffs = [],
				Techs = [],
			};
			if (singleDiff.Arcs.Count > 0 || singleDiff.Chains.Count > 0) result.Values[GridViewController.V3] = 1;
			if (singleDiff.Notes.Count == 0 || beatmapLevel.beatsPerMinute <= 0 || info.noteJumpMovementSpeed == 0) return result;

			// BeatLeader-Analyzer pass and tech rating
			float timescale = GetTimescale(map.SongSpeed);
			Ratings ratings = await Task.Run(() => Analyzer.GetRating(singleDiff, characteristic, difficultyName, beatmapLevel.beatsPerMinute, timescale), token);
			token.ThrowIfCancellationRequested();
			if (ratings == null)
			{
				Plugin.Log.Error($"Error during Analyzer Ratings fetch ({characteristic} {difficultyName})");
				return null;
			}
			result.Values[GridViewController.Crouch] = ratings.CrouchWalls.Count();
			result.Values[GridViewController.Bomb] = ratings.Statistics.BombAvoidances;
			result.Values[GridViewController.Linear] = ratings.LinearPercentage;
			result.Values[GridViewController.EBPM] = ratings.PeakSustainedEBPM;
			result.Values[GridViewController.Pass] = ratings.PassRating;
			result.Values[GridViewController.Tech] = ratings.TechRating;

			// Swing graphs
			var swings = ratings.SwingData;
			if (swings != null && swings.Count > 0)
			{
				result.Times = new float[swings.Count];
				result.Diffs = new float[swings.Count];
				result.Techs = new float[swings.Count];
				for (int i = 0; i < swings.Count; i++)
				{
					result.Times[i] = (float)swings[i].Cubes[0].Seconds;
					result.Diffs[i] = (float)swings[i].SwingDiff;
					result.Techs[i] = (float)(swings[i].SwingTech * 10);
				}
			}
			return result;
		}

		private static float GetTimescale(GameplayModifiers.SongSpeed songSpeed) => songSpeed switch
		{
			GameplayModifiers.SongSpeed.SuperFast => 1.5f,
			GameplayModifiers.SongSpeed.Faster => 1.2f,
			GameplayModifiers.SongSpeed.Slower => 0.85f,
			_ => 1f,
		};
	}
}
