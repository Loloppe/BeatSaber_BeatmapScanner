using BeatSaberMarkupLanguage;
using HarmonyLib;
using HMUI;
using IPA.Utilities;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace BeatmapScanner.Patches
{
    [HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
    static class MapDataGetter
    {
        static void Postfix(IDifficultyBeatmap ____selectedDifficultyBeatmap)
        {
            if(Config.Instance.Enabled)
            {
                try
                {
                    // Not sure if that actually work, I don't use those plugins
                    var hasRequirement = SongCore.Collections.RetrieveDifficultyData(____selectedDifficultyBeatmap)?
                        .additionalDifficultyData?
                        ._requirements?.Any(x => x == "Noodle Extensions" || x == "Mapping Extensions") == true;

                    if (!hasRequirement)
                    {
                        if (____selectedDifficultyBeatmap is CustomDifficultyBeatmap beatmap)
                        {
                            if (beatmap.beatmapSaveData.colorNotes.Count > 0 && beatmap.level.beatsPerMinute > 0)
                            {
                                var (star, tech, intensity, movement) = Algorithm.BeatmapScanner.Analyzer(beatmap.beatmapSaveData.colorNotes, beatmap.beatmapSaveData.bombNotes, beatmap.level.beatsPerMinute, beatmap.noteJumpMovementSpeed, beatmap.level.songDuration);

                                if (star > 999f) // ???
                                {
                                    Plugin.ClearUI();
                                }
                                else
                                {
                                    #region Apply text

                                    Plugin.difficulty.text = star.ToString();
                                    Plugin.tech.text = tech.ToString();
                                    Plugin.intensity.text = intensity.ToString();
                                    Plugin.movement.text = movement.ToString();

                                    #endregion

                                    #region Apply color

                                    if (star > 9f)
                                    {
                                        Plugin.difficulty.color = Config.Instance.D;
                                    }
                                    else if (star >= 7f)
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

                                    if (tech > 0.4f)
                                    {
                                        Plugin.tech.color = Config.Instance.D;
                                    }
                                    else if (tech >= 0.3f)
                                    {
                                        Plugin.tech.color = Config.Instance.C;
                                    }
                                    else if (tech >= 0.2f)
                                    {
                                        Plugin.tech.color = Config.Instance.B;
                                    }
                                    else
                                    {
                                        Plugin.tech.color = Config.Instance.A;
                                    }

                                    if (intensity > 0.5f)
                                    {
                                        Plugin.intensity.color = Config.Instance.D;
                                    }
                                    else if (intensity >= 0.4f)
                                    {
                                        Plugin.intensity.color = Config.Instance.C;
                                    }
                                    else if (intensity >= 0.3f)
                                    {
                                        Plugin.intensity.color = Config.Instance.B;
                                    }
                                    else
                                    {
                                        Plugin.intensity.color = Config.Instance.A;
                                    }

                                    if (movement > 0.4f)
                                    {
                                        Plugin.movement.color = Config.Instance.D;
                                    }
                                    else if (movement >= 0.3f)
                                    {
                                        Plugin.movement.color = Config.Instance.C;
                                    }
                                    else if (movement >= 0.2f)
                                    {
                                        Plugin.movement.color = Config.Instance.B;
                                    }
                                    else
                                    {
                                        Plugin.movement.color = Config.Instance.A;
                                    }

                                    #endregion
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
                catch(Exception e)
                {
                    Plugin.Log.Error(e.Message);
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
