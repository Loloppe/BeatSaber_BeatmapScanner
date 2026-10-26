using BeatSaberMarkupLanguage.FloatingScreen;
using System;
using System.Linq;
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
		private readonly PlatformLeaderboardViewController _platformLeaderboardViewController;

		public static FloatingScreen _floatingScreen = null;
		public static FloatingScreen _swingDiffScreen = null;
		public static FloatingScreen _swingTechScreen = null;
		public static SwingGraphViewController SwingDiffGraph = null;
		public static SwingGraphViewController SwingTechGraph = null;

		/// <summary>True only while a custom level is selected and screens should be visible.</summary>
		public static bool CustomLevelActive = false;

		private static Material _noGlowMaterial;
		/// <summary>Beat Saber's UINoGlow material, which is not washed out by the Bloom Post Process effect.</summary>
		public static Material NoGlowMaterial
		{
			get
			{
				if (_noGlowMaterial == null)
					_noGlowMaterial = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name == "UINoGlow");
				return _noGlowMaterial;
			}
		}

		public UICreator(GridViewController gridViewController, [Inject(Id = "SwingDiff")] SwingGraphViewController swingDiffViewController, [Inject(Id = "SwingTech")] SwingGraphViewController swingTechViewController, PlatformLeaderboardViewController platformLeaderboardViewController)
		{
			_gridViewController = gridViewController;
			_swingDiffViewController = swingDiffViewController;
			_swingTechViewController = swingTechViewController;
			_platformLeaderboardViewController = platformLeaderboardViewController;
		}

		public void CreateFloatingScreen(Vector3 position, Quaternion rotation)
		{
			_floatingScreen = CreateScreen(new Vector2(50f, 50f), position, rotation, _gridViewController, "BeatmapScannerScreen",
				(pos, rot) => { Settings.Instance.UIPosition = pos; Settings.Instance.UIRotation = rot; });

			_swingDiffViewController.Initialize("Swing Diff", new Color32(100, 220, 130, 255), new Color32(60, 160, 90, 60));
			_swingDiffScreen = CreateScreen(new Vector2(80f, 50f), Settings.Instance.SwingDiffGraphPosition, Settings.Instance.SwingDiffGraphRotation, _swingDiffViewController, "SwingDiffGraphScreen",
				(pos, rot) => { Settings.Instance.SwingDiffGraphPosition = pos; Settings.Instance.SwingDiffGraphRotation = rot; });
			ApplyNoGlowToScreen(_swingDiffScreen);
			SwingDiffGraph = _swingDiffViewController;

			_swingTechViewController.Initialize("Swing Tech", new Color32(255, 180, 60, 255), new Color32(180, 110, 30, 60));
			_swingTechScreen = CreateScreen(new Vector2(80f, 50f), Settings.Instance.SwingTechGraphPosition, Settings.Instance.SwingTechGraphRotation, _swingTechViewController, "SwingTechGraphScreen",
				(pos, rot) => { Settings.Instance.SwingTechGraphPosition = pos; Settings.Instance.SwingTechGraphRotation = rot; });
			ApplyNoGlowToScreen(_swingTechScreen);
			SwingTechGraph = _swingTechViewController;

			_platformLeaderboardViewController.didActivateEvent += OnLeaderboardActivated;
			_platformLeaderboardViewController.didDeactivateEvent += OnLeaderboardDeactivated;
		}

		/// <summary>
		/// Creates a hidden, draggable floating screen hosting <paramref name="viewController"/>.
		/// <paramref name="savePose"/> is called with the screen's position and rotation whenever it is dropped.
		/// </summary>
		private static FloatingScreen CreateScreen(Vector2 size, Vector3 position, Quaternion rotation, ViewController viewController, string name, Action<Vector3, Quaternion> savePose)
		{
			var screen = FloatingScreen.CreateFloatingScreen(size, true, position, rotation);
			screen.SetRootViewController(viewController, ViewController.AnimationType.None);
			SetupHandle(screen, Settings.Instance.AllowMoving);
			screen.HandleReleased += (sender, args) =>
			{
				// Keep the screen above the floor
				float handleY = screen.Handle.transform.position.y;
				if (handleY < 0)
					screen.transform.position += new Vector3(0f, -handleY + 0.1f, 0f);
				savePose(screen.transform.position, screen.transform.rotation);
			};
			screen.gameObject.name = name;

			viewController.transform.localScale = Vector3.one;
			viewController.transform.localEulerAngles = Vector3.zero;
			viewController.gameObject.SetActive(true);
			viewController.gameObject.GetComponent<VRGraphicRaycaster>().enabled = false;
			screen.gameObject.SetActive(false);
			return screen;
		}

		/// <summary>Assigns <see cref="NoGlowMaterial"/> to every Image/RawImage on the screen.</summary>
		private static void ApplyNoGlowToScreen(FloatingScreen screen)
		{
			var noGlowMat = NoGlowMaterial;
			if (noGlowMat == null) return;
			foreach (var img in screen.GetComponentsInChildren<Image>(true))
				img.material = noGlowMat;
			foreach (var raw in screen.GetComponentsInChildren<RawImage>(true))
				raw.material = noGlowMat;
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
			overlay.raycastTarget = allowMoving;
		}

		/// <summary>Toggles a floating screen's handle visibility and its drag-overlay raycast target together.</summary>
		private static void ApplyMovingState(FloatingScreen screen, bool allowMoving)
		{
			if (screen == null) return;
			screen.ShowHandle = allowMoving;
			var overlay = screen.Handle.GetComponentInChildren<NonDrawingGraphic>();
			if (overlay != null) overlay.raycastTarget = allowMoving;
		}

		/// <summary>Re-applies visibility and moving state to all screens. Called whenever the settings change.</summary>
		public static void ApplySettings()
		{
			if (_floatingScreen == null) return;
			ShowScreens(CustomLevelActive);
			ApplyMovingState(_floatingScreen, Settings.Instance.AllowMoving);
			ApplyMovingState(_swingDiffScreen, Settings.Instance.AllowMoving);
			ApplyMovingState(_swingTechScreen, Settings.Instance.AllowMoving);
			GridViewController.ApplyVisibility();
		}

		/// <summary>Shows or hides the floating screens according to the per-screen settings and the Enabled flag.</summary>
		public static void ShowScreens(bool visible)
		{
			CustomLevelActive = visible;
			bool show = visible && Settings.Instance.Enabled;
			SetScreenActive(_floatingScreen, show && Settings.Instance.ShowDataScreen);
			SetScreenActive(_swingDiffScreen, show && Settings.Instance.ShowPassGraph);
			SetScreenActive(_swingTechScreen, show && Settings.Instance.ShowTechGraph);
		}

		private static void SetScreenActive(FloatingScreen screen, bool active)
		{
			if (screen != null) screen.gameObject.SetActive(active);
		}

		private void OnLeaderboardActivated(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			if (CustomLevelActive) ShowScreens(true);
		}

		private void OnLeaderboardDeactivated(bool removedFromHierarchy, bool screenSystemDisabling)
		{
			SetScreenActive(_floatingScreen, false);
			SetScreenActive(_swingDiffScreen, false);
			SetScreenActive(_swingTechScreen, false);
		}
	}
}
