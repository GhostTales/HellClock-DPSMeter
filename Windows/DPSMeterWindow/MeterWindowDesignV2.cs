using System;
using System.Collections.Generic;
using System.Linq;
using DPSMeter.Windows.AdvancedWindow;
using FrameUI;
using UnityEngine;

namespace DPSMeter.Windows.DPSMeterWindow
{
    internal class MeterWindowDesignV2
    {
        readonly MeterWindow _meterWindow;
        readonly FigmaWindow _window;
        readonly FigmaFrame root;

        static readonly Color Accent = Helpers.Accent;
        static readonly Color Background = Helpers.Background;
        static readonly Color Primary = Helpers.Primary;
        static readonly Color Text = Helpers.Text;
        static readonly Color Secondary = Helpers.Secondary;

        private bool Minimal { get; set; } = false;

        public MeterWindowDesignV2(MeterWindow meterWindow, FigmaWindow window)
        {
            _meterWindow = meterWindow;
            _window = window;
            root = new FigmaFrame(new Rect(0f, 0f, _window.Rect.width, _window.Rect.height))
            {
                Fill = Background,
                CornerRadius = 10f,
                Layout = FigmaLayout.Vertical,
                Gap = 0f
            };


            var header = root.Add(Helpers.Header(_window, "<cspace=0.3em>DPS METER</cspace>"));

            // Button row
            var buttonRow = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.Center,
                Gap = 4f
            }.Size(FigmaSize.Hug, FigmaSize.Hug);

            buttonRow.Add(Helpers.IconButton("GearIcon.png", 24f, OpenConfig));
            buttonRow.Add(Helpers.IconButton("MinimizeIcon.png", 24f, MinimizeWindow));
            buttonRow.Add(Helpers.IconButton("CloseIcon.png", 24f, CloseWindow));

            header.Add(buttonRow);

