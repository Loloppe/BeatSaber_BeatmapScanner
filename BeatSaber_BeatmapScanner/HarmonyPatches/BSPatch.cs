using System.Collections.Generic;
using System.Threading.Tasks;
using BeatmapScanner.Utils;
using BeatmapScanner.UI;
using System.Threading;
using System.Net.Http;
using Newtonsoft.Json;
using System.Linq;
using HarmonyLib;
using System;
using SongDetailsCache.Structs;
using beatleader_analyzer;
using beatleader_parser;
using ModestTree;
using beatleader_analyzer.BeatmapScanner.Data;

namespace BeatmapScanner.HarmonyPatches
{
	[HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
	public static class BSPatch
	{
        internal static Parse parser = new();
        internal static Analyze analyzer = new();
        internal static bool start = true;

        static async void Postfix(StandardLevelDetailView __instance)
		{
			if (Settings.Instance.Enabled)
			{
                GridViewController.ResetValues();
				var beatmapLevel = __instance._beatmapLevel;
				var beatmapKey = __instance.beatmapKey;
				var hasRequirement = SongCore.Collections.RetrieveDifficultyData(beatmapLevel, beatmapKey)?
					.additionalDifficultyData?
					._requirements?.Any(x => x == "Noodle Extensions" || x == "Mapping Extensions") == true;
                if (!hasRequirement && SongDetailsUtil.songDetails != null && beatmapKey.levelId.Contains("custom"))
				{
                    var characteristic = (MapCharacteristic)BeatmapsUtil.GetCharacteristicFromDifficulty(beatmapKey);
                    var hash = BeatmapsUtil.GetHashOfLevel(__instance._beatmapLevel);
                    SongDetailsUtil.songDetails.instance.songs.FindByHash(hash, out var song);
                    song.GetDifficulty(out var difficulty, (MapDifficulty)beatmapKey.difficulty, characteristic);
					var beatmapData = await __instance._beatmapLevelsModel.LoadBeatmapLevelDataAsync(beatmapLevel.levelID, BeatmapLevelDataVersion.Original, new()).ConfigureAwait(false);
                    var beatmap = await __instance._beatmapDataLoader.LoadBeatmapDataAsync(beatmapData.beatmapLevelData, beatmapKey, song.bpm, false, null, BeatmapLevelDataVersion.Original, __instance._playerData.gameplayModifiers, __instance._playerData.playerSpecificSettings, false).ConfigureAwait(false);
                    var colorNotes = beatmap.GetBeatmapDataItems<NoteData>(0).ToList();
					var sliders = beatmap.GetBeatmapDataItems<SliderData>(0).ToList();
                    var obstacles = beatmap.GetBeatmapDataItems<ObstacleData>(0).ToList();
                    if (difficulty.notes > 0 && song.bpm > 0)
                    {
                        // EBPM
                        double ebpm = 0;
                        var red = colorNotes.Where(c => c.colorType == ColorType.ColorA).ToList();
                        if (red.Count() > 0)
                        {
                            ebpm = EBPM.GetEBPM(red, song.bpm);
                        }
                        var blue = colorNotes.Where(c => c.colorType == ColorType.ColorB).ToList();
                        if (blue.Count() > 0)
                        {
                            ebpm = Math.Max(EBPM.GetEBPM(blue, song.bpm), ebpm);
                        }
                        GridViewController.values[1] = ebpm;
                        // BeatLeader-Analyzer pass and tech rating
                        async Task wrapperAsync()
                        {
                            var ratings = await GetAsyncRating(beatmapKey, beatmapLevel, characteristic.ToString(), song.bpm).ConfigureAwait(false);
                            if (ratings != null)
                            {
                                GridViewController.values[3] = ratings.FirstOrDefault().Pass;
                                GridViewController.values[4] = ratings.FirstOrDefault().Tech * 10;
                            }
                        }
                        await wrapperAsync().ConfigureAwait(false);
                        // V3
                        if (sliders.Count == 0) GridViewController.values[0] = 1;
                        // SS and BL star rating
                        async Task wrapperAsync2()
                        {
                            GridViewController.values[2] = await GetAsyncData(hash, difficulty.difficulty.ToString(), characteristic).ConfigureAwait(false);
                            if (characteristic == MapCharacteristic.Standard)
                            {
                                if (song.rankedStates == RankedStates.ScoresaberRanked)
                                {
                                    GridViewController.values[5] = Math.Round(difficulty.stars, 2);
                                }
                            }
                        }
                        await wrapperAsync2().ConfigureAwait(false);
                    }
                }
                else if (!SongDetailsUtil.FinishedInitAttempt)
                {
                    await SongDetailsUtil.TryGet().ContinueWith(
                        x => { if (x.Result != null) GridViewController.Apply(); },
                        CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext()
                    ).ConfigureAwait(false);
                }

                GridViewController.Apply();
			}
		}

        public static async Task<List<Ratings>> GetAsyncRating(BeatmapKey beatmapKey, BeatmapLevel beatmapLevel, string characteristic, float bpm)
        {
            var folderPath = SongCore.Collections.GetLoadedSaveData(beatmapKey.levelId)?.customLevelFolderInfo.folderPath;
            if (folderPath != null)
            {
                if (start)
                {
                    Log.Warn("BeatmapScanner: You can ignore those errors.");
                    start = false;
                }
                var singleDiff = Task.Run(() => parser.TryLoadPath(folderPath, characteristic, beatmapKey.difficulty.ToString())).Result;
                if (singleDiff != null)
                {
                    var njs = beatmapLevel.GetDifficultyBeatmapData(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty)?.noteJumpMovementSpeed ?? 0;
                    if (njs != 0)
                    {
                        return Task.Run(() => analyzer.GetRating(singleDiff.Difficulty.Data, characteristic, beatmapKey.difficulty.ToString(), bpm, njs)).Result;
                    }
                }
            }

            return null;
        }

        public static async Task<double> GetAsyncData(string hash, string difficulty, MapCharacteristic characteristic)
		{
            string api = "https://api.beatleader.xyz/map/modinterface/" + hash;
            using HttpClient client = new();
            using HttpResponseMessage res = await client.GetAsync(api).ConfigureAwait(false);
            using HttpContent content = res.Content;
            var data = await content.ReadAsStringAsync().ConfigureAwait(false);
            if (data != null)
            {
                List<BLStruct> obj = JsonConvert.DeserializeObject<List<BLStruct>>(data);
                var star = obj.FirstOrDefault(o => o.difficultyName == difficulty && o.modeName == characteristic.ToString() && o.stars != null);
                if (star != null) return (double)star.stars;
            }

            return 0;
        }
	}

	[HarmonyPatch(typeof(SelectLevelCategoryViewController), nameof(SelectLevelCategoryViewController.LevelFilterCategoryIconSegmentedControlDidSelectCell))]
	public static class BSPatch2
	{
		static void Prefix()
		{
            UICreator._floatingScreen?.gameObject.SetActive(false);
		}
	}
}
