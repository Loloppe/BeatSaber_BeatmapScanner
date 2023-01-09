using System;
using Zenject;
using HMUI;
using UnityEngine;

// Inspired by: https://github.com/Spooky323/ImageCoverExpander/blob/master/ImageCoverExpander/ArtworkViewManager.cs

namespace BeatmapScanner.Views
{
    public class ArtworkViewManager : IInitializable, IDisposable
    {
        private StandardLevelDetailViewController _standardLevelViewController;
        private MainMenuViewController _mainMenuViewController;

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
            try
            {
                var imageTransform = levelBarTranform.Find("SongArtwork").GetComponent<RectTransform>();

                Plugin.star = CreateText(imageTransform, "☆", new Vector2(3.6f, 43.75f));
                Plugin.difficulty = CreateText(Plugin.star.rectTransform, string.Empty, new Vector2(4.2f, 0f));
                Plugin.t = CreateText(Plugin.difficulty.rectTransform, "T", new Vector2(15f, 0f));
                Plugin.tech = CreateText(Plugin.t.rectTransform, string.Empty, new Vector2(3f, 0f));
                Plugin.star.rectTransform.Rotate(new Vector3(0, 10f));
            }
            catch (Exception e)
            {
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
