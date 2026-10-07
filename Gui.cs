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

        static Texture2D _white;
        static GUIStyle _btnLabel;

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

        internal enum ButtonKind
        {
            Primary,
            Secondary
        }

        internal static void Divider()
        {
            GUILayout.Space(3);
            Rect r = GUILayoutUtility.GetRect(1, 1, GUIStyle.none, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint) Round(r, Border, 0f);
            GUILayout.Space(3);
        }

        // ---- Buttons ----

        static GUIStyle BtnLabel
        {
            get
            {
                if (_btnLabel == null)
                {
                    _btnLabel = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true, alignment = TextAnchor.MiddleCenter, wordWrap = false };
                }
                return _btnLabel;
            }
        }

        internal static bool FigmaButtonAt(Rect r, string text, ButtonKind kind, int fontSize = 20)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            bool down = hover && Input.GetMouseButton(0);
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            if (Event.current.type == EventType.Repaint)
            {
                Color fill = kind == ButtonKind.Primary ? Accent : Secondary;
                if (down) fill = Color.Lerp(fill, ControlOutline, 0.2f);
                else if (hover) fill = Color.Lerp(fill, Color.white, 0.12f);
                Round(r, ControlOutline, 10f);
                Round(new Rect(r.x + 1f, r.y + 1f, r.width - 2f, r.height - 2f), fill, 9f);
                var style = BtnLabel;
                style.fontSize = fontSize;
                style.fontStyle = FontStyle.Normal;
                style.normal.textColor = TextColor(ControlOutline);
                GUI.Label(r, text, style);
            }
            return clicked;
        }

        internal static void ProgressBarAt(Rect r, float value, Color fill)
        {
            if (Event.current.type != EventType.Repaint) return;
            Round(r, ControlOutline, 10f);
            var track = new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f);
            Round(track, Secondary, 8f);
            float fillWidth = track.width * Mathf.Clamp01(value);
            if (fillWidth > 0f)
                Round(new Rect(track.x, track.y, fillWidth, track.height), fill, Mathf.Min(8f, fillWidth * 0.5f));
        }

        internal static void SegmentedProgressBarAt(Rect r, float value, double[] segments, Color[] colors)
        {
            if (Event.current.type != EventType.Repaint) return;
            Round(r, ControlOutline, 10f);
            var track = new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f);
            Round(track, Secondary, 8f);

            double total = 0;
            for (int i = 0; i < segments.Length; i++) total += Mathf.Max(0f, (float)segments[i]);
            float width = track.width * Mathf.Clamp01(value);
            if (total <= 0 || width <= 0f) return;

            float x = track.x;
            for (int i = 0; i < segments.Length && i < colors.Length; i++)
            {
                if (segments[i] <= 0) continue;
                float segmentWidth = width * (float)(segments[i] / total);
                if (segmentWidth <= 0f) continue;
                float radius = Mathf.Min(8f, segmentWidth * 0.5f);
                Round(new Rect(x, track.y, segmentWidth, track.height), colors[i], radius);
                x += segmentWidth;
            }
        }

        // width 0 = auto-expand to fit the layout row
        internal static bool Button(string text, float width = 0, bool accent = false)
        {
            Rect r = width > 0
                ? GUILayoutUtility.GetRect(width, 22, GUIStyle.none, GUILayout.Width(width))
                : GUILayoutUtility.GetRect(1, 22, GUIStyle.none, GUILayout.ExpandWidth(true));
            return FigmaButtonAt(r, text, accent ? ButtonKind.Primary : ButtonKind.Secondary, 12);
        }

        internal static bool IconButton(string glyph, float size = 16f)
        {
            Rect r = GUILayoutUtility.GetRect(size, size, GUIStyle.none, GUILayout.Width(size), GUILayout.Height(size));
            return FigmaButtonAt(r, glyph, ButtonKind.Secondary, 10);
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
