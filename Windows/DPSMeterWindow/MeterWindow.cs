using DPSMeter.Windows.AdvancedWindow;
using FrameUI;
using UnityEngine;

namespace DPSMeter.Windows.DPSMeterWindow
{
    internal class MeterWindow : MonoBehaviour
    {
        const float DefaultWidth = 1f;
        const float DefaultHeight = 1f;

        FigmaWindow _window;
        private bool Visible { get; set; } = true;
        public bool GlobalVisible { get; set; } = true;

        public void Toggle() => Visible = !Visible;

        public AdvancedBreakdown AdvancedBreakdown { get; private set; }

        public T ReplaceAdvancedBreakdown<T>() where T : AdvancedBreakdown
        {
            Rect? previousRect = null;
            foreach (var existingBreakdown in GetComponents<AdvancedBreakdown>())
            {
                previousRect = existingBreakdown.ScreenRect;
                existingBreakdown.Close();
                Destroy(existingBreakdown);
            }

            var replacement = gameObject.AddComponent<T>();
            if (previousRect.HasValue) replacement.SetInitialRect(previousRect.Value);
            AdvancedBreakdown = replacement;
            return replacement;
        }

        void Update()
        {
            if (Helpers.HotkeyPressed(Plugin.ToggleKey.Value)) { GlobalVisible = !GlobalVisible; Visible = GlobalVisible; }
            if (Helpers.HotkeyPressed(Plugin.ResetKey.Value)) Tracker.Reset();
            if (Helpers.HotkeyPressed(Plugin.StopRecording.Value)) Tracker.StopRecording();
            if (Helpers.HotkeyPressed(Plugin.MinimalistMode.Value)) Plugin.Minimal = !Plugin.Minimal;
        }

        void OnGUI()
        {
            if (!Visible) return;
            if (_window == null)
            {
                Plugin.Logger.LogInfo($"Colour space: {QualitySettings.activeColorSpace}, LinearTextFix: {Plugin.LinearTextFix.Value}");
                _window = new FigmaWindow(
                    918273,
                    new Rect(20f, 200f, DefaultWidth, DefaultHeight),
                    new Vector2(DefaultWidth, DefaultHeight),
                    Draw) { AutoHeight = true, AutoWidth = true };


            }
            FigmaFrame.Wireframe = Plugin.DebugWireframe.Value;
            _window.Render();
        }

        void Draw(int id)
        {
            var root = new MeterWindowDesignV2(this, _window);
            root.Render();
            GUI.DragWindow();
        }

    }
}
