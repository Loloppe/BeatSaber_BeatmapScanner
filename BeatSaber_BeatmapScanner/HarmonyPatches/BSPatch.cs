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
        internal static List<double> Data;
        internal static bool Processing = false;

        static async void Postfix(StandardLevelDetailView __instance)
		{
            if (Settings.Instance.Enabled && Processing == false)
			{
                try
                {
                    Processing = true;
                    Data = [0, 0, 0, 0, 0, 0, 0, 0, 0];
                    var beatmapLevel = __instance._beatmapLevel;
                    var beatmapKey = __instance.beatmapKey;
                    if (SongDetailsUtil.songDetails != null && beatmapKey.levelId.Contains("custom"))
                    {
                        var characteristic = beatmapKey.beatmapCharacteristic.serializedName;
                        var hash = BeatmapsUtil.GetHashOfLevel(beatmapLevel);
                        var info = beatmapLevel.GetDifficultyBeatmapData(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty);
                        var beatmapData = await __instance._beatmapLevelsModel.LoadBeatmapLevelDataAsync(beatmapLevel.levelID, BeatmapLevelDataVersion.Original, new());
                        // BeatLeader-Parser
                        var result = SongCore.Loader.CustomLevelLoader._loadedBeatmapSaveData.TryGetValue(beatmapKey.levelId, out var value);
                        if (!result)
                        {
                            Plugin.Log.Error("Error during LoadedSaveData fetch");
                            return;
                        }
                        var infoData = value.customLevelFolderInfo.levelInfoJsonString;
                        var lightData = await beatmapData.beatmapLevelData.GetLightshowStringAsync(beatmapKey);
                        var beatData = await beatmapData.beatmapLevelData.GetBeatmapStringAsync(beatmapKey);
                        var audio = await beatmapData.beatmapLevelData.GetAudioDataStringAsync();
                        var singleDiff = await Task.Run(() => Parser.TryLoadDifficulty(infoData, beatData, audio, lightData, beatmapLevel.beatsPerMinute, info.noteJumpMovementSpeed));
                        if (singleDiff == null)
                        {
                            Plugin.Log.Error("Error during Parser data load");
                            return;
                        }
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

                                // EBPM
                                double ebpm = 0;
                                var red = singleDiff.Notes.Where(c => c.Color == (int)ColorType.ColorA).ToList();
                                if (red.Count() > 0)
                                {
                                    ebpm = EBPM.GetEBPM(red, beatmapLevel.beatsPerMinute, info.noteJumpMovementSpeed, false) * timescale;
                                }
                                var blue = singleDiff.Notes.Where(c => c.Color == (int)ColorType.ColorB).ToList();
                                if (blue.Count() > 0)
                                {
                                    ebpm = Math.Max(EBPM.GetEBPM(blue, beatmapLevel.beatsPerMinute, info.noteJumpMovementSpeed, true) * timescale, ebpm);
                                }
                                Data[4] = Math.Round(ebpm);
                                // BeatLeader-Analyzer pass and tech rating
                                List<Ratings> ratings = await Task.Run(() => Analyzer.GetRating(singleDiff, characteristic, beatmapKey.difficulty.ToString(), beatmapLevel.beatsPerMinute, timescale));
                                if (singleDiff.Walls?.Count > 0)
                                {
                                    Data[0] = EBPM.DetectCrouchWalls(singleDiff.Walls);
                                }
                                if (ratings != null)
                                {
                                    Data[6] = ratings.FirstOrDefault().Pass;
                                    Data[7] = ratings.FirstOrDefault().Tech * 10;
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
                            x => { },
                            CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext()
                        );
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error("Error during analysis: " + ex.Message);
                }
                finally
                {
                    Processing = false;
                }
            }
		}
	}
}
