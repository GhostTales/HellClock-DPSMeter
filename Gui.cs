using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DPSMeter
{
    // Small internal IMGUI toolkit.
    // The game's UnityEngine.IMGUIModule is stripped: GUIStyle only has normal.textColor (no backgrounds,
    // hover/active states, border, overflow), so nothing here relies on skin textures. Every control is
    // drawn manually with GUI.DrawTexture's rounded overload and driven with GUI.Button(rect, content, GUIStyle.none)
    // plus raw mouse events.
    internal static class Gui
    {
        internal static readonly Color Accent = new Color(1f, 0.635f, 0.169f);
        internal static readonly Color Primary = new Color(0xF6 / 255f, 0x4C / 255f, 0x2C / 255f);
        internal static readonly Color Text = new Color(0.9f, 0.9f, 0.9f);
        internal static readonly Color Muted = new Color(0.718f, 0.718f, 0.718f);
        internal static readonly Color Bg = new Color(0.094f, 0.086f, 0.102f, 0.95f);
        internal static readonly Color Border = new Color(0.718f, 0.718f, 0.718f);
        internal static readonly Color ControlOutline = new Color(0.055f, 0.027f, 0.153f);
        internal static readonly Color Secondary = new Color(0.412f, 0.412f, 0.412f);

        internal const float BarSize = 10f, BarGap = 4f, MinThumb = 24f;

        static readonly Dictionary<string, Texture2D> _icons = new Dictionary<string, Texture2D>();
        static Texture2D _white;

        static Texture2D White => _white != null ? _white : (_white = Solid(Color.white));
        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        // The game uses a linear colour space and IMGUI text comes out darker/more saturated than the hex value.
        // Pre-applying the inverse conversion makes text match the colour picked in Figma.
        internal static Color TextColor(Color c)
        {
            if (Plugin.LinearTextFix == null || !Plugin.LinearTextFix.Value || QualitySettings.activeColorSpace != ColorSpace.Linear) return c;
            var g = c.gamma;
            return new Color(g.r, g.g, g.b, c.a);
        }

        // Loads Textures/<fileName> (embedded in the dll) once; returns null if it is missing or invalid.
        internal static Texture2D Icon(string fileName)
        {
            if (_icons.TryGetValue(fileName, out var cached) && cached != null) return cached;

            Texture2D tex = null;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Textures." + fileName))
            {
                if (stream == null) Plugin.Logger.LogWarning($"Icon not found: {fileName}");
                else
                {
                    var data = new byte[stream.Length];
                    int read = 0;
                    while (read < data.Length) read += stream.Read(data, read, data.Length - read);
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
                    if (!tex.LoadImage(data)) { Plugin.Logger.LogWarning($"Icon is not a valid image: {fileName}"); tex = null; }
                }
            }
            return _icons[fileName] = tex;
        }

        // Wraps text in a colour tag (for FigmaText with RichText = true), with the same linear fix as plain text.
        internal static string Colored(string text, Color c) => $"<color=#{ColorUtility.ToHtmlStringRGB(TextColor(c))}>{text}</color>";

        internal static void Round(Rect r, Color c, float radius) => Round(r, c, new Vector4(radius, radius, radius, radius));

        // Per-corner radii (TL, TR, BR, BL)
        internal static void Round(Rect r, Color c, Vector4 radii)
        {
            float max = Mathf.Min(r.width, r.height) * 0.5f;
            radii = new Vector4(Mathf.Min(radii.x, max), Mathf.Min(radii.y, max), Mathf.Min(radii.z, max), Mathf.Min(radii.w, max));
            GUI.DrawTexture(r, White, ScaleMode.StretchToFill, true, 0f, c, Vector4.zero, radii);
        }

        internal static void Outline(Rect r, Color c, float width = 1f)
        {
            Round(new Rect(r.x, r.y, r.width, width), c, 0f);
            Round(new Rect(r.x, r.yMax - width, r.width, width), c, 0f);
            Round(new Rect(r.x, r.y + width, width, r.height - width * 2f), c, 0f);
            Round(new Rect(r.xMax - width, r.y + width, width, r.height - width * 2f), c, 0f);
        }

        internal static void Frame(Rect r, float radius = 20f, float borderWidth = 1f)
        {
            if (Event.current.type != EventType.Repaint) return;
            Round(r, Border, radius);
            var inner = new Rect(r.x + borderWidth, r.y + borderWidth, r.width - borderWidth * 2f, r.height - borderWidth * 2f);
            if (inner.width > 0f && inner.height > 0f) Round(inner, Bg, Mathf.Max(0f, radius - borderWidth));
        }

        internal static void Divider()
        {
            GUILayout.Space(3);
            Rect r = GUILayoutUtility.GetRect(1, 1, GUIStyle.none, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint) Round(r, Border, 0f);
            GUILayout.Space(3);
        }

        // ---- Scroll view ----

        internal class Scroll
        {
            public Vector2 Pos;
            public Vector2 Content, View;
            public Vector2 Drag; // grab offset inside thumb
            public int Axis = -1; // axis currently dragged
        }

        // width/height of 0 means "expand". Content is laid out with room reserved for the scrollbars.
        internal static void BeginScroll(Scroll s, float width, float height)
        {
            var opts = new System.Collections.Generic.List<GUILayoutOption>();
            if (width > 0) opts.Add(GUILayout.Width(width)); else opts.Add(GUILayout.ExpandWidth(true));
            if (height > 0) opts.Add(GUILayout.Height(height));
            s.Pos = GUILayout.BeginScrollView(s.Pos, false, false, GUIStyle.none, GUIStyle.none, GUIStyle.none, opts.ToArray());
            GUILayout.BeginVertical();
        }

        internal static void EndScroll(Scroll s)
        {
            bool needV = s.Content.y > s.View.y + 0.5f && s.View.y > 0;
            bool needH = s.Content.x > s.View.x + 0.5f && s.View.x > 0;
            // Constant control count: always add the spacer, only its size varies
            GUILayout.Space(needH ? BarSize + BarGap : 0f);
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint) s.Content = GUILayoutUtility.GetLastRect().size;
            GUILayout.EndScrollView();
            if (Event.current.type == EventType.Repaint)
            {
                var r = GUILayoutUtility.GetLastRect();
                s.View = r.size;
            }
            DrawBars(s, GUILayoutUtility.GetLastRect(), needV, needH);
        }

        static void DrawBars(Scroll s, Rect view, bool needV, bool needH)
        {
            int idV = GUIUtility.GetControlID(FocusType.Passive);
            int idH = GUIUtility.GetControlID(FocusType.Passive);
            if (needV)
            {
                var track = new Rect(view.xMax - BarSize, view.y, BarSize, view.height - (needH ? BarSize + BarGap : 0));
                float max = Mathf.Max(0, s.Content.y - view.height);
                s.Pos.y = Bar(idV, 1, track, s.Pos.y, view.height, s.Content.y, s);
                s.Pos.y = Mathf.Clamp(s.Pos.y, 0, max);
            }
            if (needH)
            {
                var track = new Rect(view.x, view.yMax - BarSize, view.width - (needV ? BarSize + BarGap : 0), BarSize);
                float max = Mathf.Max(0, s.Content.x - view.width);
                s.Pos.x = Bar(idH, 0, track, s.Pos.x, view.width, s.Content.x, s);
                s.Pos.x = Mathf.Clamp(s.Pos.x, 0, max);
            }
        }

        // axis 0 = horizontal, 1 = vertical
        static float Bar(int id, int axis, Rect track, float pos, float viewLen, float contentLen, Scroll s)
        {
            bool vert = axis == 1;
            float trackLen = vert ? track.height : track.width;
            float scrollable = Mathf.Max(1f, contentLen - viewLen);
            float thumbLen = Mathf.Clamp(trackLen * viewLen / contentLen, MinThumb, trackLen);
            float free = trackLen - thumbLen;
            float off = free * Mathf.Clamp01(pos / scrollable);
            Rect thumb = vert ? new Rect(track.x, track.y + off, track.width, thumbLen) : new Rect(track.x + off, track.y, thumbLen, track.height);

            var e = Event.current;
            bool dragging = GUIUtility.hotControl == id;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && track.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        s.Axis = axis;
                        if (thumb.Contains(e.mousePosition)) s.Drag = e.mousePosition - thumb.position;
                        else
                        {
                            // click on track: jump so the thumb centres on the cursor
                            s.Drag = new Vector2(thumbLen * 0.5f, thumbLen * 0.5f);
                            pos = ToPos(e.mousePosition, track, vert, s.Drag, free, scrollable);
                        }
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (dragging)
                    {
                        pos = ToPos(e.mousePosition, track, vert, s.Drag, free, scrollable);
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (dragging) { GUIUtility.hotControl = 0; e.Use(); }
                    break;
                case EventType.Repaint:
                    bool hover = thumb.Contains(e.mousePosition);
                    Round(track, ControlOutline, BarSize * 0.5f);
                    Round(new Rect(track.x + 1f, track.y + 1f, track.width - 2f, track.height - 2f), Secondary, (BarSize - 2f) * 0.5f);
                    Color c = dragging || hover ? Accent : Border;
                    Round(thumb, c, BarSize * 0.5f);
                    break;
            }
            return pos;
        }

        static float ToPos(Vector2 mouse, Rect track, bool vert, Vector2 grab, float free, float scrollable)
        {
            if (free <= 0) return 0;
            float t = vert ? mouse.y - grab.y - track.y : mouse.x - grab.x - track.x;
            return Mathf.Clamp01(t / free) * scrollable;
        }
    }
}
