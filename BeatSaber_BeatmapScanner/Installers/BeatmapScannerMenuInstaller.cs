using BeatmapScanner.Views;
using Zenject;

namespace BeatmapScanner.Installers
{
    public class BeatmapScannerMenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<ArtworkViewManager>().AsSingle();
        }
    }
}
