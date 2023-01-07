using BeatmapScanner.Views;
using Zenject;

namespace BeatmapScanner.Installers
{
    public class BeatmapScannerMenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<BeatmapScannerViewer>().FromNewComponentOn(new UnityEngine.GameObject()).AsSingle().NonLazy();
        }
    }
}
