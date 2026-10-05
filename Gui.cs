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
        internal static readonly Color Accent = new Color(1f, 0.45f, 0.3f);
        internal static readonly Color Gold = new Color(1f, 0.82f, 0.45f);
        internal static readonly Color Text = new Color(0.9f, 0.9f, 0.9f);
        internal static readonly Color Muted = new Color(0.65f, 0.65f, 0.7f);
        internal static readonly Color Bg = new Color(0.07f, 0.07f, 0.09f, 0.95f);
        internal static readonly Color Border = new Color(1f, 1f, 1f, 0.16f);

        internal const float BarSize = 8f, BarGap = 4f, MinThumb = 24f;

        static Texture2D _white, _line;
        static GUIStyle _btnLabel;

        static Texture2D White => _white != null ? _white : (_white = Solid(Color.white));
        static Texture2D Line => _line != null ? _line : (_line = Solid(new Color(1f, 1f, 1f, 0.15f)));

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        internal static void Round(Rect r, Color c, float radius) => Round(r, c, new Vector4(radius, radius, radius, radius));

        // Per-corner radii (TL, TR, BR, BL)
        internal static void Round(Rect r, Color c, Vector4 radii)
        {
            float max = Mathf.Min(r.width, r.height) * 0.5f;
            radii = new Vector4(Mathf.Min(radii.x, max), Mathf.Min(radii.y, max), Mathf.Min(radii.z, max), Mathf.Min(radii.w, max));
            GUI.DrawTexture(r, White, ScaleMode.StretchToFill, true, 0f, c, Vector4.zero, radii);
        }

        internal static void Divider()
        {
            GUILayout.Space(3);
            Rect r = GUILayoutUtility.GetRect(1, 1, GUIStyle.none, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint) GUI.DrawTexture(r, Line);
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

        // Draws a rounded button into r; returns true when clicked
        internal static bool ButtonAt(Rect r, string text, bool accent = false, bool flat = false, int fontSize = 12)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            bool down = hover && Input.GetMouseButton(0);
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            if (Event.current.type == EventType.Repaint)
            {
                Color c = accent ? Accent : Color.white;
                float a = accent ? (down ? 0.65f : hover ? 0.5f : 0.3f) : (down ? 0.24f : hover ? 0.16f : flat ? 0f : 0.08f);
                if (a > 0) Round(r, new Color(c.r, c.g, c.b, a), 5f);
                var st = BtnLabel;
                st.fontSize = fontSize;
                st.normal.textColor = hover ? Color.white : Text;
                GUI.Label(r, text, st);
            }
            return clicked;
        }

        // width 0 = auto-expand to fit the layout row
        internal static bool Button(string text, float width = 0, bool accent = false)
        {
            Rect r = width > 0
                ? GUILayoutUtility.GetRect(width, 22, GUIStyle.none, GUILayout.Width(width))
                : GUILayoutUtility.GetRect(1, 22, GUIStyle.none, GUILayout.ExpandWidth(true));
            return ButtonAt(r, text, accent);
        }

        // Small borderless square button (fold arrows etc.)
        internal static bool IconButton(string glyph, float size = 16f)
        {
            Rect r = GUILayoutUtility.GetRect(size, size, GUIStyle.none, GUILayout.Width(size), GUILayout.Height(size));
            return ButtonAt(r, glyph, flat: true, fontSize: 10);
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
                    Round(track, new Color(1f, 1f, 1f, 0.05f), BarSize * 0.5f);
                    Color c = dragging ? new Color(Accent.r, Accent.g, Accent.b, 0.9f) : new Color(1f, 1f, 1f, hover ? 0.45f : 0.28f);
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
