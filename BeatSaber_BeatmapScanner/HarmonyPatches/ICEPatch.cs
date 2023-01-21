using BeatSaberMarkupLanguage.Components;
using IPA.Utilities;
using UnityEngine;
using HarmonyLib;
using HMUI;

namespace BeatmapScanner.HarmonyPatches
{
    internal class ICEPatch
    {
        public static StandardLevelDetailViewController Instance { get; set; } = null;
        public static bool ImageCover { get; set; } = false;
        public static Vector3 DefaultSizeDelta { get; set; } = Vector3.one;
        public static Vector3 DefaultPosition { get; set; } = Vector3.one;


        [HarmonyPatch(typeof(StandardLevelDetailViewController), nameof(StandardLevelDetailViewController.ShowContent))]
        public static class ImageCoverExpander
        {
            static void Prefix(StandardLevelDetailViewController __instance)
            {
                if (DefaultSizeDelta == Vector3.one || (Settings.Instance.ImageCoverExpander && !ImageCover) || (!Settings.Instance.ImageCoverExpander && ImageCover))
                {
                    Instance = __instance;
                    var levelBarTranform = __instance.transform.Find("LevelDetail").Find("LevelBarBig");
                    if (!levelBarTranform) { return; }
                    var imageTransform = levelBarTranform.Find("SongArtwork").GetComponent<RectTransform>();

                    var imageView = imageTransform.GetComponent<ImageView>();

                    if (DefaultSizeDelta == Vector3.one)
                    {
                        DefaultSizeDelta = imageTransform.sizeDelta;
                        DefaultPosition = imageTransform.localPosition;
                    }

                    if (Settings.Instance.ImageCoverExpander)
                    {
                        ImageCover = true;
                        imageTransform.sizeDelta = new(70.5f, 58);
                        imageTransform.localPosition = new(-34.4f, -56f, 0f);
                        imageView.color = new Color(0.5f, 0.5f, 0.5f, 1);
                    }
                    else
                    {
                        ImageCover = false;
                        imageTransform.sizeDelta = DefaultSizeDelta;
                        imageTransform.localPosition = DefaultPosition;
                        imageView.color = new Color(1f, 1f, 1f, 1);
                    }
                    imageTransform.SetAsFirstSibling();

                    imageView.preserveAspect = false;
                    FieldAccessor<ImageView, float>.Set(ref imageView, "_skew", 0);

                    var clickableImage = imageTransform.GetComponent<ClickableImage>();
                    if (clickableImage != null)
                    {
                        clickableImage.DefaultColor = new Color(0.5f, 0.5f, 0.5f, 1);
                        clickableImage.HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1);
                    }
                }
            }
        }
    }
}
