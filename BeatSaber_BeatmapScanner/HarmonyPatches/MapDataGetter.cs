using HarmonyLib;

namespace BeatmapScanner.Patches
{
    [HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
    public class MapDataGetter
    {
        static void Postfix(IDifficultyBeatmap ____selectedDifficultyBeatmap)
        {
            if(Config.Instance.Enabled)
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
                            Plugin.difficulty = "☆" + value.star.ToString();
                            Plugin.tech = "   Tech : " + value.tech.ToString();
                            Plugin.SetUI();
                            if (value.star > 10f)
                            {
                                Plugin.ui.color = Config.Instance.D;
                            }
                            else if (value.star >= 7.5f)
                            {
                                Plugin.ui.color = Config.Instance.C;
                            }
                            else if (value.star >= 5f)
                            {
                                Plugin.ui.color = Config.Instance.B;
                            }
                            else
                            {
                                Plugin.ui.color = Config.Instance.A;
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

    [HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.ClearContent))]
    public class ResetData
    {
        static void Postfix()
        {
            Plugin.ClearUI();
        }
    }

    [HarmonyPatch(typeof(MainFlowCoordinator), nameof(MainFlowCoordinator.HandleSoloFreePlayFlowCoordinatorDidFinish))]
    public class BackButtonReset
    {
        static void Postfix()
        {
            Plugin.ClearUI();
        }
    }
}
