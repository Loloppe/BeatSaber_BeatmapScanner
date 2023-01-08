using HMUI;
using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

// Source: https://github.com/rynan4818/PlayerInfoViewer/blob/main/PlayerInfoViewer/Views/PlayerInfoView.cs

namespace BeatmapScanner.Views
{
    public class BeatmapScannerViewer : MonoBehaviour
    {
        private PlatformLeaderboardViewController _platformLeaderboardViewController;
        public GameObject rootObject;
        private Canvas _canvas;

        public static readonly Vector2 CanvasSize = new(100, 50);
        public static readonly Vector3 Scale = new(0.01f, 0.01f, 0.01f);
        public static readonly Vector3 RightPosition = new(0.8f, 0.11f, 3.5f);
        public static readonly Vector3 RightRotation = new(0, 5, 0);

        [Inject]
        public void Constructor(PlatformLeaderboardViewController platformLeaderboardViewController)
        {
            this._platformLeaderboardViewController = platformLeaderboardViewController;
        }

        private void Awake()
        {
            try
            {
                this.rootObject = new GameObject("ui", typeof(Canvas), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                var sizeFitter = this.rootObject.GetComponent<ContentSizeFitter>();
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                this._canvas = this.rootObject.GetComponent<Canvas>();
                this._canvas.sortingOrder = 3;
                this._canvas.renderMode = RenderMode.WorldSpace;

                var rectTransform = this._canvas.transform as RectTransform;
                rectTransform.sizeDelta = CanvasSize;
                this.rootObject.transform.position = RightPosition;
                this.rootObject.transform.eulerAngles = RightRotation;
                this.rootObject.transform.localScale = Scale;

                Plugin.ui = this.CreateText(this._canvas.transform as RectTransform, string.Empty, new Vector2(10, 31));
                rectTransform = Plugin.ui.transform as RectTransform;
                rectTransform.SetParent(this._canvas.transform, false);
                rectTransform.anchoredPosition = Vector2.zero;
                Plugin.ui.fontSize = 12f;
                Plugin.ui.color = Color.white;
                Plugin.ui.text = "";

                this._platformLeaderboardViewController.didActivateEvent += this.OnLeaderboardActivated;
                this._platformLeaderboardViewController.didDeactivateEvent += this.OnLeaderboardDeactivated;
                this.rootObject.SetActive(false);

            }
            catch (Exception)
            {
                // Ignore
            }
        }

        private void OnDestroy()
        {
            this._platformLeaderboardViewController.didDeactivateEvent -= this.OnLeaderboardDeactivated;
            this._platformLeaderboardViewController.didActivateEvent -= this.OnLeaderboardActivated;
            Destroy(this.rootObject);
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
            textMesh.fontSize = 4;
            textMesh.overrideColorTags = true;
            textMesh.color = Color.white;

            textMesh.rectTransform.anchorMin = new Vector2(0f, 0f);
            textMesh.rectTransform.anchorMax = new Vector2(0f, 0f);
            textMesh.rectTransform.sizeDelta = sizeDelta;
            textMesh.rectTransform.anchoredPosition = anchoredPosition;

            gameObj.SetActive(true);
            return textMesh;
        }

        public void OnLeaderboardActivated(bool firstactivation, bool addedtohierarchy, bool screensystemenabling)
        {
            this.rootObject.SetActive(true);
        }
        public void OnLeaderboardDeactivated(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            this.rootObject.SetActive(false);
        }
    }
}
