using BeatSaberMarkupLanguage.GameplaySetup;
using IPALogger = IPA.Logging.Logger;
using BeatmapScanner.Installers;
using BeatmapScanner.Utils;
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
        internal static IPALogger Log;
        private static Harmony _harmony;

        [Init]
        public Plugin(IPALogger logger, IPA.Config.Config conf, Zenjector zenject)
        {
            Log = logger;
            Settings.Instance = conf.Generated<Settings>();
            AnalysisCache.Initialize();
            _harmony = new Harmony("Loloppe.BeatSaber.BeatmapScanner");

            zenject.UseLogger(logger);
            zenject.Install<MenuInstaller>(Location.Menu);
        }

        [OnEnable]
        public void OnEnable()
        {
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            BeatSaberMarkupLanguage.Util.MainMenuAwaiter.MainMenuInitializing += MainMenuInit;
        }

        public void MainMenuInit()
        {
            GameplaySetup.Instance.AddTab("BeatmapScanner", "BeatmapScanner.UI.Views.settings.bsml", Settings.Instance, MenuType.All);
        }

        [OnDisable]
        public void OnDisable()
        {
            _harmony.UnpatchSelf();
            BeatSaberMarkupLanguage.Util.MainMenuAwaiter.MainMenuInitializing -= MainMenuInit;
            GameplaySetup.Instance.RemoveTab("BeatmapScanner");
        }
    }
}
