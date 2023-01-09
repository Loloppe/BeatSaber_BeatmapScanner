using HarmonyLib;
using System.Linq;

namespace BeatmapScanner.Patches
{
    [HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
    public class MapDataGetter
    {
        static void Postfix(IDifficultyBeatmap ____selectedDifficultyBeatmap)
        {
            if(Config.Instance.Enabled)
            {
                var hasRequirement = SongCore.Collections.RetrieveDifficultyData(____selectedDifficultyBeatmap)?
                    .additionalDifficultyData?
                    ._requirements?.Any(x => x == "Noodle Extensions" || x == "Mapping Extensions") == true;

                if (!hasRequirement)
                {
                    if (____selectedDifficultyBeatmap is CustomDifficultyBeatmap beatmap)
                    {
                        if (beatmap.beatmapSaveData.colorNotes.Count > 20)
                        {
                            var value = Algorithm.BeatmapScanner.Analyzer(beatmap.beatmapSaveData.colorNotes, ____selectedDifficultyBeatmap.level.beatsPerMinute);
                            if (value.star <= 0f || value.star > 999f)
                            {
                                Plugin.ClearUI();
                            }
                            else
                            {
                                Plugin.difficulty.text = value.star.ToString();
                                Plugin.tech.text = value.tech.ToString();
                                if (value.star > 10f)
                                {
                                    Plugin.difficulty.color = Config.Instance.D;
                                }
                                else if (value.star >= 7.5f)
                                {
                                    Plugin.difficulty.color = Config.Instance.C;
                                }
                                else if (value.star >= 5f)
                                {
                                    Plugin.difficulty.color = Config.Instance.B;
                                }
                                else
                                {
                                    Plugin.difficulty.color = Config.Instance.A;
                                }

                                if (value.tech > 1.7f)
                                {
                                    Plugin.tech.color = Config.Instance.D;
                                }
                                else if (value.tech >= 1.5f)
                                {
                                    Plugin.tech.color = Config.Instance.C;
                                }
                                else if (value.tech >= 1.3f)
                                {
                                    Plugin.tech.color = Config.Instance.B;
                                }
                                else
                                {
                                    Plugin.tech.color = Config.Instance.A;
                                }
                            }
                        }
                        else
                        {
                            Plugin.ClearUI();
                        }
                    }
                    else
                    {
                        Plugin.ClearUI();
                    }
                }
                else
                {
                    Plugin.ClearUI();
                }
            }
        }
    }
}
