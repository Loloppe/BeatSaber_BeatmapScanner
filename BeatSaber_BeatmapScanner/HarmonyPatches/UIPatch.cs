using SiraUtil.Affinity;
using BeatmapScanner.UI;

namespace BeatmapScanner.HarmonyPatches
{
	internal class UIPatch : IAffinity
	{
		private readonly UICreator _uiCreator;
		private bool FirstRun = true;

        public UIPatch(UICreator uiCreator) => _uiCreator = uiCreator;

        [AffinityPostfix]
		[AffinityPatch(typeof(MainMenuViewController), "DidActivate")]
		internal void Postfix()
		{
			if(FirstRun)
            {
                _uiCreator.CreateFloatingScreen(Settings.Instance.UIPosition, Settings.Instance.UIRotation);
                FirstRun = false;
			}
		}
	}
}
