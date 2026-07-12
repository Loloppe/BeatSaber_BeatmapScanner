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
		private readonly SwingGraphViewController _swingDiffViewController;
		private readonly SwingGraphViewController _swingTechViewController;

		public static FloatingScreen _floatingScreen = null;
			public static FloatingScreen _swingDiffScreen = null;
			public static FloatingScreen _swingTechScreen = null;
			public static SwingGraphViewController SwingDiffGraph = null;
			public static SwingGraphViewController SwingTechGraph = null;

			/// <summary>True only while a custom level is selected and screens should be visible.</summary>
			public static bool CustomLevelActive = false;

		public UICreator(GridViewController gridViewController, [Inject(Id = "SwingDiff")] SwingGraphViewController swingDiffViewController, [Inject(Id = "SwingTech")] SwingGraphViewController swingTechViewController)
		{
			_gridViewController = gridViewController;
			_swingDiffViewController = swingDiffViewController;
			_swingTechViewController = swingTechViewController;
		}

		[Inject]
		private PlatformLeaderboardViewController _platformLeaderboardViewController;

		public void CreateFloatingScreen(Vector3 position, Quaternion rotation)
		{
			_floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(50f, 50f), true, position, rotation);
				_floatingScreen.SetRootViewController(_gridViewController, ViewController.AnimationType.None);
				SetupHandle(_floatingScreen, Settings.Instance.AllowMoving);
				_floatingScreen.HandleReleased += OnHandleReleased;
			_floatingScreen.gameObject.name = "BeatmapScannerScreen";

			_gridViewController.transform.localScale = Vector3.one;
			_gridViewController.transform.localEulerAngles = Vector3.zero;
			_gridViewController.gameObject.SetActive(true);
			_gridViewController.gameObject.GetComponent<VRGraphicRaycaster>().enabled = false;
			_floatingScreen.gameObject.SetActive(false);

			// Swing diff graph screen
			_swingDiffScreen = FloatingScreen.CreateFloatingScreen(new Vector2(80f, 50f), true, Settings.Instance.SwingDiffGraphPosition, Settings.Instance.SwingDiffGraphRotation);
			_swingDiffScreen.SetRootViewController(_swingDiffViewController, ViewController.AnimationType.None);
			SetupHandle(_swingDiffScreen, Settings.Instance.AllowMoving);
			_swingDiffScreen.HandleReleased += OnSwingDiffHandleReleased;
			_swingDiffScreen.gameObject.name = "SwingDiffGraphScreen";
			_swingDiffViewController.Initialize("Swing Diff",
				new UnityEngine.Color32(100, 220, 130, 255),
				new UnityEngine.Color32(60, 160, 90, 60));
			SwingDiffGraph = _swingDiffViewController;
			_swingDiffViewController.transform.localScale = Vector3.one;
			_swingDiffViewController.transform.localEulerAngles = Vector3.zero;
			_swingDiffViewController.gameObject.SetActive(true);
			_swingDiffViewController.gameObject.GetComponent<VRGraphicRaycaster>().enabled = false;
			_swingDiffScreen.gameObject.SetActive(false);

			// Swing tech graph screen
			_swingTechScreen = FloatingScreen.CreateFloatingScreen(new Vector2(80f, 50f), true, Settings.Instance.SwingTechGraphPosition, Settings.Instance.SwingTechGraphRotation);
			_swingTechScreen.SetRootViewController(_swingTechViewController, ViewController.AnimationType.None);
			SetupHandle(_swingTechScreen, Settings.Instance.AllowMoving);
			_swingTechScreen.HandleReleased += OnSwingTechHandleReleased;
			_swingTechScreen.gameObject.name = "SwingTechGraphScreen";
			_swingTechViewController.Initialize("Swing Tech",
				new UnityEngine.Color32(255, 180, 60, 255),
				new UnityEngine.Color32(180, 110, 30, 60));
			SwingTechGraph = _swingTechViewController;
			_swingTechViewController.transform.localScale = Vector3.one;
			_swingTechViewController.transform.localEulerAngles = Vector3.zero;
			_swingTechViewController.gameObject.SetActive(true);
			_swingTechViewController.gameObject.GetComponent<VRGraphicRaycaster>().enabled = false;
			_swingTechScreen.gameObject.SetActive(false);

			this._platformLeaderboardViewController.didActivateEvent += this.OnLeaderboardActivated;
			this._platformLeaderboardViewController.didDeactivateEvent += this.OnLeaderboardDeactivated;
		}

		/// <summary>
		/// Configures the floating screen handle to be invisible but cover the whole surface for drag-anywhere support.
		/// </summary>
		private static void SetupHandle(FloatingScreen screen, bool allowMoving)
		{
			var handleRect = screen.Handle.GetComponent<RectTransform>();
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
				screen.Handle.transform.localScale = new Vector3(50f, 50f, 1f);
				screen.Handle.transform.localPosition = Vector3.zero;
			}

			screen.ShowHandle = allowMoving;

			foreach (var img in screen.Handle.GetComponentsInChildren<Image>(true))
				img.enabled = false;
			foreach (var raw in screen.Handle.GetComponentsInChildren<RawImage>(true))
				raw.enabled = false;
			foreach (var rnd in screen.Handle.GetComponentsInChildren<Renderer>(true))
				rnd.enabled = false;

			var overlayGo = new GameObject("DragOverlay");
			overlayGo.transform.SetParent(screen.Handle.transform, false);
			var overlayRect = overlayGo.AddComponent<RectTransform>();
			overlayRect.anchorMin = Vector2.zero;
			overlayRect.anchorMax = Vector2.one;
			overlayRect.offsetMin = Vector2.zero;
			overlayRect.offsetMax = Vector2.zero;
			var overlay = overlayGo.AddComponent<NonDrawingGraphic>();
			overlay.raycastTarget = true;
		}

		/// <summary>Toggles a floating screen's handle visibility and its drag-overlay raycast target together.</summary>
		public static void ApplyMovingState(FloatingScreen screen, bool allowMoving)
		{
			if (screen == null) return;
			screen.ShowHandle = allowMoving;
			var overlay = screen.Handle.GetComponentInChildren<NonDrawingGraphic>();
			if (overlay != null) overlay.raycastTarget = allowMoving;
		}

		/// <summary>Shows or hides the swing graph screens according to current settings.</summary>
		public static void ApplySwingGraphVisibility()
		{
			if (Settings.Instance == null) return;
			// Only force-hide; showing is handled by ShowScreens when data is available.
			if (!Settings.Instance.Enabled || !Settings.Instance.ShowPassGraph)
				if (_swingDiffScreen != null) _swingDiffScreen.gameObject.SetActive(false);
			if (!Settings.Instance.Enabled || !Settings.Instance.ShowTechGraph)
				if (_swingTechScreen != null) _swingTechScreen.gameObject.SetActive(false);
			ApplyMovingState(_swingDiffScreen, Settings.Instance.AllowMoving);
			ApplyMovingState(_swingTechScreen, Settings.Instance.AllowMoving);
		}

		/// <summary>Shows or hides the floating screens according to the per-screen settings and the Enabled flag.</summary>
		public static void ShowScreens(bool visible)
		{
			CustomLevelActive = visible;
			if (_floatingScreen != null)
				_floatingScreen.gameObject.SetActive(visible && Settings.Instance.Enabled && Settings.Instance.ShowDataScreen);
			if (_swingDiffScreen != null)
				_swingDiffScreen.gameObject.SetActive(visible && Settings.Instance.Enabled && Settings.Instance.ShowPassGraph);
			if (_swingTechScreen != null)
				_swingTechScreen.gameObject.SetActive(visible && Settings.Instance.Enabled && Settings.Instance.ShowTechGraph);
		}

		public void OnLeaderboardActivated(bool firstactivation, bool addedtohierarchy, bool screensystemenabling)
		{
			if (!CustomLevelActive) return;
			if (Settings.Instance.Enabled && Settings.Instance.ShowDataScreen)
				_floatingScreen?.gameObject.SetActive(true);
			if (Settings.Instance.Enabled && Settings.Instance.ShowPassGraph)
				_swingDiffScreen?.gameObject.SetActive(true);
			if (Settings.Instance.Enabled && Settings.Instance.ShowTechGraph)
				_swingTechScreen?.gameObject.SetActive(true);
		}

		public void OnLeaderboardDeactivated(bool removedFromHierarchy, bool screenSystemDisabling)
		{
			_floatingScreen.gameObject.SetActive(false);
			if (_swingDiffScreen != null) _swingDiffScreen.gameObject.SetActive(false);
			if (_swingTechScreen != null) _swingTechScreen.gameObject.SetActive(false);
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

		private void OnSwingDiffHandleReleased(object sender, FloatingScreenHandleEventArgs args)
		{
			if (_swingDiffScreen.Handle.transform.position.y < 0)
				_swingDiffScreen.transform.position += new Vector3(0f, -_swingDiffScreen.Handle.transform.position.y + 0.1f, 0f);

			Settings.Instance.SwingDiffGraphPosition = _swingDiffScreen.transform.position;
			Settings.Instance.SwingDiffGraphRotation = _swingDiffScreen.transform.rotation;
		}

		private void OnSwingTechHandleReleased(object sender, FloatingScreenHandleEventArgs args)
		{
			if (_swingTechScreen.Handle.transform.position.y < 0)
				_swingTechScreen.transform.position += new Vector3(0f, -_swingTechScreen.Handle.transform.position.y + 0.1f, 0f);

			Settings.Instance.SwingTechGraphPosition = _swingTechScreen.transform.position;
			Settings.Instance.SwingTechGraphRotation = _swingTechScreen.transform.rotation;
		}
	}
}
