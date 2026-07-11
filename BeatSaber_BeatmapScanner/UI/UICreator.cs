using BeatSaberMarkupLanguage.FloatingScreen;
using UnityEngine;
using UnityEngine.UI;
using HMUI;
using VRUIControls;
using Zenject;

namespace BeatmapScanner.UI
{
	internal class UICreator
	{
		private readonly GridViewController _gridViewController;

		public static FloatingScreen _floatingScreen = null;
        public UICreator(GridViewController gridViewController) => _gridViewController = gridViewController;

        [Inject]
        private PlatformLeaderboardViewController _platformLeaderboardViewController;

        public void CreateFloatingScreen(Vector3 position, Quaternion rotation)
		{
			_floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(50f, 50f), true, position, rotation);
				_floatingScreen.SetRootViewController(_gridViewController, ViewController.AnimationType.None);

				// Make the handle cover the entire screen surface so the user can drag from anywhere.
					var handleRect = _floatingScreen.Handle.GetComponent<RectTransform>();
					if (handleRect != null)
					{
						handleRect.anchorMin = Vector2.zero;
						handleRect.anchorMax = Vector2.one;
						handleRect.offsetMin = Vector2.zero;
						handleRect.offsetMax = Vector2.zero;
						handleRect.localPosition = Vector3.zero;
					}
					else
					{
						_floatingScreen.Handle.transform.localScale = new Vector3(50f, 50f, 1f);
						_floatingScreen.Handle.transform.localPosition = Vector3.zero;
					}

					_floatingScreen.HandleReleased += OnHandleReleased;
					// Apply ShowHandle first so BSML finishes any internal styling before we suppress visuals.
					_floatingScreen.ShowHandle = Settings.Instance.AllowMoving;

					// Disable all existing Images on the handle to remove any visible rendering.
						foreach (var img in _floatingScreen.Handle.GetComponentsInChildren<Image>(true))
							img.enabled = false;
						foreach (var raw in _floatingScreen.Handle.GetComponentsInChildren<RawImage>(true))
							raw.enabled = false;
						foreach (var rnd in _floatingScreen.Handle.GetComponentsInChildren<Renderer>(true))
							rnd.enabled = false;

					var overlayGo = new GameObject("DragOverlay");
					overlayGo.transform.SetParent(_floatingScreen.Handle.transform, false);
					var overlayRect = overlayGo.AddComponent<RectTransform>();
					overlayRect.anchorMin = Vector2.zero;
					overlayRect.anchorMax = Vector2.one;
					overlayRect.offsetMin = Vector2.zero;
					overlayRect.offsetMax = Vector2.zero;
					var overlay = overlayGo.AddComponent<NonDrawingGraphic>();
					overlay.raycastTarget = true;
			_floatingScreen.gameObject.name = "BeatmapScannerScreen";

			_gridViewController.transform.localScale = Vector3.one;
			_gridViewController.transform.localEulerAngles = Vector3.zero;
			_gridViewController.gameObject.SetActive(true);
			_gridViewController.gameObject.GetComponent<VRGraphicRaycaster>().enabled = false;
			_floatingScreen.gameObject.SetActive(false);

            this._platformLeaderboardViewController.didActivateEvent += this.OnLeaderboardActivated;
            this._platformLeaderboardViewController.didDeactivateEvent += this.OnLeaderboardDeactivated;
        }

        public void OnLeaderboardActivated(bool firstactivation, bool addedtohierarchy, bool screensystemenabling)
        {
            if (Settings.Instance.Enabled) _floatingScreen.gameObject.SetActive(true);
        }

        public void OnLeaderboardDeactivated(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            if (Settings.Instance.Enabled) _floatingScreen.gameObject.SetActive(false);
        }

        private void OnHandleReleased(object sender, FloatingScreenHandleEventArgs args)
		{
			if (_floatingScreen.Handle.transform.position.y < 0)
			{
				_floatingScreen.transform.position += new Vector3(0.0f, -_floatingScreen.Handle.transform.position.y + 0.1f, 0.0f);
			}

			Settings.Instance.UIPosition = _floatingScreen.transform.position;
			Settings.Instance.UIRotation = _floatingScreen.transform.rotation;
		}
	}
}
