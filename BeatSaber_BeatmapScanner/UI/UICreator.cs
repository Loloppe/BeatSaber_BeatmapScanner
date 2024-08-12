using BeatSaberMarkupLanguage.FloatingScreen;
using UnityEngine;
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
			_floatingScreen.HandleSide = FloatingScreen.Side.Bottom;
			_floatingScreen.HighlightHandle = true;
			_floatingScreen.handle.transform.localScale = Vector3.one * 5.0f;
			_floatingScreen.handle.transform.localPosition = new Vector3(0.0f, -12f, 0.0f);
			_floatingScreen.HandleReleased += OnHandleReleased;
			_floatingScreen.ShowHandle = Settings.Instance.ShowHandle;
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
			if (_floatingScreen.handle.transform.position.y < 0)
			{
				_floatingScreen.transform.position += new Vector3(0.0f, -_floatingScreen.handle.transform.position.y + 0.1f, 0.0f);
			}

			Settings.Instance.UIPosition = _floatingScreen.transform.position;
			Settings.Instance.UIRotation = _floatingScreen.transform.rotation;
		}
	}
}
