using BeatSaberMarkupLanguage.Components;
using HarmonyLib;
using HMUI;
using IPA.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace BeatmapScanner.Patches
{
    [HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
    static class MapDataGetter
    {
        // Yeeted from https://github.com/kinsi55/BeatSaber_BetterSongList/blob/master/HarmonyPatches/UI/ExtraLevelParams.cs
        static IEnumerator ProcessFields()
        {
            yield return new WaitForEndOfFrame();

            try
            {
                static void ModifyValue(TextMeshProUGUI text, string hoverHint)
                {
                    GameObject.DestroyImmediate(text.transform.parent.Find("Icon").GetComponent<ImageView>());
                    GameObject.DestroyImmediate(text.GetComponentInParent<LocalizedHoverHint>());
                    var hhint = text.GetComponentInParent<HoverHint>();

                    if (hhint == null)
                        return;

                    if (Plugin.hhc == null)
                        Plugin.hhc = UnityEngine.Object.FindObjectOfType<HoverHintController>();

                    ReflectionUtil.SetField(hhint, "_hoverHintController", Plugin.hhc);

                    hhint.text = hoverHint;
                    Plugin.hoverTexts.Add(hhint);
                }

                Plugin.fields[0].text = "";
                Plugin.fields[1].text = "";
                Plugin.fields[2].text = "";
                Plugin.fields[3].text = "";

                ModifyValue(Plugin.fields[0], "How hard it is to pass");
                ModifyValue(Plugin.fields[1], "% chance to badcut");
                ModifyValue(Plugin.fields[2], "Average intensity");
                ModifyValue(Plugin.fields[3], "");

                Plugin.icons.Add(CreateText(Plugin.fields[0].rectTransform, "💪", Plugin.fields[0].transform.localPosition + new Vector3(-7.4f, 5.4f, -0.5f)));
                Plugin.icons.Add(CreateText(Plugin.fields[1].rectTransform, "📐", Plugin.fields[1].transform.localPosition + new Vector3(-7.4f, 5.4f, -3f)));
                Plugin.icons.Add(CreateText(Plugin.fields[2].rectTransform, "🔥", Plugin.fields[2].transform.localPosition + new Vector3(-7.6f, 5.4f, -7.5f)));

                Plugin.icons[0].transform.Rotate(new Vector3(0, 0f));
                Plugin.icons[1].transform.Rotate(new Vector3(0, 12.5f));
                Plugin.icons[2].transform.Rotate(new Vector3(0, 25f));

                Plugin.icons[0].fontSize = 3f;
                Plugin.icons[1].fontSize = 3f;
                Plugin.icons[2].fontSize = 3f;
            }
            catch(Exception e)
            {
                Plugin.Log.Error(e.Message);
            }
        }

        private static CurvedTextMeshPro CreateText(RectTransform parent, string text, Vector3 anchoredPosition)
        {
            var gameObj = new GameObject("CustomUIText");
            gameObj.SetActive(false);

            var textMesh = gameObj.AddComponent<CurvedTextMeshPro>();
            textMesh.rectTransform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = 4f;
            textMesh.overrideColorTags = true;
            textMesh.color = Color.white;

            textMesh.rectTransform.anchorMin = new Vector2(0f, 0f);
            textMesh.rectTransform.anchorMax = new Vector2(0f, 0f);
            textMesh.rectTransform.sizeDelta = new Vector2(0f, 0f);
            textMesh.rectTransform.localPosition = anchoredPosition;
            textMesh.rectTransform.anchoredPosition = anchoredPosition;

            gameObj.SetActive(true);
            return textMesh;
        }

        static void Postfix(IDifficultyBeatmap ____selectedDifficultyBeatmap, LevelParamsPanel ____levelParamsPanel)
        {
            try
            {
                if (Plugin.extraUI == null)
                {
                    Plugin.extraUI = GameObject.Instantiate(____levelParamsPanel, ____levelParamsPanel.transform.parent).gameObject;
                    GameObject.DestroyImmediate(Plugin.extraUI.GetComponent<LevelParamsPanel>());

                    Plugin.extraUI.transform.localPosition += new Vector3(0, 8f);

                    Plugin.fields = Plugin.extraUI.GetComponentsInChildren<CurvedTextMeshPro>();

                    SharedCoroutineStarter.instance.StartCoroutine(ProcessFields());
                }

                // Not sure if that actually work, I don't use those plugins
                var hasRequirement = SongCore.Collections.RetrieveDifficultyData(____selectedDifficultyBeatmap)?
                    .additionalDifficultyData?
                    ._requirements?.Any(x => x == "Noodle Extensions" || x == "Mapping Extensions") == true;

                if (!hasRequirement && ____selectedDifficultyBeatmap is CustomDifficultyBeatmap beatmap && Plugin.hoverTexts.Count() >= 3 && beatmap.beatmapSaveData.colorNotes.Count > 0 && beatmap.level.beatsPerMinute > 0)
                {
                    var (diff, tech, intensity, ebpm) = Algorithm.BeatmapScanner.Analyzer(beatmap.beatmapSaveData.colorNotes, beatmap.beatmapSaveData.bombNotes, beatmap.level.beatsPerMinute);

                    #region Apply text

                    if (Plugin.fields.Count() > 2)
                    {
                        Plugin.fields[0].text = diff.ToString();
                        Plugin.fields[1].text = tech.ToString();
                        Plugin.fields[2].text = intensity.ToString();
                        Plugin.hoverTexts[2].text = "Peak BPM is " + ebpm.ToString();

                        Plugin.icons[0].text = "💪";
                        Plugin.icons[1].text = "📐";
                        Plugin.icons[2].text = "🔥";
                    }

                    #endregion

                    #region Apply color

                    if (diff > 9f)
                    {
                        Plugin.fields[0].color = Config.Instance.D;
                    }
                    else if (diff >= 7f)
                    {
                        Plugin.fields[0].color = Config.Instance.C;
                    }
                    else if (diff >= 5f)
                    {
                        Plugin.fields[0].color = Config.Instance.B;
                    }
                    else
                    {
                        Plugin.fields[0].color = Config.Instance.A;
                    }

                    if (tech > 0.4f)
                    {
                        Plugin.fields[1].color = Config.Instance.D;
                    }
                    else if (tech >= 0.3f)
                    {
                        Plugin.fields[1].color = Config.Instance.C;
                    }
                    else if (tech >= 0.2f)
                    {
                        Plugin.fields[1].color = Config.Instance.B;
                    }
                    else
                    {
                        Plugin.fields[1].color = Config.Instance.A;
                    }

                    if (intensity > 0.5f)
                    {
                        Plugin.fields[2].color = Config.Instance.D;
                    }
                    else if (intensity >= 0.4f)
                    {
                        Plugin.fields[2].color = Config.Instance.C;
                    }
                    else if (intensity >= 0.3f)
                    {
                        Plugin.fields[2].color = Config.Instance.B;
                    }
                    else
                    {
                        Plugin.fields[2].color = Config.Instance.A;
                    }

                    #endregion

                }
                else if (Plugin.fields.Count() > 2)
                {
                    Plugin.fields[0].text = "";
                    Plugin.fields[1].text = "";
                    Plugin.fields[2].text = "";
                }
            }
            catch (Exception e)
            {
                Plugin.Log.Error(e.Message);
            }
        }
    }

    #region ImageCoverExpander

    // Yeeted from https://github.com/Spooky323/ImageCoverExpander/blob/master/ImageCoverExpander/ArtworkViewManager.cs
    [HarmonyPatch(typeof(StandardLevelDetailViewController), nameof(StandardLevelDetailViewController.ShowContent))]
    public class ImageCoverExpander
    {
        static readonly Vector3 ModifiedSizeDelta = new(70.5f, 58);
        static readonly Vector3 ModifiedPositon = new(-34.4f, -56f, 0f);
        static Vector3 LocalSizeDelta = new();
        static Vector3 LocalPosition = new();
        static readonly float ModifiedSkew = 0;
        static bool ImageCover = false;

        static void Prefix(StandardLevelDetailViewController __instance)
        {
            if ((ImageCover && !Config.Instance.ImageCoverExpander) || (!ImageCover && Config.Instance.ImageCoverExpander))
            {
                try
                {
                    var levelBarTranform = __instance.transform.Find("LevelDetail").Find("LevelBarBig");
                    if (!levelBarTranform) { return; }
                    var imageTransform = levelBarTranform.Find("SongArtwork").GetComponent<RectTransform>();

                    if (LocalSizeDelta == new Vector3())
                    {
                        LocalSizeDelta = imageTransform.sizeDelta;
                        LocalPosition = imageTransform.localPosition;
                    }

                    if (Config.Instance.ImageCoverExpander)
                    {
                        imageTransform.sizeDelta = ModifiedSizeDelta;
                        imageTransform.localPosition = ModifiedPositon;

                        imageTransform.SetAsFirstSibling();

                        var imageView = imageTransform.GetComponent<ImageView>();
                        imageView.color = new Color(0.5f, 0.5f, 0.5f, 1);
                        imageView.preserveAspect = false;
                        FieldAccessor<ImageView, float>.Set(ref imageView, "_skew", ModifiedSkew);

                        // DiTails
                        var clickableImage = imageTransform.GetComponent<ClickableImage>();
                        if (clickableImage != null)
                        {
                            clickableImage.DefaultColor = new Color(0.5f, 0.5f, 0.5f, 1);
                            clickableImage.HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1);
                        }

                        ImageCover = true;
                    }
                    else
                    {
                        imageTransform.sizeDelta = LocalSizeDelta;
                        imageTransform.localPosition = LocalPosition;

                        ImageCover = false;
                    }
                }
                catch(Exception e)
                {
                    Plugin.Log.Error(e.Message);
                }
            }
        }
    }

    #endregion
}
