using BeatmapScanner.HarmonyPatches;
using BeatmapScanner.UI;
using Zenject;

namespace BeatmapScanner.Installers
{
    internal class MenuInstaller : Installer<MenuInstaller>
	{
		public override void InstallBindings()
		{
			Container.Bind<GridViewController>().FromNewComponentAsViewController().AsSingle();
			Container.Bind<UICreator>().AsSingle();
			Container.BindInterfacesTo<UIPatch>().AsSingle();
		}
	}
}
