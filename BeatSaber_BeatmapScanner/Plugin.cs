using HarmonyLib;
using IPA;
using System.Reflection;
using IPALogger = IPA.Logging.Logger;
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

        static class BsmlWrapper
        {
            static readonly bool hasBsml = IPA.Loader.PluginManager.GetPluginFromId("BeatSaberMarkupLanguage") != null;

            public static void EnableUI()
            {
                static void wrap() => GameplaySetup.instance.AddTab("BeatmapScanner", "BeatmapScanner.Views.settings.bsml", Config.Instance, MenuType.All);

                if (hasBsml)
                {
                    wrap();
                }
            }
            public static void DisableUI()
            {
                static void wrap() => GameplaySetup.instance.RemoveTab("BeatmapScanner");

                if (hasBsml)
                {
                    wrap();
                }
            }
        }

        [Init]
        public Plugin(IPALogger logger, IPA.Config.Config conf)
        {
            Instance = this;
            Log = logger;
            Config.Instance = conf.Generated<Config>();
            harmony = new Harmony("Loloppe.BeatSaber.BeatmapScanner");
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
