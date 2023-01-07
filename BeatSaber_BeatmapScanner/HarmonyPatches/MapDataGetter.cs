using HarmonyLib;
using UnityEngine;

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
                    if (beatmap.beatmapSaveData.colorNotes.Count > 0)
                    {
                        var value = Algorithm.BeatmapScanner.Analyzer(beatmap.beatmapSaveData.colorNotes, ____selectedDifficultyBeatmap.level.beatsPerMinute);
                        Plugin.difficulty.text = "☆" + value.ToString();
                        if (value > 10f)
                        {
                            Plugin.difficulty.color = Config.Instance.D;
                        }
                        else if (value >= 7.5f)
                        {
                            Plugin.difficulty.color = Config.Instance.C;
                        }
                        else if (value >= 5f)
                        {
                            Plugin.difficulty.color = Config.Instance.B;
                        }
                        else
                        {
                            Plugin.difficulty.color = Config.Instance.A;
                        }
                    }
                    else
                    {
                        Plugin.difficulty.text = "";
                    }
                }
                else
                {
                    Plugin.difficulty.text = "";
                }
            }
            else
            {
                Plugin.difficulty.text = "";
            }
        }
    }

    [HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.ClearContent))]
    public class ResetData
    {
        static void Postfix()
        {
            Plugin.difficulty.text = "";
        }
    }

    [HarmonyPatch(typeof(MainFlowCoordinator), nameof(MainFlowCoordinator.HandleSoloFreePlayFlowCoordinatorDidFinish))]
    public class BackButtonReset
    {
        static void Postfix()
        {
            Plugin.difficulty.text = "";
        }
    }
}
