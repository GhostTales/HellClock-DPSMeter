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
        internal static ConfigEntry<bool> AutoStop;
        internal static ConfigEntry<float> IdleTimeout;
        internal static ConfigEntry<bool> AutoArm;
        internal static ConfigEntry<KeyCode> StopRecording;
        internal static ConfigEntry<bool> CapOverkill;
        internal static ConfigEntry<bool> LinearTextFix;
        internal static ConfigEntry<float> UiScale;
        internal static ConfigEntry<bool> DebugWireframe;
        internal static ConfigEntry<bool> LogUniqueHits;
        internal static ConfigEntry<KeyCode> MinimalistMode;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo("DPSMeter starting");
            try
            {
                Initialize();
            }
            catch (System.Exception e)
            {
                Logger.LogError($"DPSMeter failed to initialize: {e}");
            }
        }

        // Kept out of Awake so a missing dependency (e.g. FrameUI.dll) is thrown here and caught above,
        // instead of failing silently while Awake is being JIT-compiled.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void Initialize()
        {
            ToggleKey = Config.Bind("Keys", "Toggle", KeyCode.F9, "Show/hide the meter");
            ResetKey = Config.Bind("Keys", "Reset", KeyCode.F10, "Resets and arms the meter");
            StopRecording = Config.Bind("Meter", "StopRecording", KeyCode.F11, "Stops recording damage");
            MinimalistMode = Config.Bind("Meter", "MinimalistMode", KeyCode.F12, "Toggle minimalist mode for the UI");
            
            AutoStop = Config.Bind("Meter", "AutoStop", true, "Automatically stop the meter when leaving combat, after idle timeout");
            IdleTimeout = Config.Bind("Meter", "IdleTimeout", 5f, "Seconds without dealing damage before the fight timer stops");
            AutoArm = Config.Bind("Meter", "AutoArm", true, "Automatically arm the meter when entering combat");
            

            CapOverkill = Config.Bind("Meter", "CapOverkill", false, "Do not count damage beyond the target's remaining life (off = show true damage)");

            LinearTextFix = Config.Bind("Display", "LinearTextFix", true, "Compensate text colours for the game's linear colour space (turn off if text looks too light)");
            DebugWireframe = Config.Bind("Debug", "Wireframe", false, "Draw the layout as a wireframe (outlines of every frame)");
            UiScale = Config.Bind("Display", "UiScale", 1f, new ConfigDescription(
                "Scales all UI. The UI is designed for 1920x1080 at 1.0; use e.g. 1.33 on 2560x1440 or 2.0 on 3840x2160",
                new AcceptableValueRange<float>(0.25f, 4f)));
            LogUniqueHits = Config.Bind("Debug", "LogUniqueHits", false, "Log the skill, source and icon details the first time each distinct damage source is seen");
            
            FigmaFrame.Wireframe = DebugWireframe.Value;
            Gui.LinearTextFix = LinearTextFix.Value;
            Gui.Scale = UiScale.Value;
            UiScale.SettingChanged += (_, __) => Gui.Scale = UiScale.Value;
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
