using System;
using FrameUI;
using UnityEngine;

namespace DPSMeter.Windows.ConfigWindow
{
    internal class ConfigWindowDesign
    {
        readonly ConfigWindow _configWindow;
        readonly FigmaWindow _window;
        readonly FigmaFrame root;

        static readonly Color Accent = Helpers.Accent;
        static readonly Color Background = Helpers.Background;
        static readonly Color Primary = Helpers.Primary;
        static readonly Color Text = Helpers.Text;
        static readonly Color Secondary = Helpers.Secondary;


        public ConfigWindowDesign(ConfigWindow configWindow, FigmaWindow window)
        {
            _configWindow = configWindow;
            _window = window;
            root = new FigmaFrame(new Rect(0f, 0f, _window.Rect.width, _window.Rect.height))
            {
                Fill = Background,
                CornerRadius = 10f,
                Layout = FigmaLayout.Vertical,
                Gap = 0f
            };

            // Design starts here.
            var header = root.Add(Helpers.Header(_window, "<cspace=0.3em>DPS METER</cspace> — <cspace=0.15em>Config</cspace>"));

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
            
        }

        private void CloseWindow()
        {
            
            _configWindow.Close();
        }

        private void MinimizeWindow()
        {
            _configWindow.Toggle();
        }

        public void Render()
        {
            root.Render(new Rect(0f, 0f, _window.Rect.width, _window.Rect.height));
        }
    }
}
