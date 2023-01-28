using BeatSaberMarkupLanguage.GameplaySetup;
using IPALogger = IPA.Logging.Logger;
using BeatmapScanner.HarmonyPatches;
using UnityEngine.SceneManagement;
using BeatmapScanner.Installers;
using IPA.Config.Stores;
using System.Reflection;
using SiraUtil.Zenject;
using HarmonyLib;
using IPA;

namespace BeatmapScanner
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Plugin Instance;
        internal static IPALogger Log;
        internal static Harmony harmony;

        public static class BsmlWrapper
        {
            static readonly bool hasBsml = IPA.Loader.PluginManager.GetPluginFromId("BeatSaberMarkupLanguage") != null;

            public static void EnableUI()
            {
                static void wrap() => GameplaySetup.instance.AddTab("BeatmapScanner", "BeatmapScanner.UI.Views.settings.bsml", Settings.Instance, MenuType.All);

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
        public Plugin(IPALogger logger, IPA.Config.Config conf, Zenjector zenject)
        {
            Instance = this;
            Log = logger;
            Settings.Instance = conf.Generated<Settings>();
            harmony = new Harmony("Loloppe.BeatSaber.BeatmapScanner");

            zenject.UseLogger(logger);
            zenject.Install<MenuInstaller>(Location.Menu);
        }

        [OnEnable]
        public void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            BsmlWrapper.EnableUI();
        }

        public void OnActiveSceneChanged(Scene prev, Scene next)
        {
            BSPatch.ResetValues();
        }

        [OnDisable]
        public void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            harmony.UnpatchSelf();
            BsmlWrapper.DisableUI();
        }
    }
}
