using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        
    }
}