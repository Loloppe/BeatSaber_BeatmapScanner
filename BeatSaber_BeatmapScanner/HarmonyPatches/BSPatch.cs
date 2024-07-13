#region Import

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

#endregion

namespace BeatmapScanner.HarmonyPatches
{
	[HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
	public static class BSPatch
	{
		#region Output

		public static double Pass { get; set; } = 0;
		public static double Tech { get; set; } = 0;
		public static double Linear { get; set; } = 0;
		public static double Pattern { get; set; } = 0;
        public static double Crouch { get; set; } = 0;
        public static double EBPM { get; set; } = 0;
        public static double SS { get; set; } = 0;
        public static double BL { get; set; } = 0;
        public static double V3 { get; set; } = 0;

        #endregion

        static async void Postfix(StandardLevelDetailView __instance)
		{
			if (Settings.Instance.Enabled)
			{
				ResetValues();
				var beatmapLevel = __instance._beatmapLevel;
				var beatmapKey = __instance.beatmapKey;
				var hasRequirement = SongCore.Collections.RetrieveDifficultyData(beatmapLevel, beatmapKey)?
					.additionalDifficultyData?
					._requirements?.Any(x => x == "Noodle Extensions" || x == "Mapping Extensions") == true;
                if (!hasRequirement && SongDetailsUtil.songDetails != null && beatmapKey.levelId.Contains("custom"))
				{
                    var characteristic = (SongDetailsCache.Structs.MapCharacteristic)BeatmapsUtil.GetCharacteristicFromDifficulty(beatmapKey);
                    var hash = BeatmapsUtil.GetHashOfLevel(__instance._beatmapLevel);
                    SongDetailsUtil.songDetails.instance.songs.FindByHash(hash, out var song);
                    song.GetDifficulty(out var difficulty, (SongDetailsCache.Structs.MapDifficulty)beatmapKey.difficulty, characteristic);
					var beatmapData = await __instance._beatmapLevelsModel.LoadBeatmapLevelDataAsync(beatmapLevel.levelID, BeatmapLevelDataVersion.Original, new());
                    var beatmap = await __instance._beatmapDataLoader.LoadBeatmapDataAsync(beatmapData.beatmapLevelData, beatmapKey, song.bpm, false, null, BeatmapLevelDataVersion.Original, __instance._playerData.gameplayModifiers, __instance._playerData.playerSpecificSettings, false);
                    var colorNotes = beatmap.GetBeatmapDataItems<NoteData>(0).ToList();
					var sliders = beatmap.GetBeatmapDataItems<SliderData>(0).ToList();
                    var obstacles = beatmap.GetBeatmapDataItems<ObstacleData>(0).ToList();
                    if (difficulty.notes > 0 && song.bpm > 0)
                    {
                        var basicData = beatmapLevel.GetDifficultyBeatmapData(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty);
                        var njs = basicData?.noteJumpMovementSpeed ?? 0;
                        var data = Algorithm.BeatmapScanner.Analyzer(colorNotes, sliders, obstacles, song.bpm, njs);
                        Pass = data[0];
                        Tech = data[1] * 10;
                        Linear = data[3];
                        Pattern = data[4];
                        Crouch = data[5];
                        EBPM = data[6];
                        if (sliders.Count > 0) V3 = 1;
                        async Task wrapperAsync()
                        {
                            BL = Math.Round(await GetAsyncData(hash, difficulty.difficulty.ToString()), 2);
                            if (characteristic == SongDetailsCache.Structs.MapCharacteristic.Standard)
                            {
                                if (song.rankedStates == SongDetailsCache.Structs.RankedStates.ScoresaberRanked)
                                {
                                    SS = Math.Round(difficulty.stars, 2);
                                }
                            }
                        }
                        await wrapperAsync();
                    }
                }
                else
                {
                    if (!SongDetailsUtil.FinishedInitAttempt)
                    {
                        await SongDetailsUtil.TryGet().ContinueWith(
                            x => { if (x.Result != null) GridViewController.Apply(); },
                            CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext()
                        );
                    }
                }

                GridViewController.Apply();
			}
		}

		public static void ResetValues()
		{
			Pass = 0;
			Tech = 0;
			Linear = 0;
			Pattern = 0;
			Crouch = 0;
			EBPM = 0;
			SS = 0;
			BL = 0;
			V3 = 0;

			if (UICreator._floatingScreen != null)
			{
				UICreator._floatingScreen.gameObject.SetActive(true);
				GridViewController.Apply();
			}
		}

		public static async Task<double> GetAsyncData(string hash, string difficulty)
		{
			try
			{
				string api = "https://api.beatleader.xyz/map/modinterface/" + hash;

				using HttpClient client = new();
				using HttpResponseMessage res = await client.GetAsync(api);
				using HttpContent content = res.Content;
				var data = await content.ReadAsStringAsync();
				if (data != null)
				{
					List<BLStruct> obj = JsonConvert.DeserializeObject<List<BLStruct>>(data);
					foreach (var o in obj)
					{
						if (o.difficultyName == difficulty && o.modeName == "Standard")
						{
							if (o.stars != null)
							{
								return (double)o.stars;
							}
							else
							{
								return 0;
							}
						}
					}

					return 0;
				}
				else
				{
					return 0;
				}
			}
			catch
			{
				return 0;
			}
		}
	}

	[HarmonyPatch(typeof(SelectLevelCategoryViewController), nameof(SelectLevelCategoryViewController.LevelFilterCategoryIconSegmentedControlDidSelectCell))]
	public static class BSPatch2
	{
		static void Prefix()
		{
			if(UICreator._floatingScreen != null)
            {
				UICreator._floatingScreen.gameObject.SetActive(false);
			}
		}
	}
}
