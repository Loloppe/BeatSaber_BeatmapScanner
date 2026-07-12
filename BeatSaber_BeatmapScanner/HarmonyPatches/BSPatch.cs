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
		internal static Parse Parser = new();
		internal static Analyze Analyzer = new();
		internal static readonly List<double> Data = new(9) { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		private static CancellationTokenSource _cts = new CancellationTokenSource();

		static async void Postfix(StandardLevelDetailView __instance)
		{
			if (!Settings.Instance.Enabled) return;

			// Cancel any in-progress analysis and begin a new one for the latest selection
			var newCts = new CancellationTokenSource();
			var oldCts = Interlocked.Exchange(ref _cts, newCts);
			oldCts.Cancel();
			oldCts.Dispose();
			var token = newCts.Token;

			try
			{
				for (int i = 0; i < Data.Count; i++) Data[i] = 0;
				GridViewController.Apply(Data);
				UICreator.SwingDiffGraph?.ClearGraph();
				UICreator.SwingTechGraph?.ClearGraph();
				UICreator.ShowScreens(false);
				var beatmapLevel = __instance._beatmapLevel;
				var beatmapKey = __instance.beatmapKey;
				if (SongDetailsUtil.songDetails != null && beatmapKey.levelId.Contains("custom"))
				{
					UICreator.ShowScreens(true);
					var characteristic = beatmapKey.beatmapCharacteristic.serializedName;
					var difficultyName = beatmapKey.difficulty.ToString();
					var hash = BeatmapsUtil.GetHashOfLevel(beatmapLevel);
					var info = beatmapLevel.GetDifficultyBeatmapData(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty);
					var beatmapData = await __instance._beatmapLevelsModel.LoadBeatmapLevelDataAsync(beatmapLevel.levelID, BeatmapLevelDataVersion.Original, token);
					token.ThrowIfCancellationRequested();
					// BeatLeader-Parser
					var result = SongCore.Loader.CustomLevelLoader._loadedBeatmapSaveData.TryGetValue(beatmapKey.levelId, out var value);
                    if (!result)
					{
						Plugin.Log.Error("Error during LoadedSaveData fetch");
                        UICreator.ShowScreens(false);
                        return;
					}
					var infoData = value.customLevelFolderInfo.levelInfoJsonString;
					var lightData = await beatmapData.beatmapLevelData.GetLightshowStringAsync(beatmapKey);
					token.ThrowIfCancellationRequested();
					var beatData = await beatmapData.beatmapLevelData.GetBeatmapStringAsync(beatmapKey);
					token.ThrowIfCancellationRequested();
					var audio = await beatmapData.beatmapLevelData.GetAudioDataStringAsync();
					token.ThrowIfCancellationRequested();
					var singleDiff = await Task.Run(() => Parser.TryLoadDifficulty(infoData, beatData, audio, lightData, beatmapLevel.beatsPerMinute, info.noteJumpMovementSpeed, characteristic, difficultyName).Difficulties[0].Data, token);
					if (singleDiff == null)
					{
						Plugin.Log.Error("Error during Parser data load");
                        UICreator.ShowScreens(false);
                        return;
					}
					token.ThrowIfCancellationRequested();
					// V3
					if (singleDiff.Arcs.Count > 0 || singleDiff.Chains.Count > 0) Data[3] = 1;
					if (singleDiff.Notes.Count > 0 && beatmapLevel.beatsPerMinute > 0)
					{
						if (info.noteJumpMovementSpeed != 0)
						{
							float timescale;
							switch (__instance._playerData.gameplayModifiers.songSpeed)
							{
								case GameplayModifiers.SongSpeed.SuperFast:
									timescale = 1.5f;
									break;
								case GameplayModifiers.SongSpeed.Faster:
									timescale = 1.2f;
									break;
								case GameplayModifiers.SongSpeed.Slower:
									timescale = 0.85f;
									break;
								default:
									timescale = 1f;
									break;
							}
							// BeatLeader-Analyzer pass and tech rating
							Ratings ratings = await Task.Run(() => Analyzer.GetRating(singleDiff, characteristic, difficultyName, beatmapLevel.beatsPerMinute, timescale), token);
							token.ThrowIfCancellationRequested();
							if (ratings != null)
							{
								Data[0] = ratings.CrouchWalls.Count();
								Data[1] = ratings.Statistics.BombAvoidances;
								Data[2] = ratings.LinearPercentage;
								Data[4] = ratings.PeakSustainedEBPM;
								Data[6] = ratings.PassRating;
								Data[7] = ratings.TechRating;
								// Swing graphs
								if (ratings.SwingData != null && ratings.SwingData.Count > 0)
								{
									var times = ratings.SwingData.Select(s => (double)s.Cubes[0].Seconds).ToList();
									var diffs = ratings.SwingData.Select(s => s.SwingDiff).ToList();
									var techs = ratings.SwingData.Select(s => s.SwingTech * 10).ToList();
									UICreator.SwingDiffGraph?.SetData(times, diffs);
									UICreator.SwingTechGraph?.SetData(times, techs);
								}
							}
							else
							{
                                Plugin.Log.Error("Error during Analyzer Ratings fetch");
                                UICreator.ShowScreens(false);
                                return;
                            }
						}
						// SS and BL star rating
						var uploaded = SongDetailsUtil.songDetails.instance.songs.FindByHash(hash, out var song);
						if (uploaded)
						{
							song.GetDifficulty(out var difficulty, (MapDifficulty)beatmapKey.difficulty, characteristic);
							Data[8] = Math.Round(difficulty.stars, 2);
							Data[5] = Math.Round(difficulty.starsBeatleader, 2);
						}
					}
					GridViewController.Apply(Data);
				}
				else if (!SongDetailsUtil.FinishedInitAttempt)
					{
						await SongDetailsUtil.TryGet().ContinueWith(
							x => { Postfix(__instance); }, // Retry after initialization
                            CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext()
						);
					}
			}
			catch (OperationCanceledException)
			{
				// A newer difficulty was selected; silently discard this result
			}
			catch (Exception ex)
			{
				Plugin.Log.Error($"Error during analysis: {ex.Message}");
                UICreator.ShowScreens(false);
            }
		}
	}
}
