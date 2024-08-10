using BeatSaberMarkupLanguage.GameplaySetup;
using IPALogger = IPA.Logging.Logger;
using UnityEngine.SceneManagement;
using BeatmapScanner.Installers;
using IPA.Config.Stores;
using System.Reflection;
using SiraUtil.Zenject;
using HarmonyLib;
using IPA;
using BeatmapScanner.UI;
using beatleader_parser;
using beatleader_analyzer;

namespace BeatmapScanner
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Plugin Instance;
        internal static IPALogger Log;
        internal static Harmony harmony;

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
            GameplaySetup.instance.AddTab("BeatmapScanner", "BeatmapScanner.UI.Views.settings.bsml", Settings.Instance, MenuType.All);
        }

        public void OnActiveSceneChanged(Scene prev, Scene next)
        {
            GridViewController.ResetValues();
        }

        [OnDisable]
        public void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            harmony.UnpatchSelf();
            GameplaySetup.instance.RemoveTab("BeatmapScanner");
        }
    }
}
