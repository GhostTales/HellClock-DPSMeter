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
        readonly Color Text = new Color(0xFF / 255f, 0xE9 / 255f, 0xE9 / 255f);
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
            var title = header.Add(new FigmaText(default, "DPS METER", 16, Primary).Size(FigmaSize.Hug, FigmaSize.Hug));

            // Button row
            var buttonRow = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.Center,
                Gap = 4f
            }.Size(FigmaSize.Hug, FigmaSize.Hug);

            // Debug button toggles the wireframe mode on and off
            var debugButton = new FigmaButton(default, FigmaFrame.Wireframe ? "Debug: On" : "Debug: Off",
                FigmaFrame.Wireframe ? Gui.ButtonKind.Primary : Gui.ButtonKind.Secondary, 14, ToggleWireframe).Size(FigmaSize.Hug, FigmaSize.Hug);
            buttonRow.Add(debugButton);

            // Config gear button
            var configButton = new FigmaButton(default, Gui.Icon("GearIcon.png"), Gui.ButtonKind.None, 14, OpenConfig)
            {
                IconSize = 24f,
            }.Size(FigmaSize.Hug, FigmaSize.Hug);
            buttonRow.Add(configButton);

            // Minimize button
            var minimizeButton = new FigmaButton(default, Gui.Icon("MinimizeIcon.png"), Gui.ButtonKind.None, 14, MinimizeWindow)
            {
                IconSize = 24f,
            }.Size(FigmaSize.Hug, FigmaSize.Hug);
            buttonRow.Add(minimizeButton);

            // Close button
            var closeButton = new FigmaButton(default, Gui.Icon("CloseIcon.png"), Gui.ButtonKind.None, 14, CloseWindow)
            {
                IconSize = 24f,
            }.Size(FigmaSize.Hug, FigmaSize.Hug);
            buttonRow.Add(closeButton);

            header.Add(buttonRow);


            var mainBody = root.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                Fill = Background,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Start,
                CornerRadii = new Vector4(0f, 0f, 10f, 10f),
                Padding = new Vector4(0f, 10f, 0f, 10f),
                Gap = 10f,
            }.Size(FigmaSize.Fill, FigmaSize.Fill));

            var mainBodyLeft = mainBody.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                Fill = Background,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Gap = 20f,
                Padding = new Vector4(10f, 0f, 0f, 0f),
                Bounds = new Rect(0f, 0f, 266f, _window.Rect.height),
            }.Size(FigmaSize.Fixed, FigmaSize.Fill));

            // DPS Readout frame
            var primaryReadout = mainBodyLeft.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                Fill = Background,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Padding = new Vector4(0f, 0f, 0f, 0f),
            }.Size(FigmaSize.Fill, FigmaSize.Hug));

            var primaryReadoutTextFrame = primaryReadout.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                Fill = Background,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Padding = new Vector4(0f, 0f, 0f, 0f),
                Bounds = new Rect(0f, 0f, _window.Rect.width, 66f),
            }.Size(FigmaSize.Fill, FigmaSize.Hug));

            // Primary readout DPS text
            var primaryReadoutText = primaryReadoutTextFrame.Add(new FigmaText(default, Fmt(Tracker.Dps), HeaderStyle(TextAnchor.MiddleCenter, Primary, 50)).Size(FigmaSize.Fill, FigmaSize.Hug));

            // peak DPS text
            var PeakText = primaryReadout.Add(new FigmaText(default, $"Peak: {Gui.Colored(Fmt(Tracker.PeakDps), Primary)}", HeaderStyle(TextAnchor.MiddleCenter, Text, 20, FontStyle.Normal) ) { RichText = true,  }.Size(FigmaSize.Fill, FigmaSize.Hug));

            // ButtonContainer frame
            var buttonContainer = mainBodyLeft.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                Fill = Background,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Padding = new Vector4(0f, 0f, 0f, 0f),
                Bounds = new Rect(0f, 0f, 126f, 30f),
                Gap = 10f,
            }.Size(FigmaSize.Hug, FigmaSize.Hug));

            // counter inside the button container
            var counterFrame = buttonContainer.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                Fill = Background,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Padding = new Vector4(0f, 0f, 0f, 0f),
                Bounds = new Rect(0f, 0f, 46, 30f),
                Border = Primary,
                CornerRadius = 3f,
                BorderWidth = 1f,
            }.Size(FigmaSize.Fixed, FigmaSize.Fixed));

            

            // Elapsed time once a fight has started, otherwise a circular icon.
            bool fighting = Tracker.Elapsed > 0;
            var counterButton = counterFrame.Add(new FigmaButton(default,
                fighting ? Gui.Colored(TimeSpan.FromSeconds(Tracker.Elapsed).ToString("ss\\.f"), Primary) : "",
                Gui.ButtonKind.None, 14, Tracker.Reset)
            {
                Icon = fighting ? null : Gui.Icon("Circle.png"),
                IconSize = 14f
            }.Size(FigmaSize.Fill, FigmaSize.Hug));


            var mainBodyRight = mainBody.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                Fill = Background,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Start,
                Padding = new Vector4(0f, 5f, 10f, 5f),
                Gap = 7f,
                Bounds = new Rect(0f, 0f, _window.Rect.width, 180f),
            }.Size(FigmaSize.Fill, FigmaSize.Hug));

            Dictionary<string, string> infoDict = new()
            {
                { "Total", Fmt(Tracker.Total)},
                { "Highest hit", Fmt(Tracker.Highest)},
                { "Avg hit", Fmt(Tracker.Total / Math.Max(1, Tracker.Hits)) },
                { "Hits", $"{Tracker.Hits} | {(float)Tracker.Hits / Math.Max(1, Tracker.Elapsed):0.#}/s" },
                { "Crit rate", $"{(float)Tracker.Crits / Math.Max(1, Tracker.Hits) * 100:0.#}%" }
            };

            foreach (var kvp in infoDict)
            {
                var infoFrame = mainBodyRight.Add(new FigmaFrame(default)
                {
                    Layout = FigmaLayout.Horizontal,
                    Fill = Background,
                    MainAlign = FigmaAlign.SpaceBetween,
                    CrossAlign = FigmaAlign.Center,
                    Padding = new Vector4(0f, 0f, 0f, 0f),
                    Bounds = new Rect(0f, 0f, _window.Rect.width, 30f),
                    Gap = 10f,
                }.Size(FigmaSize.Fill, FigmaSize.Hug));

                // The value fills the rest of the row, and Align right-aligns the text inside it.
                infoFrame.Add(new FigmaText(default, kvp.Key, 16, Text, TextAnchor.MiddleLeft).Size(FigmaSize.Hug, FigmaSize.Hug));
                infoFrame.Add(new FigmaText(default, kvp.Value, 16, Primary, TextAnchor.MiddleRight).Size(FigmaSize.Hug, FigmaSize.Hug));
            }


        }

        static void OpenConfig()
        {
            // Implement the logic to open the configuration window here
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

        static void MinimizeWindow()
        {
            // Implement the logic to minimize the window here
        }

        static void CloseWindow()
        {
            // Implement the logic to close the window here
        }

        static string Fmt(double v)
        {
            if (v >= 1e18) return (v / 1e18).ToString("0.##") + "Qi";
            if (v >= 1e15) return (v / 1e15).ToString("0.##") + "Qa";
            if (v >= 1e12) return (v / 1e12).ToString("0.##") + "T";
            if (v >= 1e9) return (v / 1e9).ToString("0.##") + "B";
            if (v >= 1e6) return (v / 1e6).ToString("0.##") + "M";
            if (v >= 1e3) return (v / 1e3).ToString("0.##") + "K";
            return v.ToString("0");
        }

        static GUIStyle HeaderStyle(TextAnchor anchor, Color textColor, int fontSize = 50, FontStyle fontStyle = FontStyle.Bold)
        {
            var style = new GUIStyle
            {
                alignment = anchor,
                normal = { textColor = textColor },
                fontSize = fontSize,
                fontStyle = fontStyle
            };
            return style;
        }
    }
}