            var mainBody = root.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                Fill = Background,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Start,
                Padding = new Vector4(0f, 10f, 0f, 10f),
                CornerRadii = new Vector4(0f, 0f, 10f, 10f),
                Gap = 10f,
                Bounds = new Rect(0f, 0f, _window.Rect.width, 170f),
            }.Size(FigmaSize.Fill, FigmaSize.Hug));

            var mainBodyLeft = mainBody.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                Fill = Background,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Gap = 20f,
                Padding = new Vector4(10f, 0f, 0f, 10f),
                Bounds = new Rect(0f, 0f, 266f, _window.Rect.height),
            }.Size(FigmaSize.Fixed, FigmaSize.Fill));

            // DPS Readout frame
            var primaryReadout = mainBodyLeft.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                Fill = Background,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Start,
            }.Size(FigmaSize.Fill, FigmaSize.Hug));

            var primaryReadoutTextFrame = primaryReadout.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                Fill = Background,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Padding = new Vector4(0f, 0f, 0f, 0f),
                Bounds = new Rect(0f, 0f, _window.Rect.width, 65f),
                Gap = 10f,
            }.Size(FigmaSize.Fill, FigmaSize.Hug));

            // Primary readout DPS text
            var primaryReadoutText = primaryReadoutTextFrame.Add(new FigmaText(default, Helpers.Fmt(Tracker.Dps), Helpers.HeaderStyle(TextAnchor.UpperLeft, Primary, 50))
            {
                Font = BundledFont.Medium,
                Bounds = new Rect(0f, 0f, 158f, 65f),
            }.Size(FigmaSize.Hug, FigmaSize.Hug));

            // peak DPS text
            var PeakText = primaryReadout.Add(new FigmaText(default, $"Peak: {Gui.Colored(Helpers.Fmt(Tracker.PeakDps), Primary)}", Helpers.HeaderStyle(TextAnchor.UpperCenter, Text, 20, FontStyle.Normal)) { RichText = true, Font = BundledFont.Medium }.Size(FigmaSize.Fill, FigmaSize.Hug));

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

            // The whole frame is the reset button. It shows the elapsed time once a fight has started, otherwise a circular icon.
            var counterFrame = buttonContainer.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                Fill = Background,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Bounds = new Rect(0f, 0f, 46, 30f),
                Border = Primary,
                CornerRadius = 3f,
                BorderWidth = 1f,
                Clicked = Tracker.Reset,
                HoverTint = Color.white,
                PressedTint = Pressed,
            }.Size(FigmaSize.Fixed, FigmaSize.Fixed));

            if (Tracker.Elapsed > 0)
                counterFrame.Add(new FigmaText(default, TimeSpan.FromSeconds(Tracker.Elapsed).ToString("ss\\.f"), 14, Primary, TextAnchor.MiddleCenter)
                    .Size(FigmaSize.Hug, FigmaSize.Hug));
            else
                counterFrame.Add(new FigmaImage(new Rect(0f, 0f, 14f, 14f), Gui.Icon("Circle.png")));


            if (!Minimal)
            {
                // Right side of the main body frame
                var mainBodyRight = mainBody.Add(new FigmaFrame(default)
                {
                    Layout = FigmaLayout.Vertical,
                    Fill = Background,
                    MainAlign = FigmaAlign.Start,
                    CrossAlign = FigmaAlign.Start,
                    Padding = new Vector4(0f, 0f, 10f, 0f),
                    Gap = 5f,
                    Bounds = new Rect(0f, 0f, _window.Rect.width, 150f),
                }.Size(FigmaSize.Fill, FigmaSize.Hug));



                // Info dictionary for the right side of the main body
                Dictionary<string, string> infoDict = new()
                {
                    { "Total", Helpers.Fmt(Tracker.Total)},
                    { "Highest hit", Helpers.Fmt(Tracker.Highest)},
                    { "Avg hit", Helpers.Fmt(Tracker.Total / Math.Max(1, Tracker.Hits)) },
                    { "Hits", $"{Tracker.Hits} | {(float)Tracker.Hits / Math.Max(1, Tracker.Elapsed):0.0}/s" },
                    { "Crit rate", $"{(float)Tracker.Crits / Math.Max(1, Tracker.Hits) * 100:0.#}%" }
                };

                // Iterate through the info dictionary and create UI elements for each key-value pair
                foreach (var kvp in infoDict)
                {
                    var infoFrame = mainBodyRight.Add(new FigmaFrame(default)
                    {
                        Layout = FigmaLayout.Horizontal,
                        Fill = Background,
                        MainAlign = FigmaAlign.SpaceBetween,
                        CrossAlign = FigmaAlign.Center,
                        Bounds = new Rect(0f, 0f, _window.Rect.width, 26f),
                    }.Size(FigmaSize.Fill, FigmaSize.Hug));

                    // The value fills the rest of the row, and Align right-aligns the text inside it.
                    infoFrame.Add(new FigmaText(default, kvp.Key, 20, Text, TextAnchor.UpperLeft).Size(FigmaSize.Hug, FigmaSize.Hug));
                    infoFrame.Add(new FigmaText(default, kvp.Value, 20, Primary, TextAnchor.UpperLeft).Size(FigmaSize.Hug, FigmaSize.Hug));
                }



                // var mainBodyBottomButtonHeader = mainBodyBottom.Add(new FigmaFrame(default)
                // {
                //     Layout = FigmaLayout.Horizontal,
                //     Fill = Background,
                //     MainAlign = FigmaAlign.Start,
                //     CrossAlign = FigmaAlign.Start,
                //     Padding = new Vector4(0f, 0f, 0f, 0f),
                //     Gap = 10f,
                //     Bounds = new Rect(0f, 0f, _window.Rect.width, 35f),
                // }.Size(FigmaSize.Fill, FigmaSize.Fixed));

                // // breakdown tab buttons, the selected one is highlighted
                // mainBodyBottomButtonHeader.Add(Button("Skill", SelectedBreakdownTab == "Skill", 20, () => SelectedBreakdownTab = "Skill")
                //     .Size(FigmaSize.Fill, FigmaSize.Fixed));
                // mainBodyBottomButtonHeader.Add(Button("Damage Type", SelectedBreakdownTab == "Damage Type", 20, () => SelectedBreakdownTab = "Damage Type")
                //     .Size(FigmaSize.Fill, FigmaSize.Fixed));


                // add info bars for damage types. only display types with non-zero totals
                if (Tracker.TypeTotals.Max() != 0)
                {
                    // Bottom section of the main body frame
                    var mainBodyBottom = root.Add(new FigmaFrame(default)
                    {
                        Layout = FigmaLayout.Vertical,
                        Fill = Background,
                        MainAlign = FigmaAlign.Center,
                        CrossAlign = FigmaAlign.Center,
                        CornerRadii = new Vector4(0f, 0f, 10f, 10f),
                        Padding = new Vector4(10f, 10f, 10f, 10f),
                        Gap = 10f,
                        Bounds = new Rect(0f, 0f, _window.Rect.width, 208f),
                    }.Size(FigmaSize.Fill, FigmaSize.Hug));

                    var mainBodyBottomBreakdownHeader = mainBodyBottom.Add(new FigmaFrame(default)
                    {
                        Layout = FigmaLayout.Horizontal,
                        Fill = Background,
                        MainAlign = FigmaAlign.Start,
                        CrossAlign = FigmaAlign.SpaceBetween,
                        BorderWidths = new Vector4(0f, 0f, 0f, 1f),
                        Border = Primary,
                        Padding = new Vector4(0f, 5f, 0f, 5f),
                    }.Size(FigmaSize.Fill, FigmaSize.Hug));

                    mainBodyBottomBreakdownHeader.Add(new FigmaText(default, "Breakdown", 16, Primary, TextAnchor.LowerLeft)
                        .Size(FigmaSize.Fill, FigmaSize.Hug));

                    mainBodyBottomBreakdownHeader.Add(Helpers.IconButton("AdvancedBreakdown.png", 20, OpenAdvancedBreakdown)
                    .Size(FigmaSize.Hug, FigmaSize.Hug));

                    var mainBodyBottomContent = mainBodyBottom.Add(new FigmaFrame(default)
                    {
                        Layout = FigmaLayout.Horizontal,
                        Fill = Background,
                        MainAlign = FigmaAlign.Start,
                        CrossAlign = FigmaAlign.Start,
                        Padding = new Vector4(0f, 0f, 0f, 0f),
                        Gap = 10f,
                        Bounds = new Rect(0f, 0f, _window.Rect.width, 143f),
                    }.Size(FigmaSize.Fill, FigmaSize.Hug));

                    var mainBodyBottomBars = mainBodyBottomContent.Add(new FigmaFrame(default)
                    {
                        Layout = FigmaLayout.Vertical,
                        Fill = Background,
                        MainAlign = FigmaAlign.Start,
                        CrossAlign = FigmaAlign.Start,
                        Padding = new Vector4(0f, 0f, 0f, 0f),
                        Gap = 10f,
                        Bounds = new Rect(0f, 0f, _window.Rect.width, 208f),
                    }.Size(FigmaSize.Fill, FigmaSize.Hug));

                    var mainBodyBottomBarsInfo = mainBodyBottomContent.Add(new FigmaFrame(default)
                    {
                        Layout = FigmaLayout.Vertical,
                        Fill = Background,
                        MainAlign = FigmaAlign.Start,
                        CrossAlign = FigmaAlign.Start,
                        Padding = new Vector4(0f, 0f, 0f, 0f),
                        Gap = 0f,
                        Bounds = new Rect(0f, 0f, 130, _window.Rect.height),
                    }.Size(FigmaSize.Hug, FigmaSize.Fill));

                    var mainBodyBottomBarsInfoHeader = mainBodyBottomBarsInfo.Add(new FigmaFrame(default)
                    {
                        Layout = FigmaLayout.Horizontal,
                        Fill = Background,
                        MainAlign = FigmaAlign.End,
                        CrossAlign = FigmaAlign.Center,
                        Padding = new Vector4(0f, 0f, 0f, 0f),
                        Gap = 10f,
                        Bounds = new Rect(0f, 0f, 130f, 14f),
                    }.Size(FigmaSize.Fill, FigmaSize.Hug));

                    var mainBodyBottomBarsInfoData = mainBodyBottomBarsInfo.Add(new FigmaFrame(default)
                    {
                        Layout = FigmaLayout.Vertical,
                        Fill = Background,
                        MainAlign = FigmaAlign.End,
                        CrossAlign = FigmaAlign.Center,
                        Padding = new Vector4(0f, 0f, 0f, 0f),
                        Gap = 25f,
                        Bounds = new Rect(0f, 0f, 130f, 123f),
                    }.Size(FigmaSize.Fixed, FigmaSize.Hug));

                    // add info bars for each damage type with non-zero totals
                    double sum = Tracker.TypeTotals.Sum();

                    mainBodyBottomBarsInfoHeader.Add(new FigmaText(default, "Sum", 14, Text).Size(FigmaSize.Hug, FigmaSize.Hug));
                    mainBodyBottomBarsInfoHeader.Add(new FigmaText(default, "Distribution", 14, Text).Size(FigmaSize.Hug, FigmaSize.Hug));

                    // sort so that damage types with higher totals appear first
                    var sortedIndices = Enumerable.Range(0, Tracker.TypeCount)
                        .OrderByDescending(i => Tracker.TypeTotals[i])
                        .ToList();

                    for (int idx = 0; idx < sortedIndices.Count; idx++)
                    {
                        int i = sortedIndices[idx];
                        double amount = Tracker.TypeTotals[i];
                        if (amount <= 0) continue;

                        // One segment filling the whole bar means no segmentation
                        mainBodyBottomBars.Add(ProgressBar((float)(amount / sum), new[] { 1.0 }, new[] { Tracker.TypeColors[i] }, Tracker.TypeNames[i], Tracker.IconsForType(i)));


                        var barsInfoData = mainBodyBottomBarsInfoData.Add(new FigmaFrame(default)
                        {
                            Layout = FigmaLayout.Horizontal,
                            Fill = Background,
                            MainAlign = FigmaAlign.End,
                            CrossAlign = FigmaAlign.Center,
                            Bounds = new Rect(0f, 0f, 130f, 12f),
                            Gap = 10f,
                            Padding = new Vector4(0f, 0f, 0f, 0f),
                        }.Size(FigmaSize.Fill, FigmaSize.Hug));


                        barsInfoData.Add(new FigmaText(default, Helpers.Fmt(amount), 12, Primary)
                        {
                            Bounds = new Rect(0f, 0f, 50f, 12f),
                            Align = TextAnchor.MiddleRight
                        }.Size(FigmaSize.Fixed, FigmaSize.Hug));

                        barsInfoData.Add(new FigmaText(default, (amount / sum).ToString("P1"), 12, Primary)
                        {
                            Bounds = new Rect(0f, 0f, 70f, 12f),
                            Align = TextAnchor.MiddleRight
                        }.Size(FigmaSize.Fixed, FigmaSize.Hug));
                    }
                }
            }
        }

        private void OpenAdvancedBreakdown()
        {
            _meterWindow.ReplaceAdvancedBreakdown<AdvancedBreakdown>();
        }

        static void OpenConfig()
        {
            ToggleWireframe();
            // Implement the logic to open the configuration window here
        }

        static void ToggleWireframe()
        {
            FigmaFrame.Wireframe = !FigmaFrame.Wireframe;
            Plugin.DebugWireframe.Value = FigmaFrame.Wireframe;
        }

        public void Render()
        {
            _window.FitTo(root);
            root.Render(new Rect(0f, 0f, _window.Rect.width, _window.Rect.height));
        }

        void MinimizeWindow() 
        { 
            Minimal = !Minimal;
        }

        void CloseWindow()
        {
            _meterWindow.Toggle();
        }

        // ---- Widgets: plain frames with a click handler and per-state colours ----

        static readonly Color Dimmed = new Color(0.85f, 0.85f, 0.85f, 1f);
        static readonly Color Pressed = new Color(0.7f, 0.7f, 0.7f, 1f);

        // Framed button with a text label. Selected buttons use the primary colour.
        static FigmaFrame Button(string text, bool selected, int fontSize, Action clicked)
        {
            var border = selected ? Primary : Secondary;
            var button = new FigmaFrame(new Rect(0f, 0f, 0f, 30f))
            {
                Layout = FigmaLayout.Vertical,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Padding = new Vector4(0f, 0f, 0f, 0f),
                Border = border,
                BorderWidths = new Vector4(0f, 0f, 0f, 1f),
                //CornerRadius = 2f,
                Gap = 20f,
                Fill = Background,
                Clicked = clicked,
            }.Size(FigmaSize.Hug, FigmaSize.Hug);
            button.Add(new FigmaText(default, text, fontSize, border, TextAnchor.MiddleCenter).Size(FigmaSize.Hug, FigmaSize.Hug));
            return button;
        }

        // Borderless button that only shows an icon, dimmed until hovered.


        // Progress bar built from frames: outline > track > one frame per segment, sized by FillWeight.
        // value is the filled fraction of the track; segments are split proportionally over the filled part.
        static FigmaFrame ProgressBar(float value, double[] segments, Color[] colors, string label = null, List<Sprite> icons = null, Action clicked = null, Action hovered = null)
        {
            // main container for the progress bar and its label
            var frame = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                Gap = 2f,
                Bounds = new Rect(0f, 0f, 0f, 32f),
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Start,
            }.Size(FigmaSize.Fill, FigmaSize.Hug);


            FigmaFrame labelHeader = frame.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Center,
                Bounds = new Rect(0f, 0f, 0f, 16f),
                Gap = 10f,
            }.Size(FigmaSize.Fill, FigmaSize.Fixed));


            // label for the progress bar, if provided
            if (!string.IsNullOrEmpty(label))
            {
                labelHeader.Add(new FigmaText(default, label, 14, Text, TextAnchor.MiddleLeft).Size(FigmaSize.Hug, FigmaSize.Hug));
            }
            // icons for the progress bar, if provided
            if (icons != null)
            {
                foreach (var icon in icons)
                {
                    labelHeader.Add(new FigmaImage(new Rect(0f, 0f, 16f, 16f), icon).Size(FigmaSize.Hug, FigmaSize.Hug));
                }
            }

            // container for the progress bar
            var barContainer = frame.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Center,
                Bounds = new Rect(0f, 0f, 0f, 12f),
            }.Size(FigmaSize.Fill, FigmaSize.Fixed));

            // outer bar (the outline of the progress bar)
            var bar = barContainer.Add(new FigmaFrame(new Rect(0f, 0f, 0f, 20f))
            {
                Layout = FigmaLayout.Horizontal,
                Padding = new Vector4(2f, 2f, 2f, 2f),
                Fill = Background,
                CornerRadius = 10f,
                Clicked = clicked,
                Hovered = hovered,
                Bounds = new Rect(0f, 0f, 0f, 12f),
                Border = Secondary,
                BorderWidth = 1f,
            }.Size(FigmaSize.Fill, FigmaSize.Fixed));

            // track: the empty background. It lays its children (the segments) out side by side, sharing its width by FillWeight
            var track = bar.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                Fill = Background,
                CornerRadius = 10f,
                Border = Color.black,
                BorderWidth = 1f,
                Padding = new Vector4(1f, 1f, 1f, 1f), // keeps the segments inside the border
            }.Size(FigmaSize.Fill, FigmaSize.Fill));

            double total = 0;
            for (int i = 0; i < segments.Length && i < colors.Length; i++) total += Math.Max(0.0, segments[i]);
            float filled = total > 0 ? Mathf.Clamp01(value) : 0f;

            // Only the outer ends of the filled part are rounded, so segments butt up against each other.
            int first = -1, last = -1;
            for (int i = 0; i < segments.Length && i < colors.Length; i++)
            {
                if (segments[i] <= 0) continue;
                if (first < 0) first = i;
                last = i;
            }
            for (int i = first; first >= 0 && i <= last; i++)
            {
                if (segments[i] <= 0) continue;
                float left = i == first ? 8f : 0f, right = i == last ? 8f : 0f;
                track.Add(new FigmaFrame(default)
                {
                    Fill = colors[i],
                    CornerRadii = new Vector4(left, right, right, left),
                    FillWeight = filled * (float)(segments[i] / total),
                }.Size(FigmaSize.Fill, FigmaSize.Fill));
            }

            // Unfilled remainder of the track.
            track.Add(new FigmaFrame(default) { FillWeight = 1f - filled }.Size(FigmaSize.Fill, FigmaSize.Fill));
            return frame;
        }


    }
}