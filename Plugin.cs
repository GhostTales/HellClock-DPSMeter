using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using DPSMeter.Windows.DPSMeterWindow;
using FrameUI;
using HarmonyLib;
using UnityEngine;

namespace DPSMeter
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        internal static ConfigEntry<KeyCode> ToggleKey;
        internal static ConfigEntry<KeyCode> ResetKey;
        internal static ConfigEntry<float> IdleTimeout;
        internal static ConfigEntry<bool> CapOverkill;
        internal static ConfigEntry<bool> LinearTextFix;
        internal static ConfigEntry<bool> DebugWireframe;
        internal static ConfigEntry<bool> LogUniqueHits;

        private void Awake()
        {
            Logger = base.Logger;
            ToggleKey = Config.Bind("Keys", "Toggle", KeyCode.F8, "Show/hide the meter");
            ResetKey = Config.Bind("Keys", "Reset", KeyCode.F9, "Reset the meter");
            IdleTimeout = Config.Bind("Meter", "IdleTimeout", 5f, "Seconds without dealing damage before the fight timer stops");
            CapOverkill = Config.Bind("Meter", "CapOverkill", false, "Do not count damage beyond the target's remaining life (off = show true damage)");

            LinearTextFix = Config.Bind("Display", "LinearTextFix", true, "Compensate text colours for the game's linear colour space (turn off if text looks too light)");
            DebugWireframe = Config.Bind("Debug", "Wireframe", false, "Draw the layout as a wireframe (outlines of every frame)");
            LogUniqueHits = Config.Bind("Debug", "LogUniqueHits", true, "Log the skill, source and icon details the first time each distinct damage source is seen");
            FigmaFrame.Wireframe = DebugWireframe.Value;
            Gui.LinearTextFix = LinearTextFix.Value;
            Gui.Warn = Logger.LogWarning;
            try
            {
                BundledFont.Load();
                FigmaText.DefaultFont = BundledFont.Light;
            }
            catch (System.Exception e)
            {
                Logger.LogError($"Could not load the bundled font, using the default font: {e}");
            }

            var go = new GameObject("DPSMeter");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<MeterWindow>();

            new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll();
            Logger.LogInfo("DPSMeter loaded");
        }
    }
}
