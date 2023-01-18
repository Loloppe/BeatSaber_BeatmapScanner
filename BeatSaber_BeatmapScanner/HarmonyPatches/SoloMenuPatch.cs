﻿#region Import

using BeatSaberMarkupLanguage.Components;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using IPA.Utilities;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using System.IO;
using System;
using TMPro;
using HMUI;

#endregion

namespace BeatmapScanner.Patches
{
    #region RefreshContent

    [HarmonyPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
    public static class SoloMenuPatch
    {
        
        public static bool FirstRun { get; set; } = true;
        public static bool OldVal { get; set; } = false;

        #region Output

        public static double OldDiff { get; set; } = 0;
        public static double NewDiff { get; set; } = 0;
        public static double Tech { get; set; } = 0;
        public static double Intensity { get; set; } = 0;
        public static double EBPM { get; set; } = 0;

        #endregion

        #region UI

        public static StandardLevelDetailViewController Instance { get; set; } = null;
        public static List<TextMeshProUGUI> Fields { get; set; } = new();
        public static List<HoverHint> HoverTXT { get; set; } = new();
        public static HoverHintController HHC { get; set; } = null;
        public static GameObject ExtraUI { get; set; } = null;
        public static bool ImageCover { get; set; } = false;

        // Read images from embedded files as string and then convert to bytes.
        internal static byte[] YEET(string name)
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
        // All the check are for if the user reload from settings menu. I'm not sure why, but the objects don't stay active and everything need to be redone pretty much.
        internal static IEnumerator Process()
        {
            yield return new WaitForEndOfFrame();

            static void ModifyValue(TextMeshProUGUI text, string hoverHint, string icon)
            {
                // Set image
                var t = text.transform.parent.Find("Icon").GetComponent<ImageView>();
                if (icon.Count() > 0)
                {
                    var img = YEET(icon);

                    Texture2D tex = new(2, 2);
                    ImageConversion.LoadImage(tex, img);
                    if (tex != null)
                    {
                        t.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }
                else if (t != null) // Or destroy if we don't want them
                {
                    GameObject.DestroyImmediate(t);
                }

                // Not sure why we destroy that one
                var locHoverHint = text.GetComponentInParent<LocalizedHoverHint>();
                if (locHoverHint != null)
                {
                    GameObject.DestroyImmediate(text.GetComponentInParent<LocalizedHoverHint>());
                }

                var hhint = text.GetComponentInParent<HoverHint>();
                if (hhint == null)
                {
                    return;
                }

                if (HHC == null)
                {
                    HHC = UnityEngine.Object.FindObjectOfType<HoverHintController>();
                }

                ReflectionUtil.SetField(hhint, "_hoverHintController", HHC);

                hhint.text = hoverHint;
                HoverTXT.Add(hhint); // So we can modify them afterward, keep them in a list/array
            }


            Fields[0].text = "";
            Fields[1].text = "";
            Fields[2].text = "";
            Fields[3].text = "";

            ModifyValue(Fields[0], "", "fire");
            ModifyValue(Fields[1], "% chance to badcut", "ruler");
            ModifyValue(Fields[2], "", "");
            ModifyValue(Fields[3], "", "");

            if (HoverTXT.Count() == 4) // On reload, they stay disabled
            {
                Fields[2].GetComponentInParent<Touchable>().enabled = false;
                Fields[3].GetComponentInParent<Touchable>().enabled = false;
                HoverTXT[2].enabled = false;
                HoverTXT[3].enabled = false;
            }

            FirstRun = false; // Now ready to display stuff
        }

        #endregion

        #region Apply Data

        public static void ApplyColor() // TODO: Make those "step" configurable from the menu I guess
        {
            if (!Config.Instance.OldValue) // Diff
            {
                if (NewDiff > 9f)
                {
                    Fields[0].color = Config.Instance.D;
                }
                else if (NewDiff >= 7f)
                {
                    Fields[0].color = Config.Instance.C;
                }
                else if (NewDiff >= 5f)
                {
                    Fields[0].color = Config.Instance.B;
                }
                else
                {
                    Fields[0].color = Config.Instance.A;
                }
            }
            else // Old diff
            {
                if (OldDiff > 9f)
                {
                    Fields[0].color = Config.Instance.D;
                }
                else if (OldDiff >= 7f)
                {
                    Fields[0].color = Config.Instance.C;
                }
                else if (OldDiff >= 5f)
                {
                    Fields[0].color = Config.Instance.B;
                }
                else
                {
                    Fields[0].color = Config.Instance.A;
                }
            }

            // Tech
            if (Tech > 0.4f)
            {
                Fields[1].color = Config.Instance.D;
            }
            else if (Tech >= 0.3f)
            {
                Fields[1].color = Config.Instance.C;
            }
            else if (Tech >= 0.2f)
            {
                Fields[1].color = Config.Instance.B;
            }
            else
            {
                Fields[1].color = Config.Instance.A;
            }
        }

        public static void ApplyText()
        {
            if (!Config.Instance.OldValue)
            {
                Fields[0].text = Math.Round(NewDiff, 2).ToString();
            }
            else
            {
                Fields[0].text = Math.Round(OldDiff, 2).ToString();
            }

            Fields[1].text = Math.Round(Tech, 2).ToString();
            HoverTXT[0].text = "Intensity: " + Math.Round(Intensity, 2).ToString() + " Peak BPM: " + Math.Round(EBPM).ToString();
        }

        #endregion

        static void Postfix(IDifficultyBeatmap ____selectedDifficultyBeatmap, LevelParamsPanel ____levelParamsPanel)
        {
            if (ExtraUI == null)
            {
                // Set back default value on those in case it's a reset
                FirstRun = true;
                Fields = new();
                HoverTXT = new();

                ExtraUI = GameObject.Instantiate(____levelParamsPanel, ____levelParamsPanel.transform.parent).gameObject;
                GameObject.Destroy(ExtraUI.GetComponent<LevelParamsPanel>());
                ExtraUI.transform.localPosition += new Vector3(0, 8f);
                Fields.AddRange(ExtraUI.GetComponentsInChildren<CurvedTextMeshPro>());
                SharedCoroutineStarter.instance.StartCoroutine(Process());
            }

            // Not sure if that actually work, I don't use those plugins
            var hasRequirement = SongCore.Collections.RetrieveDifficultyData(____selectedDifficultyBeatmap)?
                .additionalDifficultyData?
                ._requirements?.Any(x => x == "Noodle Extensions" || x == "Mapping Extensions") == true;

            if (!hasRequirement && ____selectedDifficultyBeatmap is CustomDifficultyBeatmap beatmap && beatmap.beatmapSaveData.colorNotes.Count > 0 && beatmap.level.beatsPerMinute > 0 && !FirstRun)
            {
                (NewDiff, OldDiff, Tech, Intensity, EBPM) = Algorithm.BeatmapScanner.Analyzer(beatmap.beatmapSaveData.colorNotes, beatmap.beatmapSaveData.bombNotes, beatmap.level.beatsPerMinute);

                ApplyText();

                ApplyColor();
            }
            else if (Fields.Count() > 1)
            {
                Fields[0].text = "";
                Fields[1].text = "";
            }
        }
    }

    #endregion

    #region ImageCoverExpander

    // Yeeted from https://github.com/Spooky323/ImageCoverExpander/blob/master/ImageCoverExpander/ArtworkViewManager.cs
    [HarmonyPatch(typeof(StandardLevelDetailViewController), nameof(StandardLevelDetailViewController.ShowContent))]
    public static class ImageCoverExpander
    {
        

        static void Prefix(StandardLevelDetailViewController __instance)
        {
            // On first run/reload or if config was modified
            if (SoloMenuPatch.FirstRun || (Config.Instance.ImageCoverExpander && !SoloMenuPatch.ImageCover) || (!Config.Instance.ImageCoverExpander && SoloMenuPatch.ImageCover))
            {
                try
                {
                    SoloMenuPatch.Instance = __instance;
                    var levelBarTranform = __instance.transform.Find("LevelDetail").Find("LevelBarBig");
                    if (!levelBarTranform) { return; }
                    var imageTransform = levelBarTranform.Find("SongArtwork").GetComponent<RectTransform>();

                    var imageView = imageTransform.GetComponent<ImageView>();

                    if (Config.Instance.ImageCoverExpander)
                    {
                        SoloMenuPatch.ImageCover = true;
                        imageTransform.sizeDelta = new(70.5f, 58);
                        imageTransform.localPosition = new(-34.4f, -56f, 0f);
                        imageView.color = new Color(0.5f, 0.5f, 0.5f, 1);
                    }
                    else
                    {
                        SoloMenuPatch.ImageCover = false;
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
