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
        static GameObject extraUI = null;
        static HoverHintController hhc = null;
        static TextMeshProUGUI[] fields = null;
        static List<CurvedTextMeshPro> icons = new();
        static List<HoverHint> hoverTexts = new();

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

                    if (hhc == null)
                        hhc = UnityEngine.Object.FindObjectOfType<HoverHintController>();

                    ReflectionUtil.SetField(hhint, "_hoverHintController", hhc);

                    hhint.text = hoverHint;
                    hoverTexts.Add(hhint);
                }

                fields[0].text = "";
                fields[1].text = "";
                fields[2].text = "";
                fields[3].text = "";

                ModifyValue(fields[0], "How hard it is to pass");
                ModifyValue(fields[1], "% chance to badcut");
                ModifyValue(fields[2], "Average intensity");
                ModifyValue(fields[3], "");

                icons.Add(CreateText(fields[0].rectTransform, "💪", fields[0].transform.localPosition + new Vector3(-7.5f, 6f, -0.5f)));
                icons.Add(CreateText(fields[1].rectTransform, "📐", fields[1].transform.localPosition + new Vector3(-7.5f, 6f, -3f)));
                icons.Add(CreateText(fields[2].rectTransform, "🔥", fields[2].transform.localPosition + new Vector3(-7.5f, 6f, -6.5f)));

                icons[0].transform.Rotate(new Vector3(0, 0f));
                icons[1].transform.Rotate(new Vector3(0, 10f));
                icons[2].transform.Rotate(new Vector3(0, 20f));

                icons[0].fontSize = 3f;
                icons[1].fontSize = 3f;
                icons[2].fontSize = 3f;
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
            if(Config.Instance.Enabled)
            {
                try
                {
                    if (extraUI == null)
                    {
                        extraUI = GameObject.Instantiate(____levelParamsPanel, ____levelParamsPanel.transform.parent).gameObject;
                        GameObject.DestroyImmediate(extraUI.GetComponent<LevelParamsPanel>());

                        extraUI.transform.localPosition += new Vector3(0, 8f);

                        fields = extraUI.GetComponentsInChildren<CurvedTextMeshPro>();
  
                        SharedCoroutineStarter.instance.StartCoroutine(ProcessFields());
                    }

                    // Not sure if that actually work, I don't use those plugins
                    var hasRequirement = SongCore.Collections.RetrieveDifficultyData(____selectedDifficultyBeatmap)?
                        .additionalDifficultyData?
                        ._requirements?.Any(x => x == "Noodle Extensions" || x == "Mapping Extensions") == true;

                    if (!hasRequirement && ____selectedDifficultyBeatmap is CustomDifficultyBeatmap beatmap && hoverTexts.Count() >= 3 && beatmap.beatmapSaveData.colorNotes.Count > 0 && beatmap.level.beatsPerMinute > 0)
                    {
                        var (diff, tech, intensity, ebpm) = Algorithm.BeatmapScanner.Analyzer(beatmap.beatmapSaveData.colorNotes, beatmap.beatmapSaveData.bombNotes, beatmap.level.beatsPerMinute);

                        #region Apply text

                        if (fields.Count() > 2)
                        {
                            fields[0].text = diff.ToString();
                            fields[1].text = tech.ToString();
                            fields[2].text = intensity.ToString();
                            hoverTexts[2].text = "Peak BPM is " + ebpm.ToString();

                            icons[0].text = "💪";
                            icons[1].text = "📐";
                            icons[2].text = "🔥";
                        }

                        #endregion

                        #region Apply color

                        if (diff > 9f)
                        {
                            fields[0].color = Config.Instance.D;
                        }
                        else if (diff >= 7f)
                        {
                            fields[0].color = Config.Instance.C;
                        }
                        else if (diff >= 5f)
                        {
                            fields[0].color = Config.Instance.B;
                        }
                        else
                        {
                            fields[0].color = Config.Instance.A;
                        }

                        if (tech > 0.4f)
                        {
                            fields[1].color = Config.Instance.D;
                        }
                        else if (tech >= 0.3f)
                        {
                            fields[1].color = Config.Instance.C;
                        }
                        else if (tech >= 0.2f)
                        {
                            fields[1].color = Config.Instance.B;
                        }
                        else
                        {
                            fields[1].color = Config.Instance.A;
                        }

                        if (intensity > 0.5f)
                        {
                            fields[2].color = Config.Instance.D;
                        }
                        else if (intensity >= 0.4f)
                        {
                            fields[2].color = Config.Instance.C;
                        }
                        else if (intensity >= 0.3f)
                        {
                            fields[2].color = Config.Instance.B;
                        }
                        else
                        {
                            fields[2].color = Config.Instance.A;
                        }

                        #endregion
                    
                    }
                    else if(fields.Count() > 2)
                    {
                        fields[0].text = "";
                        fields[1].text = "";
                        fields[2].text = "";
                    }
                }
                catch(Exception e)
                {
                    Plugin.Log.Error(e.Message);
                }
            }
            else if(icons.Count() > 2)
            {
                fields[0].text = "";
                fields[1].text = "";
                fields[2].text = "";
                icons[0].text = "";
                icons[1].text = "";
                icons[2].text = "";
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
        static Vector3 OldSizeDelta = new();
        static Vector3 OldPosition = new();
        static readonly float ModifiedSkew = 0;
        static bool ImageCover = false;

        static void Prefix(StandardLevelDetailViewController __instance)
        {
            if((ImageCover && !Config.Instance.ImageCoverExpander) || (!ImageCover && Config.Instance.ImageCoverExpander))
            {
                try
                {
                    var levelBarTranform = __instance.transform.Find("LevelDetail").Find("LevelBarBig");
                    if (!levelBarTranform) { return; }
                    var imageTransform = levelBarTranform.Find("SongArtwork").GetComponent<RectTransform>();

                    if (OldSizeDelta == new Vector3())
                    {
                        OldSizeDelta = imageTransform.sizeDelta;
                        OldPosition = imageTransform.localPosition;
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
                        imageTransform.sizeDelta = OldSizeDelta;
                        imageTransform.localPosition = OldPosition;

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
