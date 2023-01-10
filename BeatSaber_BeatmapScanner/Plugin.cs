using BeatmapScanner.Installers;
using HarmonyLib;
using HMUI;
using IPA;
using System.Reflection;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;
using BeatSaberMarkupLanguage.Settings;
using IPA.Config.Stores;
using BeatSaberMarkupLanguage.GameplaySetup;

namespace BeatmapScanner
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Plugin Instance;
        internal static IPALogger Log;
        internal static Harmony harmony;
        internal static CurvedTextMeshPro star;
        internal static CurvedTextMeshPro difficulty;
        internal static CurvedTextMeshPro t;
        internal static CurvedTextMeshPro tech;
        internal static CurvedTextMeshPro i;
        internal static CurvedTextMeshPro intensity;
        internal static CurvedTextMeshPro m;
        internal static CurvedTextMeshPro movement;

        static class BsmlWrapper
        {
            static readonly bool hasBsml = IPA.Loader.PluginManager.GetPluginFromId("BeatSaberMarkupLanguage") != null;

            public static void EnableUI()
            {
                static void wrap() => BSMLSettings.instance.AddSettingsMenu("BeatmapScanner", "BeatmapScanner.Views.settings.bsml", Config.Instance);
                void wrap2() => GameplaySetup.instance.AddTab("BeatmapScanner", "BeatmapScanner.Views.settings.bsml", Config.Instance, MenuType.All);

                if (hasBsml)
                {
                    wrap();
                    wrap2();
                }
            }
            public static void DisableUI()
            {
                static void wrap() => BSMLSettings.instance.RemoveSettingsMenu(Config.Instance);
                void wrap2() => GameplaySetup.instance.RemoveTab("BeatmapScanner");

                if (hasBsml)
                {
                    wrap();
                    wrap2();
                }
            }
        }

        public static void ClearUI()
        {
            difficulty.text = "";
            tech.text = "";
            intensity.text = "";
            movement.text = "";
        }

        [Init]
        public Plugin(IPALogger logger, IPA.Config.Config conf, Zenjector zenjector)
        {
            Instance = this;
            Log = logger;
            Config.Instance = conf.Generated<Config>();
            harmony = new Harmony("Loloppe.BeatSaber.BeatmapScanner");
            zenjector.Install<BeatmapScannerMenuInstaller>(Location.Menu);
        }

        [OnEnable]
        public void OnEnable()
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            BsmlWrapper.EnableUI();
        }

        [OnDisable]
        public void OnDisable()
        {
            harmony.UnpatchSelf();
            BsmlWrapper.DisableUI();
        }
    }
}
