using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
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

        readonly FigmaFrame header;

        static readonly Color Accent = Helpers.Accent;
        static readonly Color Background = Helpers.Background;
        static readonly Color Primary = Helpers.Primary;
        static readonly Color Text = Helpers.Text;
        static readonly Color Secondary = Helpers.Secondary;

        static readonly Color Dimmed = new(1f, 1f, 1f, 0.7f);
        static readonly Color Pressed = new(1.1f, 1.1f, 1.1f, 1f);

        private static bool Minimal
        {
            get => Plugin.Minimal;
            set => Plugin.Minimal = value;
        }

        static void ToggleMinimal() => Minimal = !Minimal;
        

        private static bool ConfigOpen = false;

        static void ToggleConfig() => ConfigOpen = !ConfigOpen;

        private static string SelectedSkill = string.Empty;
        private static string SelectedSkillKey = string.Empty; // raw tracker name of the selected skill

        // Hovered is only raised while the pointer is over an icon, so the key is collected during a repaint
        // and applied on the next one; it clears itself as soon as no icon reports a hover.
        private static string HoveredSkillKey = string.Empty;
        private static string PendingHoveredSkillKey = string.Empty;

        const float UnselectedOpacity = 0.25f;

        public MeterWindowDesignV2(MeterWindow meterWindow, FigmaWindow window)
        {
            _meterWindow = meterWindow;
            _window = window;
            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                HoveredSkillKey = PendingHoveredSkillKey;
                PendingHoveredSkillKey = string.Empty;
            }
            root = new FigmaFrame(new Rect(0f, 0f, _window.Rect.width, _window.Rect.height))
            {
                Fill = Background,
                CornerRadius = 10f,
                Layout = FigmaLayout.Vertical,
                Gap = 0f
            };



            
            if (SelectedSkill != string.Empty && !Minimal)
                header = root.Add(Helpers.Header(_window, $"<cspace=0.3em>DPS METER — </cspace><cspace=0.15em>{SelectedSkill}</cspace>"));
            else
                header = root.Add(Helpers.Header(_window, "<cspace=0.3em>DPS METER</cspace>"));

            // Button row
            var buttonRow = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.Center,
                Gap = 4f
            }.Size(FigmaSize.Hug, FigmaSize.Hug);

            if (!Minimal)
            {
                buttonRow.Add(Helpers.IconButton("GearIcon.png", 24f, OpenConfig));
                buttonRow.Add(Helpers.IconButton("BigToSmall.png", 24f, ToggleMinimal));
                buttonRow.Add(Helpers.IconButton("CloseIcon.png", 24f, CloseWindow));
            }
            else
            {
                buttonRow.Add(Helpers.IconButton("SmallToBig.png", 24f, ToggleMinimal));
            }

            header.Add(buttonRow);


            if (ConfigOpen)
            {
                ConfigWindow();
            }
            else
            {
                MeterWindow();
            }
        }
 
        private FigmaFrame MeterWindow()
        {
            var mainBody = root.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Center,
                Padding = new Vector4(0f, 10f, 0f, 10f),
                CornerRadii = new Vector4(0f, 0f, 10f, 10f),
                Gap = 10f,
                Bounds = new Rect(0f, 0f, _window.Rect.width, 170f),
            }.Size(FigmaSize.Fill, FigmaSize.Fixed));

            if (Minimal)
            {
                mainBody.Padding = new Vector4(0f, 0f, 0f, 0f);
                mainBody.Bounds = new Rect(0f, 0f, _window.Rect.width, 112f);
            }

            mainBody.Add(MainBodyLeft());


            if (!Minimal)
            {
                // Right side of the main body frame
                mainBody.Add(MainBodyRight());

                // add info bars for damage types. only display types with non-zero totals
                if (Tracker.TypeTotals.Max() != 0)
                {
                    root.Add(MainBodyBottom());
                }
            }
            return mainBody;
        }

        private FigmaFrame MainBodyLeft()
        {
            var mainBodyLeft = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Gap = 20f,
                Padding = new Vector4(10f, 0f, 0f, 0f),
                Bounds = new Rect(0f, 0f, 266f, 150f),
            }.Size(FigmaSize.Fixed, FigmaSize.Fixed);

            if (Minimal)
            {
                mainBodyLeft.Bounds = new Rect(0f, 0f, 171f, 112f);
                mainBodyLeft.Padding = new Vector4(10f, 0f, 10f, 10f);
                mainBodyLeft.Gap = 5f;

            }

            // DPS Readout frame
            var primaryReadout = mainBodyLeft.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Center,
                Bounds = new Rect(0f, 0f, 256f, 91f),
            }.Size(FigmaSize.Fill, FigmaSize.Hug));

            var primaryReadoutTextFrame = primaryReadout.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
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

            if (Minimal)
            {
                primaryReadoutText.FontSize = 38;
                PeakText.FontSize = 14;
            }

            // The whole frame is the reset button. It shows the elapsed time once a fight has started, otherwise a circular icon.
            var counterFrame = mainBodyLeft.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Bounds = new Rect(0f, 0f, 46, 30f),
                Border = Primary,
                CornerRadius = 3f,
                BorderWidth = 1f,
                Clicked = Tracker.Reset,
                HoverTint = Dimmed,
                PressedTint = Pressed,
            }.Size(FigmaSize.Fixed, FigmaSize.Fixed));

            if (Tracker.Elapsed > 0)
                counterFrame.Add(new FigmaText(default, TimeSpan.FromSeconds(Tracker.Elapsed).ToString("ss\\.f"), 14, Primary, TextAnchor.MiddleCenter)
                    .Size(FigmaSize.Hug, FigmaSize.Hug));
            else
                counterFrame.Add(new FigmaImage(new Rect(0f, 0f, 14f, 14f), Gui.Icon("Circle.png")));

            return mainBodyLeft;
        }

        private FigmaFrame MainBodyRight()
        {
            // Right side of the main body frame
            var mainBodyRight = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Start,
                Padding = new Vector4(0f, 0f, 10f, 0f),
                Gap = 5f,
                Bounds = new Rect(0f, 0f, 183f, 150f),
            }.Size(FigmaSize.Fixed, FigmaSize.Fixed);



            // Info dictionary for the right side of the main body
            Dictionary<string, string> infoDict = new()
                {
                    { "Total", Helpers.Fmt(Tracker.Total)},
                    { "Highest hit", Helpers.Fmt(Tracker.Highest)},
                    { "Avg hit", Helpers.Fmt(Tracker.Total / Math.Max(1, Tracker.Hits)) },
                    { "Hits", $"{Tracker.Hits} | {Tracker.Hits / Math.Max(1, Tracker.Elapsed):0.0}/s" },
                    { "Crit rate", $"{(float)Tracker.Crits / Math.Max(1, Tracker.Hits) * 100:0.#}%" }
                };

            // Iterate through the info dictionary and create UI elements for each key-value pair
            foreach (var kvp in infoDict)
            {
                var infoFrame = mainBodyRight.Add(new FigmaFrame(default)
                {
                    Layout = FigmaLayout.Horizontal,
                    MainAlign = FigmaAlign.SpaceBetween,
                    CrossAlign = FigmaAlign.Center,
                    Bounds = new Rect(0f, 0f, _window.Rect.width, 26f),
                }.Size(FigmaSize.Fill, FigmaSize.Fixed));

                // The value fills the rest of the row, and Align right-aligns the text inside it.
                infoFrame.Add(new FigmaText(default, kvp.Key, 20, Text, TextAnchor.UpperLeft).Size(FigmaSize.Hug, FigmaSize.Hug));
                infoFrame.Add(new FigmaText(default, kvp.Value, 20, Primary, TextAnchor.UpperLeft).Size(FigmaSize.Hug, FigmaSize.Hug));
            }
            return mainBodyRight;
        }

        private FigmaFrame MainBodyBottom()
        {
            // Bottom section of the main body frame
            var mainBodyBottom = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                CornerRadii = new Vector4(0f, 0f, 10f, 10f),
                Padding = new Vector4(10f, 10f, 10f, 10f),
                Gap = 10f,
                Bounds = new Rect(0f, 0f, _window.Rect.width, 221f),
            }.Size(FigmaSize.Fill, FigmaSize.Hug);

            var mainBodyBottomBreakdownHeader = mainBodyBottom.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.SpaceBetween,
                BorderWidths = new Vector4(0f, 0f, 0f, 1f),
                Border = Primary,
                Padding = new Vector4(0f, 5f, 0f, 5f),
                Bounds = new Rect(0f, 0f, _window.Rect.width, 30f),
            }.Size(FigmaSize.Fill, FigmaSize.Fixed));

            mainBodyBottomBreakdownHeader.Add(new FigmaText(default, "Breakdown", 16, Primary, TextAnchor.LowerLeft)
                .Size(FigmaSize.Fill, FigmaSize.Hug));

            mainBodyBottomBreakdownHeader.Add(Helpers.IconButton("AdvancedBreakdown.png", 24, OpenAdvancedBreakdown)
            .Size(FigmaSize.Hug, FigmaSize.Hug));

            var mainBodyBottomContent = mainBodyBottom.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Start,
                Padding = new Vector4(0f, 0f, 0f, 0f),
                Gap = 10f,
                Bounds = new Rect(0f, 0f, _window.Rect.width, 143f),
            }.Size(FigmaSize.Fill, FigmaSize.Hug));

            var mainBodyBottomBars = mainBodyBottomContent.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Start,
                Gap = 5f,
                Bounds = new Rect(0f, 0f, _window.Rect.width, 208f),
            }.Size(FigmaSize.Fill, FigmaSize.Hug));

            var mainBodyBottomBarsInfo = mainBodyBottomContent.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Start,
                Gap = 5f,
                Bounds = new Rect(0f, 0f, 130, _window.Rect.height),
            }.Size(FigmaSize.Hug, FigmaSize.Fill));

            var mainBodyBottomBarsInfoHeader = mainBodyBottomBarsInfo.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.SpaceBetween,
                CrossAlign = FigmaAlign.Center,
                Gap = 10f,
                Bounds = new Rect(0f, 0f, 130f, 16f),
            }.Size(FigmaSize.Fill, FigmaSize.Fixed));

            mainBodyBottomBarsInfoHeader.Add(new FigmaText(default, "Sum", 16, Text){Align = TextAnchor.MiddleLeft}.Size(FigmaSize.Hug, FigmaSize.Hug));
            mainBodyBottomBarsInfoHeader.Add(new FigmaText(default, "% of All", 16, Text){Align = TextAnchor.MiddleRight}.Size(FigmaSize.Hug, FigmaSize.Hug));

            var mainBodyBottomBarsInfoData = mainBodyBottomBarsInfo.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                MainAlign = FigmaAlign.SpaceBetween,
                CrossAlign = FigmaAlign.End,
                Bounds = new Rect(0f, 0f, 130f, 135f),
            }.Size(FigmaSize.Fixed, FigmaSize.Fill));

            // add info bars for each damage type with non-zero totals
            double sum = Tracker.TypeTotals.Sum();

            // sort so that damage types with higher totals appear first
            var sortedIndices = Enumerable.Range(0, Tracker.TypeCount)
                .OrderByDescending(i => Tracker.TypeTotals[i])
                .ToList();

            for (int idx = 0; idx < sortedIndices.Count; idx++)
            {
                int i = sortedIndices[idx];
                double amount = Tracker.TypeTotals[i];
                if (amount <= 0) continue;

                // One segment per skill that dealt this damage type, biggest first
                var contributors = Tracker.Skills
                    .Where(s => s.Value.ByType[i] > 0)
                    .OrderByDescending(s => s.Value.ByType[i])
                    .ToList();
                var segmentKeys = contributors.Select(s => s.Key).ToArray();
                var segmentValues = contributors.Select(s => s.Value.ByType[i]).ToArray();
                var segmentColors = contributors.Select(_ => Tracker.TypeColors[i]).ToArray();
                mainBodyBottomBars.Add(ProgressBar((float)(amount / sum), segmentValues, segmentColors, Tracker.TypeNames[i], Tracker.IconsForType(i), segmentKeys: segmentKeys));


                var barsInfoData = mainBodyBottomBarsInfoData.Add(new FigmaFrame(default)
                {
                    Layout = FigmaLayout.Horizontal,
                    MainAlign = FigmaAlign.SpaceBetween,
                    CrossAlign = FigmaAlign.Center,
                    Bounds = new Rect(0f, 0f, 130f, 14f),
                }.Size(FigmaSize.Fill, FigmaSize.Fixed));


                barsInfoData.Add(new FigmaText(default, Helpers.Fmt(amount), 14, Primary)
                {
                    Bounds = new Rect(0f, 0f, 50f, 14f),
                    Align = TextAnchor.MiddleLeft
                }.Size(FigmaSize.Fixed, FigmaSize.Hug));

                barsInfoData.Add(new FigmaText(default, (amount / sum).ToString("P1"), 14, Primary)
                {
                    Bounds = new Rect(0f, 0f, 70f, 14f),
                    Align = TextAnchor.MiddleRight
                }.Size(FigmaSize.Fixed, FigmaSize.Hug));
            }

            return mainBodyBottom;
        }

        private FigmaFrame ConfigWindow()
        {
            // Fixed width uses Bounds.width; Hug height follows the children, so the window (AutoHeight) grows with them.
            var configWindow = root.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Start,
                Bounds = new Rect(0f, 0f, 459f, 0f),
                Gap = 20f,
                Padding = new Vector4(10f, 10f, 10f, 10f),
            }.Size(FigmaSize.Fixed, FigmaSize.Hug));

            configWindow.Add(ConfigWindowAutoRecording());

            configWindow.Add(Helpers.ToggleButton(Plugin.DebugWireframe.Value, ToggleWireframe));

            return configWindow;
        }

        private FigmaFrame ConfigWindowAutoRecording()
        {
            var frame = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                Bounds = new Rect(0f, 0f, 439f, 103f),
                Gap = 10f,
            }.Size(FigmaSize.Fixed, FigmaSize.Fixed);

            var titleBox = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Start,
                Bounds = new Rect(0f, 0f, 132f, 36f),
                Gap = 5f,
            }.Size(FigmaSize.Hug, FigmaSize.Fixed);


            titleBox.Add(ForceTextHeightBox(14, 132, "Automatic Recording", Primary, BundledFont.Bold));
            titleBox.Add(ForceTextHeightBox(12, 132, "Toggle and edit timings", Secondary, BundledFont.Regular));

            frame.Add(titleBox);

            var TogglesBox = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                Bounds = new Rect(0f, 0f, _window.Rect.width, 62f),
                Gap = 10f,
            }.Size(FigmaSize.Fill, FigmaSize.Fixed);

            TogglesBox.Add(SettingsToggle(Plugin.AutoArm, "Arm recording automatically", 16));
            TogglesBox.Add(SettingsToggle(Plugin.AutoStop, "Stop recording automatically after:", 16, true, Plugin.IdleTimeout, "Min 5 seconds", 5));

            frame.Add(TogglesBox);

            return frame;
        }


        private void OpenAdvancedBreakdown()
        {
            _meterWindow.ReplaceAdvancedBreakdown<AdvancedBreakdown>();
        }

        static void OpenConfig() 
        {
            //ToggleWireframe();
            ToggleConfig();
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

        void CloseWindow()
        {
            _meterWindow.Toggle();
        }


        private static Dictionary<string, string> skills = new()
        {
            ["Veil of  Quils"] = "Veil of Quills",
            ["Tupas  Wrath"] = "Tupas Wrath",
            ["Passage -  Start  Damage"] = "Passage",
            ["The Old Bell"] = "Old Bell",
            ["Double Knives"] = "Double Knives",
            ["Closed  Body  Retaliation -  Ao E"] = "Closed Body",
            ["Reflexes"] = "Spectral Knives",
            ["Basic Attack"] = "Basic Attack",
            ["Bleeding (DoT)"] = "Bleeding (DoT)",
            ["Splitshot"] = "Splitshot",
            ["Ignited (DoT)"] = "Ignited (DoT)",
            ["Repeater"] = "Repeater",
            ["Shadow Dash"] = "Shadow Dash",
            ["Bombardment Projectile"] = "Bombardment",
            ["Enemy  Cannons  Damage"] = "Matedeira",
            ["Summon  The  Guard"] = "Summon The Guard",
            ["Holy  Rosary"] = "Holy Rosary",
            ["Summon  Marksmen"] = "Summon Marksmen",
            ["Feitors Lash"] = "Feitors Lash",
            ["Six  Shooter"] = "Lead Sermon",
            ["Holy  Fire"] = "Holy Fire",
            ["Holy  Fire -  Damage"] = "Holy Fire Explosion",
            ["Carcara  Dive -  Damage"] = "Carcara  Dive",
            ["Cilada -  Trap  Explosion"] = "Cilada",
            ["Jaguar  Spear"] = "Jaguar Spear",
        };

        // Progress bar built from frames: outline > track > one frame per segment, sized by FillWeight.
        // value is the filled fraction of the track; segments are split proportionally over the filled part.
        static FigmaFrame ProgressBar(float value, double[] segments, Color[] colors, string label = null, List<KeyValuePair<string, Sprite>> icons = null, Action clicked = null, Action hovered = null, string[] segmentKeys = null)
        {
            // main container for the progress bar and its label
            var frame = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Vertical,
                Gap = 2f,
                Bounds = new Rect(0f, 0f, 0f, 35f),
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Start,
            }.Size(FigmaSize.Fill, FigmaSize.Fixed);


            FigmaFrame labelHeader = frame.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Center,
                Bounds = new Rect(0f, 0f, 0f, 21f),
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
                foreach (var skill in icons)
                {
                    string skillName = skill.Key;
                    //if (skill.Value == null) Plugin.Logger.LogWarning($"Icon for skill {skillName} is null.");

                    labelHeader.Add(Helpers.IconButton(skill.Value, 21f, () =>
                    {
                        // clicking the selected skill again clears the selection
                        if (SelectedSkillKey == skillName)
                        {
                            SelectedSkillKey = SelectedSkill = string.Empty;
                            return;
                        }
                        SelectedSkillKey = skillName;
                        SelectedSkill = skills.TryGetValue(skillName, out var translated) ? translated : skillName;
                        //Plugin.Logger.LogInfo($"Selected skill: {skillName}, translated: {SelectedSkill}, icon: {skill.Value}");
                    },
                    // temporarily highlight the segments of the hovered skill
                    hovered: () => PendingHoveredSkillKey = skillName));
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
                CornerRadius = 10f,
                Border = Color.black,
                BorderWidth = 1f,
                Padding = new Vector4(1f, 1f, 1f, 1f), // keeps the segments inside the border
                Gap = 2f,
            }.Size(FigmaSize.Fill, FigmaSize.Fill));

            double total = 0;
            for (int i = 0; i < segments.Length && i < colors.Length; i++) total += Math.Max(0.0, segments[i]);
            float filled = total > 0 ? Mathf.Clamp01(value) : 0f;

            // a hovered skill takes precedence over the selected one while the pointer is on its icon
            string highlightKey = HoveredSkillKey != string.Empty ? HoveredSkillKey : SelectedSkillKey;

            for (int i = 0; i < segments.Length && i < colors.Length; i++)
            {
                if (segments[i] <= 0) continue;
                var color = colors[i];
                // dim every segment but the highlighted skill's
                if (highlightKey != string.Empty && (segmentKeys == null || i >= segmentKeys.Length || segmentKeys[i] != highlightKey))
                    color.a *= UnselectedOpacity;
                track.Add(new FigmaFrame(default)
                {
                    Fill = color,
                    CornerRadius = 10f,
                    FillWeight = filled * (float)(segments[i] / total),
                }.Size(FigmaSize.Fill, FigmaSize.Fill));
            }

            // Unfilled remainder of the track.
            if (filled < 1f)
                track.Add(new FigmaFrame(default) { FillWeight = 1f - filled }.Size(FigmaSize.Fill, FigmaSize.Fill));
            return frame;
        }

        static FigmaFrame ForceTextHeightBox(int height, int width, string text, Color color, Font font)
        {
            var frame = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                Bounds = new Rect(0f, 0f, width, height),
            }.Size(FigmaSize.Hug, FigmaSize.Fixed);

            frame.Add(new FigmaText(default, text, height, color)
            {
                Font = font,
                Align = TextAnchor.UpperLeft,
                Bounds = new Rect(0f, 0f, width, height),
            }.Size(FigmaSize.Hug, FigmaSize.Hug));

            return frame;
        }

        static FigmaFrame SettingsToggle(ConfigEntry<bool> conf, string label, int fontSize, bool additional = false, ConfigEntry<int> additionalConf = null, string additionalLabel = "", int additionalMinValue = 0)
        {
            var frame = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.SpaceBetween,
                CrossAlign = FigmaAlign.Center,
                Bounds = new Rect(0f, 0f, 0f, 26f),
            }.Size(FigmaSize.Fill, FigmaSize.Fixed);

            var ToggleBox = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.Start,
                CrossAlign = FigmaAlign.Center,
                Bounds = new Rect(0f, 0f, 0f, 20f),
                Gap = 5f,
            }.Size(FigmaSize.Hug, FigmaSize.Fixed);

            ToggleBox.Add(Helpers.ToggleButton(conf.Value, () => conf.Value = !conf.Value));

            ToggleBox.Add(new FigmaText(default, label, fontSize, conf.Value ? Text : Secondary)
            {
                Font = BundledFont.Light,
                Align = TextAnchor.MiddleLeft,
                Bounds = new Rect(0f, 0f, 0f, fontSize),
            }.Size(FigmaSize.Hug, FigmaSize.Hug));

            //ToggleBox.Add(ForceTextHeightBox(fontSize, 0, label, conf.Value ? Text : Secondary, BundledFont.Light));
            
            frame.Add(ToggleBox);

            if (additional)
            {
                var additionalFrame = new FigmaFrame(default)
                {
                    Layout = FigmaLayout.Horizontal,
                    MainAlign = FigmaAlign.Center,
                    CrossAlign = FigmaAlign.Center,
                    Bounds = new Rect(0f, 0f, 0f, 26f),
                    Gap = 5f,
                }.Size(FigmaSize.Hug, FigmaSize.Fixed);

                //additionalFrame.Add(ForceTextHeightBox(fontSize, 0, additionalLabel, conf.Value ? Text : Secondary, BundledFont.Light));

                additionalFrame.Add(new FigmaText(default, additionalLabel, fontSize, conf.Value ? Text : Secondary)
                {
                    Font = BundledFont.Light,
                    Align = TextAnchor.MiddleLeft,
                    Bounds = new Rect(0f, 0f, 0f, fontSize),
                }.Size(FigmaSize.Hug, FigmaSize.Hug));
                
                additionalFrame.Add(Helpers.NumberButton(additionalConf, conf.Value, fontSize, additionalMinValue));
                frame.Add(additionalFrame);
            }

            return frame;
        }
    }
}