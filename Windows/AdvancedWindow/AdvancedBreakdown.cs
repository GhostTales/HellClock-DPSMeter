using FrameUI;
using UnityEngine;

namespace DPSMeter.Windows.AdvancedWindow
{
    internal class AdvancedBreakdown : MonoBehaviour
    {
        const float DefaultWidth = 459f;
        const float DefaultHeight = 400f;

        FigmaWindow _window;
        Rect _initialRect = new Rect(500f, 200f, DefaultWidth, DefaultHeight);

        public bool GlobalVisible { get; set; } = true;

        private bool Visible { get; set; } = true;

        public void Toggle() => Visible = !Visible;

        public Rect ScreenRect => _window != null ? _window.Rect : _initialRect;

        public void SetInitialRect(Rect rect)
        {
            _initialRect = rect;
        }

        public void Close()
        {
            Visible = false;
            enabled = false;
            if (_window != null)
            {
                _initialRect = _window.Rect;
                _window.Destroy();
                _window = null;
            }
        }


        void OnDestroy()
        {
            Close();
        }

        void OnGUI()
        {
            if (!Visible) return;
            if (_window == null)
            {
                _window = new FigmaWindow(
                    918274,
                    _initialRect,
                    new Vector2(DefaultWidth, DefaultHeight),
                    Draw);
            }

            _window.Render();
        }

        void Update()
        {
            if (Input.GetKeyDown(Plugin.ToggleKey.Value)) { GlobalVisible = !GlobalVisible; Visible = GlobalVisible; }
        }

        void Draw(int id)
        {
            var design = new AdvancedBreakdownDesign(this, _window);
            design.Render();
            GUI.DragWindow();
        }
        
    }
}
