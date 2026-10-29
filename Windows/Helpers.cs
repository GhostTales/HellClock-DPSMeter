using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BepInEx.Configuration;
using FrameUI;
using UnityEngine;

namespace DPSMeter
{
    public class Helpers
    {
        public static readonly Color Accent = new Color(0x15 / 255f, 0x14 / 255f, 0x19 / 255f);
        public static readonly Color Background = new Color(0x17 / 255f, 0x16 / 255f, 0x1D / 255f);
        public static readonly Color Primary = new Color(0xF6 / 255f, 0x4C / 255f, 0x2C / 255f);
        public static readonly Color Text = new Color(0xFF / 255f, 0xE9 / 255f, 0xE9 / 255f);
        public static readonly Color Secondary = new Color(0x8B / 255f, 0x7F / 255f, 0x83 / 255f);

        public static string Fmt(double v)
        {
            if (v >= 1e18) return (v / 1e18).ToString("0.00") + "Qi";
            if (v >= 1e15) return (v / 1e15).ToString("0.00") + "Qa";
            if (v >= 1e12) return (v / 1e12).ToString("0.00") + "T";
            if (v >= 1e9) return (v / 1e9).ToString("0.00") + "B";
            if (v >= 1e6) return (v / 1e6).ToString("0.00") + "M";
            if (v >= 1e3) return (v / 1e3).ToString("0.00") + "K";
            return v.ToString("0");
        }

        public static GUIStyle HeaderStyle(TextAnchor anchor, Color textColor, int fontSize = 50, FontStyle fontStyle = FontStyle.Bold)
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

        public static FigmaFrame Header(FigmaWindow _window, String title)
        {
            var header = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                Fill = Accent,
                MainAlign = FigmaAlign.SpaceBetween,   // title left, buttons right
                CrossAlign = FigmaAlign.Center,
                CornerRadii = new Vector4(10f, 10f, 0f, 0f),
                Padding = new Vector4(10f, 0f, 10f, 0f),
                Bounds = new Rect(0f, 0f, _window.Rect.width, 40f),
            }.Size(FigmaSize.Fill, FigmaSize.Fixed);

            // Header title
            var titleText = new FigmaText(default, title, 16, Primary) { RichText = true };
            if (BundledFont.Medium != null) titleText.Font = BundledFont.Medium;
            header.Add(titleText.Size(FigmaSize.Hug, FigmaSize.Hug));

            return header;
        }

        static readonly Color Dimmed = new(1f, 1f, 1f, 0.7f);
        static readonly Color Pressed = new(1.1f, 1.1f, 1.1f, 1f);
        public static FigmaFrame IconButton(string icon, float size, Action clicked)
        {
            var button = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Clicked = clicked,
                HoverTint = Dimmed,
                PressedTint = Pressed,
            }.Size(FigmaSize.Hug, FigmaSize.Hug);
            button.Add(new FigmaImage(new Rect(0f, 0f, size, size), Gui.Icon(icon)));
            return button;
        }

        public static FigmaFrame IconButton(Sprite icon, float size, Action clicked, Action hovered = null)
        {
            var button = new FigmaFrame(default)
            {
                Layout = FigmaLayout.Horizontal,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Clicked = clicked,
                Hovered = hovered,
                HoverTint = Dimmed,
                PressedTint = Pressed,
            }.Size(FigmaSize.Hug, FigmaSize.Hug);
            button.Add(new FigmaImage(new Rect(0f, 0f, size, size), icon));
            return button;
        }

        public static FigmaFrame Button(string text, bool selected, int fontSize, Action clicked)
        {
            var border = selected ? Primary : Secondary;
            var button = new FigmaFrame(new Rect(0f, 0f, 30f, 26f))
            {
                Layout = FigmaLayout.Vertical,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Padding = new Vector4(2f, 0f, 2f, 0f),
                Border = border,
                BorderWidths = new Vector4(1f, 1f, 1f, 1f),
                CornerRadius = 2f,
                MinWidth = 30f,
                MinHeight = 26f,
                Clicked = clicked,
            }.Size(FigmaSize.Hug, FigmaSize.Hug);
            button.Add(new FigmaText(default, text, fontSize, border, TextAnchor.MiddleCenter).Size(FigmaSize.Hug, FigmaSize.Hug));
            return button;
        }

        // The tree is rebuilt on every GUI event, so the entry being edited lives in statics.
        static ConfigEntry<int> _editing;
        static string _editBuffer = "";
        static FigmaFrame _editFrame;
        const int MaxDigits = 9;

        /// <summary>
        /// A button showing an int config value. Clicking it lets the user type a new number:
        /// Enter (or clicking elsewhere) applies it (raised to minValue if lower), Escape cancels. Does nothing while disabled.
        /// </summary>
        public static FigmaFrame NumberButton(ConfigEntry<int> conf, bool enabled, int fontSize, int minValue)
        {
            if (_editing == conf && !enabled) _editing = null;
            if (_editing == conf) HandleEditInput(conf, minValue);

            bool editing = _editing == conf;
            string text = $"{conf.Value}";
            if (editing)
                text = _editBuffer + (Mathf.FloorToInt(Time.unscaledTime * 2f) % 2 == 0 ? "|" : " ");

            var button = Button(text, enabled, fontSize, () =>
            {
                if (!enabled) return;
                if (_editing == conf) CommitEdit(conf, minValue);
                else
                {
                    _editing = conf;
                    _editBuffer = "";
                }
            });
            if (editing) _editFrame = button;
            return button;
        }

        static void HandleEditInput(ConfigEntry<int> conf, int minValue)
        {
            var e = Event.current;
            if (e == null) return;

            if (e.type == EventType.MouseDown && _editFrame != null && !_editFrame.LayoutRect.Contains(e.mousePosition))
            {
                CommitEdit(conf, minValue);
                return;
            }
            if (e.type != EventType.KeyDown) return;

            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                CommitEdit(conf, minValue);
                e.Use();
            }
            else if (e.keyCode == KeyCode.Escape)
            {
                _editing = null;
                e.Use();
            }
            else if (e.keyCode == KeyCode.Backspace)
            {
                if (_editBuffer.Length > 0) _editBuffer = _editBuffer.Substring(0, _editBuffer.Length - 1);
                e.Use();
            }
            else if (e.character >= '0' && e.character <= '9')
            {
                if (_editBuffer.Length < MaxDigits) _editBuffer += e.character;
                e.Use();
            }
        }

        static void CommitEdit(ConfigEntry<int> conf, int minValue)
        {
            // The entry's setter additionally clamps to its AcceptableValueRange, if any.
            if (int.TryParse(_editBuffer, out var value)) conf.Value = Mathf.Max(value, minValue);
            _editing = null;
        }

        public static FigmaFrame ToggleButton(bool selected, Action clicked)
        {
            var button = new FigmaFrame(new Rect(0f, 0f, 20f, 20f))
            {
                Layout = FigmaLayout.Vertical,
                MainAlign = FigmaAlign.Center,
                CrossAlign = FigmaAlign.Center,
                Border = Primary,
                BorderWidth = 1f,
                CornerRadius = 2.4f,
                Clicked = clicked,
            }.Size(FigmaSize.Fixed, FigmaSize.Fixed);

            if (selected)
            {
                button.Add(new FigmaFrame(new Rect(0f, 0f, 12f, 12f))
                {
                    Border = Primary,
                    Fill = Primary,
                    BorderWidth = 0.48f,
                    CornerRadius = 0.96f,
                }.Size(FigmaSize.Fixed, FigmaSize.Fixed));
            }

            return button;
        }

    }
}