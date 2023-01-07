using BeatmapScanner.Installers;
using HarmonyLib;
using HMUI;
using IPA;
using System.Reflection;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace BeatmapScanner
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Plugin Instance;
        internal static IPALogger Log;
        internal static Harmony harmony;
        internal static CurvedTextMeshPro difficulty;

        [Init]
        public Plugin(IPALogger logger, Zenjector zenjector)
        {
            Instance = this;
            Log = logger;
            harmony = new Harmony("Loloppe.BeatSaber.BeatmapScanner");
            zenjector.Install<BeatmapScannerMenuInstaller>(Location.Menu);
        }

        [OnEnable]
        public void OnEnable()
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [OnDisable]
        public void OnDisable()
        {
            harmony.UnpatchSelf();
        }
    }
}
