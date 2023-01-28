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

		public static double Diff { get; set; } = 0;
		public static double Tech { get; set; } = 0;
		public static double EBPM { get; set; } = 0;
		public static double SS { get; set; } = 0;
		public static double BL { get; set; } = 0;
		public static double Slider { get; set; } = 0;
		public static double Crouch { get; set; } = 0;
		public static double Reset { get; set; } = 0;
		public static bool V3 { get; set; } = false;

		#endregion

		static async void Postfix(IDifficultyBeatmap ____selectedDifficultyBeatmap, IBeatmapLevel ____level)
		{
			if (Settings.Instance.Enabled)
			{
				ResetValues();

				var hasRequirement = SongCore.Collections.RetrieveDifficultyData(____selectedDifficultyBeatmap)?
					.additionalDifficultyData?
					._requirements?.Any(x => x == "Noodle Extensions" || x == "Mapping Extensions") == true;

				if (____selectedDifficultyBeatmap is CustomDifficultyBeatmap beatmap && beatmap.beatmapSaveData.colorNotes.Count > 0 && beatmap.level.beatsPerMinute > 0)
				{
					if (beatmap.beatmapSaveData.burstSliders.Any() || beatmap.beatmapSaveData.sliders.Any())
					{
						V3 = true;
					}

					(Diff, Tech, EBPM, Slider, Reset, Crouch) = Algorithm.BeatmapScanner.Analyzer(beatmap.beatmapSaveData.colorNotes, beatmap.beatmapSaveData.bombNotes, beatmap.beatmapSaveData.obstacles, beatmap.level.beatsPerMinute);

					if (hasRequirement)
					{
						Diff = -1;
						Tech = -1;
						Reset = -1;
						Crouch = -1;
					}

					if (!SongDetailsUtil.IsAvailable)
					{
						SS = -1;
					}
					else if (SongDetailsUtil.songDetails != null)
					{
						async Task wrapperAsync()
						{
							var ch = (SongDetailsCache.Structs.MapCharacteristic)BeatmapsUtil.GetCharacteristicFromDifficulty(____selectedDifficultyBeatmap);

							if (ch != SongDetailsCache.Structs.MapCharacteristic.Standard)
							{
								SS = -1;
							}
							else
							{
								var mh = BeatmapsUtil.GetHashOfPreview(____level);
								BL = Math.Round(await GetAsyncData(mh, beatmap.difficulty.ToString()), 2);
								if (BL == 0)
								{
									BL = -1;
								}

								if (mh == null || !SongDetailsUtil.songDetails.instance.songs.FindByHash(mh, out var song) || !song.GetDifficulty(
										out var diff, (SongDetailsCache.Structs.MapDifficulty)____selectedDifficultyBeatmap.difficulty, ch))
								{
									SS = -1;
								}
								else if (!diff.ranked)
								{
									SS = -1;
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

				GridViewController.Apply();
			}
		}

		public static void ResetValues()
		{
			Diff = 0;
			Tech = 0;
			EBPM = 0;
			SS = 0;
			BL = 0;
			Slider = 0;
			Crouch = 0;
			Reset = 0;
			V3 = false;

			if (UICreator._floatingScreen != null)
			{
				GridViewController.Apply();
			}
		}

		public static async Task<double> GetAsyncData(string hash, string difficulty)
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
							return -1;
						}
					}
				}

				return -1;
			}
			else
			{
				return -1;
			}
		}
	}
}
