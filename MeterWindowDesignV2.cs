using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace DPSMeter
{
    internal class MeterWindowDesignV2
    {
        internal static bool IsMouseOver;
        readonly FigmaWindow _window;
        FigmaFrame root;

        readonly Color Accent = new Color(0x15 / 255f, 0x14 / 255f, 0x19 / 255f);
        readonly Color Background = new Color(0x17 / 255f, 0x16 / 255f, 0x1D / 255f);
        readonly Color Primary = new Color(0xF6 / 255f, 0x4C / 255f, 0x2C / 255f);
        readonly Color Text = new Color(0xff / 255f, 0xff / 255f, 0xff / 255f);
        readonly Color Border = new Color(0x33 / 255f, 0x33 / 255f, 0x33 / 255f);

        public MeterWindowDesignV2(FigmaWindow window)
        {
            _window = window;
            root = new FigmaFrame(new Rect(0f, 0f, _window.Rect.width, _window.Rect.height))
            {
                Fill = Background,
                CornerRadius = 10f,
                Layout = FigmaLayout.Vertical,
                Gap = 0f
            };

            
            var header = root.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                Fill = Accent,
                MainAlign = FigmaAlign.SpaceBetween,   // title left, buttons right
                CrossAlign = FigmaAlign.Center,        // vertically centred
                CornerRadii = new Vector4(10f, 10f, 0f, 0f),
                Padding = new Vector4(10f, 0f, 10f, 0f),
                Bounds = new Rect(0f, 0f, _window.Rect.width, 40f),
            }.Size(FigmaSize.Fill, FigmaSize.Fixed));

            // Header title
            var title = header.Add(new FigmaText(default, "DPS METER", 18, Gui.Primary).Size(FigmaSize.Hug, FigmaSize.Hug));

            var mainBody = root.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                Fill = Background,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Start,
                Padding = new Vector4(10f, 10f, 10f, 10f),
            }.Size(FigmaSize.Fill, FigmaSize.Fill));

            // Button row
            var buttonRow = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                Gap = 4f
            }.Size(FigmaSize.Hug, FigmaSize.Hug);

            // Debug button toggles the wireframe mode on and off
            // config gear button
            var configButton = new FigmaButton(default, FigmaFrame.Wireframe ? "Debug: On" : "Debug: Off",
                FigmaFrame.Wireframe ? Gui.ButtonKind.Primary : Gui.ButtonKind.Secondary, 14, ToggleWireframe).Size(FigmaSize.Hug, FigmaSize.Hug);
            buttonRow.Add(configButton);

            header.Add(buttonRow);
            // header.Add(new FigmaText(default, Tracker.Dps.ToString("N0") + " DPS", HeaderStyle(TextAnchor.MiddleCenter, Gui.Text)).Size(FigmaSize.Fill, FigmaSize.Fill));
            // var reset = header.Add(new FigmaButton(default, "Reset", Gui.ButtonKind.Secondary, 14, Tracker.Reset).Size(FigmaSize.Hug, FigmaSize.Hug));
            // reset.CellAlignY = FigmaAlign.Center;
        }

        static void ToggleWireframe()
        {
            FigmaFrame.Wireframe = !FigmaFrame.Wireframe;
            Plugin.DebugWireframe.Value = FigmaFrame.Wireframe;
        }
        
        public void Render()
        {
            root.Render(new Rect(0f, 0f, _window.Rect.width, _window.Rect.height));
        }

    }
}