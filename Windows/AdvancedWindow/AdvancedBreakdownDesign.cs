using System;
using FrameUI;
using UnityEngine;

namespace DPSMeter.Windows.AdvancedWindow
{
    internal class AdvancedBreakdownDesign
    {
        readonly AdvancedBreakdown _advancedBreakdown;
        readonly FigmaWindow _window;
        readonly FigmaFrame root;

        static readonly Color Accent = Helpers.Accent;
        static readonly Color Background = Helpers.Background;
        static readonly Color Primary = Helpers.Primary;
        static readonly Color Text = Helpers.Text;
        static readonly Color Secondary = Helpers.Secondary;


        public AdvancedBreakdownDesign(AdvancedBreakdown advancedBreakdown, FigmaWindow window)
        {
            _advancedBreakdown = advancedBreakdown;
            _window = window;
            root = new FigmaFrame(new Rect(0f, 0f, _window.Rect.width, _window.Rect.height))
            {
                Fill = Background,
                CornerRadius = 10f,
                Layout = FigmaLayout.Vertical,
                Gap = 0f
            };

            // Design starts here.
            var header = root.Add(Helpers.Header(_window, "<cspace=0.3em>DPS METER — </cspace><cspace=0.15em>Advanced Breakdown</cspace>"));

            // Button row
            var buttonRow = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.Center,
                Gap = 4f
            }.Size(FigmaSize.Hug, FigmaSize.Hug);

            buttonRow.Add(Helpers.IconButton("MinimizeIcon.png", 24f, MinimizeWindow));
            buttonRow.Add(Helpers.IconButton("CloseIcon.png", 24f, CloseWindow));

            header.Add(buttonRow);

            // var linkTest = root.Add(new FigmaText(default, "Wiki", 14, Primary)
            // {
            //     Clicked = () => Application.OpenURL("https://hellclock.wiki.gg/"),
            //     HoverTint = Color.white * 1.2f,
            //     PressedTint = new Color(0.55f, 0.55f, 0.55f, 1f)
            // }.Size(FigmaSize.Hug, FigmaSize.Hug));

            var content = root.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Gap = 8f,
                Padding = new Vector4(10, 10, 10, 10)
            }.Size(FigmaSize.Fill, FigmaSize.Hug));

            content.Add(new FigmaText(default, "Advanced breakdown is under construction.", 20, Primary){}.Size(FigmaSize.Hug, FigmaSize.Hug));
            content.Add(new FigmaText(default, "It will be in the next update.", 20, Primary){}.Size(FigmaSize.Hug, FigmaSize.Hug));

            
        }

        private void CloseWindow()
        {
            
            _advancedBreakdown.Close();
        }

        private void MinimizeWindow()
        {
            _advancedBreakdown.Toggle();
        }

        public void Render()
        {
            root.Render(new Rect(0f, 0f, _window.Rect.width, _window.Rect.height));
        }
    }
}
