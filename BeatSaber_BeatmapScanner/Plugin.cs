﻿using BeatmapScanner.Installers;
using HarmonyLib;
using HMUI;
using IPA;
using System.Reflection;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;
using BeatSaberMarkupLanguage.Settings;
using IPA.Config.Stores;
using UnityEngine.SceneManagement;

namespace BeatmapScanner
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Plugin Instance;
        internal static IPALogger Log;
        internal static Harmony harmony;
        internal static CurvedTextMeshPro ui;
        internal static string difficulty;
        internal static string tech;
        internal static bool inGame = false;

        static class BsmlWrapper
        {
            static readonly bool hasBsml = IPA.Loader.PluginManager.GetPluginFromId("BeatSaberMarkupLanguage") != null;

            public static void EnableUI()
            {
                void wrap() => BSMLSettings.instance.AddSettingsMenu("BeatmapScanner", "BeatmapScanner.Views.settings.bsml", Config.Instance);

                if (hasBsml)
                {
                    wrap();
                }
            }
            public static void DisableUI()
            {
                void wrap() => BSMLSettings.instance.RemoveSettingsMenu(Config.Instance);

                if (hasBsml)
                {
                    wrap();
                }
            }
        }

        public static void SetUI()
        {
            ui.text = difficulty + tech;
        }

        public static void ClearUI()
        {
            difficulty = "";
            tech = "";
            ui.text = "";
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
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            BsmlWrapper.EnableUI();
        }

        public void OnActiveSceneChanged(Scene prev, Scene next)
        {
            if (BS_Utils.SceneNames.Game == next.name)
            {
                inGame = true;
            }
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
