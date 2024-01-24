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

        static async void Postfix(IDifficultyBeatmap ____selectedDifficultyBeatmap, IBeatmapLevel ____level)
		{
			if (Settings.Instance.Enabled)
			{
				ResetValues();

				var hasRequirement = SongCore.Collections.RetrieveDifficultyData(____selectedDifficultyBeatmap)?
					.additionalDifficultyData?
					._requirements?.Any(x => x == "Noodle Extensions" || x == "Mapping Extensions") == true;

				if (!hasRequirement && ____selectedDifficultyBeatmap is CustomDifficultyBeatmap beatmap && beatmap.beatmapSaveData.colorNotes.Count > 0 && beatmap.level.beatsPerMinute > 0)
				{
                    var data = Algorithm.BeatmapScanner.Analyzer(beatmap.beatmapSaveData.colorNotes, beatmap.beatmapSaveData.burstSliders, beatmap.beatmapSaveData.bombNotes, beatmap.beatmapSaveData.obstacles, beatmap.level.beatsPerMinute, ____selectedDifficultyBeatmap.noteJumpMovementSpeed);
					Pass = data[0];
					Tech = data[1] * 10;
					Linear = data[3];
                    Pattern = data[4];
					Crouch = data[5];
                    EBPM = data[6];

					if(beatmap.beatmapSaveData.burstSliders.Count > 0 || beatmap.beatmapSaveData.sliders.Count > 0) V3 = 1;

                    if (!SongDetailsUtil.IsAvailable)
					{
						SS = 0;
					}
					else if (SongDetailsUtil.songDetails != null)
					{
						async Task wrapperAsync()
						{
							var ch = (SongDetailsCache.Structs.MapCharacteristic)BeatmapsUtil.GetCharacteristicFromDifficulty(____selectedDifficultyBeatmap);

							if (ch != SongDetailsCache.Structs.MapCharacteristic.Standard)
							{
								SS = 0;
							}
							else
							{
								var mh = BeatmapsUtil.GetHashOfPreview(____level);
								BL = Math.Round(await GetAsyncData(mh, beatmap.difficulty.ToString()), 2);
								if (BL == 0)
								{
									BL = 0;
								}

								if (mh == null || !SongDetailsUtil.songDetails.instance.songs.FindByHash(mh, out var song) || !song.GetDifficulty(
										out var diff, (SongDetailsCache.Structs.MapDifficulty)____selectedDifficultyBeatmap.difficulty, ch))
								{
									SS = 0;
								}
								else if (!diff.ranked)
								{
									SS = 0;
								}
								else
								{
									SS = Math.Round(diff.stars, 2);
								}
							}
						}

						await wrapperAsync();
					}
					else if (!SongDetailsUtil.FinishedInitAttempt)
					{
						await SongDetailsUtil.TryGet().ContinueWith(
							x => { if (x.Result != null) GridViewController.Apply(); },
							CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext()
						);
					}
				}
				else
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
