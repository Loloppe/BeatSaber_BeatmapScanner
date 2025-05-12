using System.Collections.Generic;
using System.Threading.Tasks;
using BeatmapScanner.Utils;
using BeatmapScanner.UI;
using System.Linq;
using HarmonyLib;
using System;
using SongDetailsCache.Structs;
using beatleader_analyzer;
using beatleader_parser;
using beatleader_analyzer.BeatmapScanner.Data;
using System.Threading;

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
                        var beatmap = await __instance._beatmapDataLoader.LoadBeatmapDataAsync(beatmapData.beatmapLevelData, beatmapKey, beatmapLevel.beatsPerMinute, false, null, null, BeatmapLevelDataVersion.Original, __instance._playerData.gameplayModifiers, __instance._playerData.playerSpecificSettings, false);
                        var colorNotes = beatmap.GetBeatmapDataItems<NoteData>(0).ToList();
                        var sliders = beatmap.GetBeatmapDataItems<SliderData>(0).ToList();
                        // V3
                        if (sliders.Count != 0) Data[3] = 1;
                        if (colorNotes.Count > 0 && beatmapLevel.beatsPerMinute > 0)
                        {
                            if (info.noteJumpMovementSpeed != 0)
                            {
                                // EBPM
                                double ebpm = 0;
                                var red = colorNotes.Where(c => c.colorType == ColorType.ColorA).ToList();
                                if (red.Count() > 0)
                                {
                                    ebpm = EBPM.GetEBPM(red, beatmapLevel.beatsPerMinute, info.noteJumpMovementSpeed, false);
                                }
                                var blue = colorNotes.Where(c => c.colorType == ColorType.ColorB).ToList();
                                if (blue.Count() > 0)
                                {
                                    ebpm = Math.Max(EBPM.GetEBPM(blue, beatmapLevel.beatsPerMinute, info.noteJumpMovementSpeed, true), ebpm);
                                }
                                Data[4] = Math.Round(ebpm);
                                // BeatLeader-Analyzer pass and tech rating
                                List<Ratings> ratings = null;
                                var result = SongCore.Loader.CustomLevelLoader._loadedBeatmapSaveData.TryGetValue(beatmapKey.levelId, out var value);
                                if (result)
                                {
                                    var folderPath = value.customLevelFolderInfo.folderPath;
                                    if (folderPath != null)
                                    {
                                        var singleDiff = await Task.Run(() => Parser.TryLoadPath(folderPath, characteristic, beatmapKey.difficulty.ToString()));
                                        if (singleDiff != null)
                                        {
                                            ratings = await Task.Run(() => Analyzer.GetRating(singleDiff.Difficulty.Data, characteristic, beatmapKey.difficulty.ToString(), beatmapLevel.beatsPerMinute, info.noteJumpMovementSpeed));
                                            if (singleDiff.Difficulty.Data?.Walls?.Count > 0)
                                            {
                                                Data[0] = EBPM.DetectCrouchWalls(singleDiff.Difficulty.Data.Walls);
                                            }
                                        }
                                    }
                                    if (ratings != null)
                                    {
                                        Data[6] = ratings.FirstOrDefault().Pass;
                                        Data[7] = ratings.FirstOrDefault().Tech * 10;
                                    }
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
                finally
                {
                    Processing = false;
                }
            }
		}
	}
}
