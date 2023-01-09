using BeatmapScanner.Views;
using UnityEngine;
using Zenject;

namespace BeatmapScanner.Installers
{
    public class BeatmapScannerMenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<BeatmapScannerViewer>().FromNewComponentOn(new GameObject()).AsSingle().NonLazy();
        }
    }
}
