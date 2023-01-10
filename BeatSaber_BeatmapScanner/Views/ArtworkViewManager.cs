using System;
using Zenject;
using HMUI;
using UnityEngine;
using IPA.Utilities;
using BeatSaberMarkupLanguage.Components;

// I fused the UI with ImageCoverExpander: https://github.com/Spooky323/ImageCoverExpander/blob/master/ImageCoverExpander/ArtworkViewManager.cs

namespace BeatmapScanner.Views
{
    public class ArtworkViewManager : IInitializable, IDisposable
    {
        private StandardLevelDetailViewController _standardLevelViewController;
        private MainMenuViewController _mainMenuViewController;

        private static readonly Vector3 modifiedSizeDelta = new(70.5f, 58);
        private static readonly Vector3 modifiedPositon = new(-34.4f, -56f, 0f);
        private static readonly float modifiedSkew = 0;

        public ArtworkViewManager(StandardLevelDetailViewController standardLevelDetailViewController, MainMenuViewController mainMenuViewController)
        {
            _standardLevelViewController = standardLevelDetailViewController;
            _mainMenuViewController = mainMenuViewController;
        }

        public void Initialize()
        {
            _mainMenuViewController.didFinishEvent += OnDidFinishEvent;
        }

        public void Dispose()
        {
            _mainMenuViewController.didFinishEvent -= OnDidFinishEvent;
        }

        private void OnDidFinishEvent(MainMenuViewController _, MainMenuViewController.MenuButton __)
        {
            var levelBarTranform = _standardLevelViewController.transform.Find("LevelDetail").Find("LevelBarBig");
            if (!levelBarTranform) { return; }
            Plugin.Log.Notice("Changing artwork for " + levelBarTranform.name);
            try
            {
                var imageTransform = levelBarTranform.Find("SongArtwork").GetComponent<RectTransform>();

                if (Config.Instance.ImageCoverExpander)
                {
                    imageTransform.sizeDelta = modifiedSizeDelta;
                    imageTransform.localPosition = modifiedPositon;
                    imageTransform.SetAsFirstSibling();

                    var imageView = imageTransform.GetComponent<ImageView>();
                    imageView.color = new Color(0.5f, 0.5f, 0.5f, 1);
                    imageView.preserveAspect = false;
                    FieldAccessor<ImageView, float>.Set(ref imageView, "_skew", modifiedSkew);

                    // DiTails
                    var clickableImage = imageTransform.GetComponent<ClickableImage>();
                    if (clickableImage != null)
                    {
                        clickableImage.DefaultColor = new Color(0.5f, 0.5f, 0.5f, 1);
                        clickableImage.HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1);
                    }
                }

                if(Config.Instance.ImageCoverExpander)
                {
                    Plugin.star = CreateText(imageTransform, "☆", new Vector2(3.7f, 48.75f));
                }
                else
                {
                    Plugin.star = CreateText(imageTransform, "☆", new Vector2(3.7f, 1.5f));
                }
                Plugin.difficulty = CreateText(Plugin.star.rectTransform, string.Empty, new Vector2(4.2f, 0f));
                Plugin.t = CreateText(Plugin.difficulty.rectTransform, "T", new Vector2(15f, 0f));
                Plugin.tech = CreateText(Plugin.t.rectTransform, string.Empty, new Vector2(3f, 0f));
                Plugin.i = CreateText(Plugin.tech.rectTransform, "I", new Vector2(15f, 0f));
                Plugin.intensity = CreateText(Plugin.i.rectTransform, string.Empty, new Vector2(3f, 0f));
                Plugin.m = CreateText(Plugin.intensity.rectTransform, "M", new Vector2(14.8f, 0f));
                Plugin.movement = CreateText(Plugin.m.rectTransform, string.Empty, new Vector2(3f, 0f));
                Plugin.star.rectTransform.Rotate(new Vector3(0, 10f));
                Plugin.i.rectTransform.Rotate(new Vector3(0, 20f));

            }
            catch (Exception e)
            {
                Plugin.Log.Error("Error changing artwork fields for " + levelBarTranform.name);
                Plugin.Log.Error(e);
            }
        }

        private CurvedTextMeshPro CreateText(RectTransform parent, string text, Vector2 anchoredPosition)
        {
            return this.CreateText(parent, text, anchoredPosition, new Vector2(0, 0));
        }

        private CurvedTextMeshPro CreateText(RectTransform parent, string text, Vector2 anchoredPosition, Vector2 sizeDelta)
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
            textMesh.rectTransform.sizeDelta = sizeDelta;
            textMesh.rectTransform.anchoredPosition = anchoredPosition;

            gameObj.SetActive(true);
            return textMesh;
        }
    }
}
