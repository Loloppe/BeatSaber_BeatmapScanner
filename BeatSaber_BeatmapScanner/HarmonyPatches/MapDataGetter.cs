using BeatSaberMarkupLanguage.Components;
using HarmonyLib;
using HMUI;
using IPA.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace BeatmapScanner.Patches
{
    public static class Stuff
    {
        public static GameObject extraUI { get; set; } = null;
        public static HoverHintController hhc { get; set; } = null;
        public static TextMeshProUGUI[] fields { get; set; } = null;
        public static List<HoverHint> hoverTexts { get; set; } = new();
    }

    [HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
    static class MapDataGetter
    {
        static byte[] YEET(string name)
        {
            try
            {
                using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BeatmapScanner.Icons." + name + ".png");
                using StreamReader streamReader = new(stream);
                using MemoryStream memoryStream = new();
                streamReader.BaseStream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
            catch (Exception innerException)
            {
                Plugin.Log.Info(innerException.Message);
            }

            return null;
        }

        // Yeeted from https://github.com/kinsi55/BeatSaber_BetterSongList/blob/master/HarmonyPatches/UI/ExtraLevelParams.cs
        static IEnumerator Process()
        {
            yield return new WaitForEndOfFrame();

            static void ModifyValue(TextMeshProUGUI text, string hoverHint, string icon)
            {
                var t = text.transform.parent.Find("Icon").GetComponent<ImageView>();

                if(icon.Count() > 0)
                {
                    var img = YEET(icon);

                    Texture2D tex = new(2, 2);
                    ImageConversion.LoadImage(tex, img);
                    if (tex != null)
                    {
                        t.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }
                else
                {
                    GameObject.DestroyImmediate(t);
                }

                GameObject.DestroyImmediate(text.GetComponentInParent<LocalizedHoverHint>());
                var hhint = text.GetComponentInParent<HoverHint>();

                if (hhint == null)
                    return;

                if (Stuff.hhc == null)
                    Stuff.hhc = UnityEngine.Object.FindObjectOfType<HoverHintController>();

                ReflectionUtil.SetField(hhint, "_hoverHintController", Stuff.hhc);

                hhint.text = hoverHint;
                Stuff.hoverTexts.Add(hhint);
            }

            Stuff.fields[0].text = "";
            Stuff.fields[1].text = "";
            Stuff.fields[2].text = "";
            Stuff.fields[3].text = "";

            ModifyValue(Stuff.fields[0], "", "fire");
            ModifyValue(Stuff.fields[1], "% chance to badcut", "ruler");
            ModifyValue(Stuff.fields[2], "", "");
            ModifyValue(Stuff.fields[3], "", "");

            Stuff.fields[2].GetComponentInParent<Touchable>().enabled = false;
            Stuff.fields[3].GetComponentInParent<Touchable>().enabled = false;
            Stuff.hoverTexts[2].enabled = false;
            Stuff.hoverTexts[3].enabled = false;
        }

        private static CurvedTextMeshPro CreateText(RectTransform parent, string text, Vector3 anchoredPosition)
        {
            var gameObj = new GameObject("CustomUIText");
            gameObj.SetActive(false);

            var textMesh = gameObj.AddComponent<CurvedTextMeshPro>();
            textMesh.rectTransform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = 3f;
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
                // Didn't manage to remove the Raycast from specific elements, so added config to change position instead.
                if (Stuff.extraUI == null)
                {
                    Stuff.extraUI = GameObject.Instantiate(____levelParamsPanel, ____levelParamsPanel.transform.parent).gameObject;
                    Stuff.extraUI.transform.position = new Vector3(0.30f, 1.55f, 4.35f);
                    Stuff.fields = Stuff.extraUI.GetComponentsInChildren<CurvedTextMeshPro>();
                    SharedCoroutineStarter.instance.StartCoroutine(Process());
                }

                // Not sure if that actually work, I don't use those plugins
                var hasRequirement = SongCore.Collections.RetrieveDifficultyData(____selectedDifficultyBeatmap)?
                    .additionalDifficultyData?
                    ._requirements?.Any(x => x == "Noodle Extensions" || x == "Mapping Extensions") == true;

                if (!hasRequirement && ____selectedDifficultyBeatmap is CustomDifficultyBeatmap beatmap && Stuff.hoverTexts.Count() > 1 && beatmap.beatmapSaveData.colorNotes.Count > 0 && beatmap.level.beatsPerMinute > 0)
                {
                    var (diff, tech, intensity, ebpm) = Algorithm.BeatmapScanner.Analyzer(beatmap.beatmapSaveData.colorNotes, beatmap.beatmapSaveData.bombNotes, beatmap.level.beatsPerMinute);

                    #region Apply text

                    if (Stuff.fields.Count() > 2)
                    {
                        Stuff.fields[0].text = diff.ToString();
                        Stuff.fields[1].text = tech.ToString();
                        Stuff.hoverTexts[0].text = "Intensity: " + intensity.ToString() + " Peak BPM: " + ebpm.ToString();
                    }

                    #endregion

                    #region Apply color

                    if (diff > 9f)
                    {
                        Stuff.fields[0].color = Config.Instance.D;
                    }
                    else if (diff >= 7f)
                    {
                        Stuff.fields[0].color = Config.Instance.C;
                    }
                    else if (diff >= 5f)
                    {
                        Stuff.fields[0].color = Config.Instance.B;
                    }
                    else
                    {
                        Stuff.fields[0].color = Config.Instance.A;
                    }

                    if (tech > 0.4f)
                    {
                        Stuff.fields[1].color = Config.Instance.D;
                    }
                    else if (tech >= 0.3f)
                    {
                        Stuff.fields[1].color = Config.Instance.C;
                    }
                    else if (tech >= 0.2f)
                    {
                        Stuff.fields[1].color = Config.Instance.B;
                    }
                    else
                    {
                        Stuff.fields[1].color = Config.Instance.A;
                    }

                    #endregion

                }
                else if (Stuff.fields.Count() > 2)
                {
                    Stuff.fields[0].text = "";
                    Stuff.fields[1].text = "";
                    Stuff.fields[2].text = "";
                }
            }
            catch (Exception e)
            {
                Plugin.Log.Error(e);
                Plugin.Log.Error(e.Message);
            }
        }
    }

    #region ImageCoverExpander

    // Yeeted from https://github.com/Spooky323/ImageCoverExpander/blob/master/ImageCoverExpander/ArtworkViewManager.cs
    [HarmonyPatch(typeof(StandardLevelDetailViewController), nameof(StandardLevelDetailViewController.ShowContent))]
    public class ImageCoverExpander
    {
        static bool FirstRun = true;
        static bool ImageCover = false;

        static void Prefix(StandardLevelDetailViewController __instance)
        {
            if ((ImageCover && !Config.Instance.ImageCoverExpander) || (!ImageCover && Config.Instance.ImageCoverExpander) || FirstRun)
            {
                try
                {
                    var levelBarTranform = __instance.transform.Find("LevelDetail").Find("LevelBarBig");
                    if (!levelBarTranform) { return; }
                    var imageTransform = levelBarTranform.Find("SongArtwork").GetComponent<RectTransform>();

                    var imageView = imageTransform.GetComponent<ImageView>();

                    if (Config.Instance.ImageCoverExpander)
                    {
                        ImageCover = true;
                        imageTransform.sizeDelta = new(70.5f, 58);
                        imageTransform.localPosition = new(-34.4f, -56f, 0f);
                        imageView.color = new Color(0.5f, 0.5f, 0.5f, 1);
                    }
                    else
                    {
                        ImageCover = false;
                        imageTransform.sizeDelta = new(10f, 10f);
                        imageTransform.localPosition = new(-30f, -12f);
                        imageView.color = new Color(1f, 1f, 1f, 1);
                    }
                    imageTransform.SetAsFirstSibling();

                    imageView.preserveAspect = false;
                    FieldAccessor<ImageView, float>.Set(ref imageView, "_skew", 0);

                    // DiTails
                    var clickableImage = imageTransform.GetComponent<ClickableImage>();
                    if (clickableImage != null)
                    {
                        clickableImage.DefaultColor = new Color(0.5f, 0.5f, 0.5f, 1);
                        clickableImage.HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1);
                    }

                    FirstRun = false;
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
