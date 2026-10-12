using UnityEngine;

namespace DPSMeter
{
    internal class MeterWindow : MonoBehaviour
    {
        const float DefaultWidth = 459f;
        const float DefaultHeight = 210f;

        FigmaWindow _window;
        bool _visible = true;

        internal static bool IsMouseOver;

        void Update()
        {
            if (_window != null)
            {
                var mouse = Input.mousePosition;
                IsMouseOver = _visible && _window.Contains(new Vector2(mouse.x, Screen.height - mouse.y));
            }

            if (Input.GetKeyDown(Plugin.ToggleKey.Value)) _visible = !_visible;
            if (Input.GetKeyDown(Plugin.ResetKey.Value)) Tracker.Reset();
        }

        void OnGUI()
        {
            if (!_visible) return;
            if (_window == null)
            {
                Plugin.Logger.LogInfo($"Colour space: {QualitySettings.activeColorSpace}, LinearTextFix: {Plugin.LinearTextFix.Value}");
                _window = new FigmaWindow(
                    918273,
                    new Rect(20f, 200f, DefaultWidth, DefaultHeight),
                    new Vector2(DefaultWidth, DefaultHeight),
                    Draw) { AutoHeight = true };
            }

            _window.Render();
        }

        void Draw(int id)
        {
            var root = new MeterWindowDesignV2(_window);
            root.Render();
            GUI.DragWindow();
        }

    }
}
