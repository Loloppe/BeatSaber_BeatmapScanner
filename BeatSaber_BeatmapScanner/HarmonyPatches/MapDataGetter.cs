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
                            if(Config.Instance.Log)
                            {
                                Plugin.Log.Info("---------------------------------------------------------");
                                Plugin.Log.Info("Beatmap Name: " + beatmap.level.songName + " Difficulty: " + beatmap.difficulty);
                            }
                            
                            var (star, tech, intensity, movement) = Algorithm.BeatmapScanner.Analyzer(beatmap.beatmapSaveData.colorNotes, beatmap.level.beatsPerMinute);

                            if (star <= 0f || star > 999f)
                            {
                                Plugin.ClearUI();
                            }
                            else
                            {

                                Plugin.difficulty.text = star.ToString();
                                Plugin.tech.text = tech.ToString();
                                Plugin.intensity.text = intensity.ToString();
                                Plugin.movement.text = movement.ToString();

                                if (star > 10f)
                                {
                                    Plugin.difficulty.color = Config.Instance.D;
                                }
                                else if (star >= 7.5f)
                                {
                                    Plugin.difficulty.color = Config.Instance.C;
                                }
                                else if (star >= 5f)
                                {
                                    Plugin.difficulty.color = Config.Instance.B;
                                }
                                else
                                {
                                    Plugin.difficulty.color = Config.Instance.A;
                                }

                                if (tech > 0.5f)
                                {
                                    Plugin.tech.color = Config.Instance.D;
                                }
                                else if (tech >= 0.4f)
                                {
                                    Plugin.tech.color = Config.Instance.C;
                                }
                                else if (tech >= 0.3f)
                                {
                                    Plugin.tech.color = Config.Instance.B;
                                }
                                else
                                {
                                    Plugin.tech.color = Config.Instance.A;
                                }

                                if (intensity > 4f)
                                {
                                    Plugin.intensity.color = Config.Instance.D;
                                }
                                else if (intensity >= 0.3f)
                                {
                                    Plugin.intensity.color = Config.Instance.C;
                                }
                                else if (intensity >= 0.2f)
                                {
                                    Plugin.intensity.color = Config.Instance.B;
                                }
                                else
                                {
                                    Plugin.intensity.color = Config.Instance.A;
                                }

                                if (movement > 0.9f)
                                {
                                    Plugin.movement.color = Config.Instance.D;
                                }
                                else if (movement >= 0.6f)
                                {
                                    Plugin.movement.color = Config.Instance.C;
                                }
                                else if (movement >= 0.3f)
                                {
                                    Plugin.movement.color = Config.Instance.B;
                                }
                                else
                                {
                                    Plugin.movement.color = Config.Instance.A;
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
            else
            {
                Plugin.ClearUI();
            }
        }
    }

    // Fix a bug that happen when changing colors in the config menu and then pressing Back instead of Apply.
    [HarmonyPatch(typeof(MainFlowCoordinator), nameof(MainFlowCoordinator.HandleSoloFreePlayFlowCoordinatorDidFinish))]
    public class BackButtonReset
    {
        static void Postfix()
        {
            Plugin.ClearUI();
        }
    }
}
