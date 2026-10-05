using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

namespace DPSMeter
{
    internal class MeterWindow : MonoBehaviour
    {
        const float Width = 300f;
        const float LeftColW = 135f, RightColW = 135f;

        Rect _rect = new Rect(20, 200, Width, 120);
        bool _visible = true;
        int _tab;
        static readonly string[] Tabs = { "Skills", "Damage Types" };

        GUIStyle _pct, _label, _right, _title, _hint, _big, _small, _tabStyle;
        const float Pad = 10f;
        float _contentH = 100;

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

        static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        internal static bool IsMouseOver;
        static Rect _screenRect, _advScreenRect;
        static bool _shown;

        Rect _advRect = new Rect(340, 200, 640, 480);
        string _advSkill;
        string _advKey;
        readonly Gui.Scroll _listScroll = new Gui.Scroll(), _chainScroll = new Gui.Scroll();

        const float Grip = 14f, MinW = 420f, MinH = 300f;
        int _corner = -1;
        Vector2 _resizeStartMouse;
        Rect _resizeStartRect;

        void UpdateResize(Vector2 mp)
        {
            if (_advSkill == null) { _corner = -1; return; }
            if (_corner < 0 && Input.GetMouseButtonDown(0))
            {
                var r = _advRect;
                bool l = Mathf.Abs(mp.x - r.xMin) <= Grip, rt = Mathf.Abs(mp.x - r.xMax) <= Grip;
                bool t = Mathf.Abs(mp.y - r.yMin) <= Grip, b = Mathf.Abs(mp.y - r.yMax) <= Grip;
                if ((l || rt) && (t || b))
                {
                    _corner = (rt ? 1 : 0) + (b ? 2 : 0);
                    _resizeStartMouse = mp;
                    _resizeStartRect = r;
                }
            }
            if (_corner < 0) return;
            if (!Input.GetMouseButton(0)) { _corner = -1; return; }
            var d = mp - _resizeStartMouse;
            var s = _resizeStartRect;
            float xMin = s.xMin, xMax = s.xMax, yMin = s.yMin, yMax = s.yMax;
            if ((_corner & 1) == 0) xMin = Mathf.Min(s.xMin + d.x, xMax - MinW); else xMax = Mathf.Max(s.xMax + d.x, xMin + MinW);
            if ((_corner & 2) == 0) yMin = Mathf.Min(s.yMin + d.y, yMax - MinH); else yMax = Mathf.Max(s.yMax + d.y, yMin + MinH);
            _advRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        void Update()
        {
            var m = Input.mousePosition;
            var mp = new Vector2(m.x, Screen.height - m.y);
            IsMouseOver = _shown && (_screenRect.Contains(mp) || (_advSkill != null && _advScreenRect.Contains(mp)));
            UpdateResize(mp);
            if (Input.GetKeyDown(Plugin.ToggleKey.Value)) _visible = !_visible;
            if (Input.GetKeyDown(Plugin.ResetKey.Value)) Tracker.Reset();
        }

        void Init()
        {
            _label = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true, wordWrap = false };
            _label.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            _right = new GUIStyle(_label) { alignment = TextAnchor.MiddleRight };
            _pct = new GUIStyle(_right) { fontStyle = FontStyle.Bold };
            _pct.normal.textColor = new Color(1f, 0.9f, 0.6f);
            _small = new GUIStyle(_label) { fontSize = 10 };
            _small.normal.textColor = new Color(0.65f, 0.65f, 0.7f);
            _hint = new GUIStyle(_small) { alignment = TextAnchor.MiddleCenter };
            _title = new GUIStyle(_label) { fontSize = 14, fontStyle = FontStyle.Bold };
            _title.normal.textColor = new Color(1f, 0.82f, 0.45f);
            _big = new GUIStyle(_label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _big.normal.textColor = Color.white;

            _vtab = new GUIStyle(_label) { fontSize = 12, richText = true, alignment = TextAnchor.MiddleLeft };
            _vtabSel = new GUIStyle(_vtab);
            _fold = new GUIStyle(_label) { alignment = TextAnchor.MiddleLeft };
            _tabStyle = new GUIStyle(_label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }

        void OnGUI()
        {
            _shown = _visible;
            if (!_visible) return;
            if (_label == null) Init();

            _rect.width = Width;
            _rect.height = _contentH + Pad * 2;
            _rect = GUI.Window(918273, _rect, Draw, "", GUIStyle.none);
            _screenRect = _rect;
            if (_advSkill != null)
            {
                _advRect.width = Mathf.Clamp(_advRect.width, Mathf.Min(MinW, Screen.width), Screen.width);
                _advRect.height = Mathf.Clamp(_advRect.height, Mathf.Min(MinH, Screen.height), Screen.height);
                _advRect = GUI.Window(918274, _advRect, DrawAdvanced, "", GUIStyle.none);
                _advScreenRect = _advRect;
                _advRect.x = Mathf.Clamp(_advRect.x, 0, Screen.width - 40);
                _advRect.y = Mathf.Clamp(_advRect.y, 0, Screen.height - 40);
            }
            _rect.x = Mathf.Clamp(_rect.x, 0, Screen.width - 40);
            _rect.y = Mathf.Clamp(_rect.y, 0, Screen.height - 40);
        }

        void Draw(int id)
        {
            if (Event.current.type == EventType.Repaint)
            {
                var full = new Rect(0, 0, _rect.width, _rect.height);
                Round(full, new Color(1f, 1f, 1f, 0.16f), 10f);
                Round(new Rect(1, 1, full.width - 2, full.height - 2), new Color(0.07f, 0.07f, 0.09f, 0.95f), 9f);
            }
            GUILayout.BeginArea(new Rect(Pad, Pad, Width - Pad * 2, 4000));
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("DPS METER", _title);
            GUILayout.FlexibleSpace();
            GUILayout.Label(Tracker.Elapsed.ToString("0.0") + "s", _right, GUILayout.Width(60));
            GUILayout.EndHorizontal();
            Divider();

            // Headline numbers
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(LeftColW));
            GUILayout.Label(Fmt(Tracker.Dps), _big, GUILayout.Width(LeftColW));
            GUILayout.Label("damage per second", _small, GUILayout.Width(LeftColW));
            GUILayout.Label("Peak <color=#ffd27a>" + Fmt(Tracker.PeakDps) + "</color>", _small, GUILayout.Width(LeftColW));
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUILayout.Width(RightColW));
            Stat("Total", Fmt(Tracker.Total));
            Stat("Highest hit", Fmt(Tracker.Highest));
            Stat("Hits", Tracker.Hits.ToString());
            Stat("Crit rate", (Tracker.Hits > 0 ? 100.0 * Tracker.Crits / Tracker.Hits : 0).ToString("0.#") + "%");
            Stat("Avg hit", Fmt(Tracker.Hits > 0 ? Tracker.Total / Tracker.Hits : 0));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (Plugin.ShowBreakdown.Value)
            {
                Divider();
                DrawTabs();
                GUILayout.Space(4);

                if (Tracker.Total <= 0) GUILayout.Label("Waiting for damage...", _hint);
                else if (_tab == 0) DrawSkills();
                else DrawTypes();
            }

            if (Event.current.type == EventType.Layout) { _hoverShown = _hover; _hover = ""; }
            Divider();
            GUILayout.Label(_hoverShown.Length > 0 ? _hoverShown : " ", _hint);
            GUILayout.Label($"{Plugin.ToggleKey.Value}: hide   {Plugin.ResetKey.Value}: reset   drag to move", _hint);
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint) _contentH = GUILayoutUtility.GetLastRect().height;
            GUILayout.EndArea();
            GUI.DragWindow();
        }

        static void Round(Rect r, Color c, float radius) => Gui.Round(r, c, radius);
        static void Round(Rect r, Color c, Vector4 radii) => Gui.Round(r, c, radii);

        static string TypeSplit(double[] by)
        {
            double sum = by.Sum();
            if (sum <= 0) return "";
            var parts = Enumerable.Range(0, Tracker.TypeCount).Where(i => by[i] > 0).OrderByDescending(i => by[i])
                .Select(i => $"{Tracker.TypeNames[i]} {100.0 * by[i] / sum:0}%");
            return string.Join(" / ", parts);
        }

        void DrawTabs()
        {
            Rect row = GUILayoutUtility.GetRect(1, 26, GUIStyle.none, GUILayout.ExpandWidth(true));
            float w = row.width / Tabs.Length;
            var accent = new Color(1f, 0.45f, 0.3f);
            for (int i = 0; i < Tabs.Length; i++)
            {
                var r = new Rect(row.x + i * w, row.y, w, row.height);
                bool active = i == _tab;
                bool hover = r.Contains(Event.current.mousePosition);
                if (GUI.Button(r, new GUIContent(""), GUIStyle.none)) _tab = i;
                if (Event.current.type != EventType.Repaint) continue;

                var old = GUI.color;
                if (active || hover)
                    Round(new Rect(r.x + 2, r.y, r.width - 4, r.height - 3), new Color(1f, 1f, 1f, active ? 0.09f : 0.04f), 5f);
                Round(new Rect(r.x + 2, r.yMax - 2, r.width - 4, active ? 2 : 1),
                    active ? accent : new Color(1f, 1f, 1f, hover ? 0.35f : 0.12f), 1f);
                _tabStyle.normal.textColor = active ? Color.white : new Color(0.65f, 0.65f, 0.7f);
                GUI.Label(r, Tabs[i], _tabStyle);
            }
        }

        void DrawSkills()
        {
            foreach (var kv in Tracker.Skills.OrderByDescending(k => k.Value.Total).Take(8))
            {
                var e = kv.Value;
                string split = TypeSplit(e.ByType);
                if (Bar(kv.Key, Fmt(e.Total), 100.0 * e.Total / Tracker.Total, e.ByType,
                    $"{split} · {e.Hits} hits · {(e.Hits > 0 ? 100.0 * e.Crits / e.Hits : 0):0}% crit · max {Fmt(e.Max)} · click for details"))
                {
                    _advSkill = _advSkill == kv.Key ? null : kv.Key;
                    _advKey = null;
                }
            }
        }

        void DrawTypes()
        {
            var order = Enumerable.Range(0, Tracker.TypeCount).Where(i => Tracker.TypeTotals[i] > 0)
                .OrderByDescending(i => Tracker.TypeTotals[i]);
            foreach (int i in order)
            {
                double t = Tracker.TypeTotals[i];
                var by = new double[Tracker.TypeCount];
                by[i] = t;
                Bar(Tracker.TypeNames[i], Fmt(t), 100.0 * t / Tracker.Total, by,
                    $"{Tracker.TypeHits[i]} hits · avg {Fmt(t / Math.Max(1, Tracker.TypeHits[i]))}");
            }
            if (Tracker.DotTotal > 0)
            {
                GUILayout.Space(2);
                Stat("Damage over time", Fmt(Tracker.DotTotal) + " (" + (100.0 * Tracker.DotTotal / Tracker.Total).ToString("0") + "%)");
            }
        }

        void ShadowLabel(Rect r, string text, GUIStyle style)
        {
            var old = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.9f);
            GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), text, style);
            GUI.color = old;
            GUI.Label(r, text, style);
        }

        // Bar filled proportional to pct, split into coloured segments by damage type
        bool Bar(string name, string value, double pct, double[] byType, string tooltip)
        {
            Rect r = GUILayoutUtility.GetRect(1, 22, GUIStyle.none, GUILayout.ExpandWidth(true));
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            const float pctW = 40f, valW = 56f;
            if (Event.current.type == EventType.Repaint)
            {
                Round(r, new Color(1f, 1f, 1f, 0.06f), 5f);
                double sum = byType.Sum();
                float w = r.width * Mathf.Clamp01((float)(pct / 100.0));
                float x = r.x;
                int first = -1, last = -1;
                for (int i = 0; i < Tracker.TypeCount; i++)
                    if (byType[i] > 0) { if (first < 0) first = i; last = i; }
                for (int i = 0; i < Tracker.TypeCount && sum > 0; i++)
                {
                    if (byType[i] <= 0) continue;
                    float sw = w * (float)(byType[i] / sum);
                    var c = Tracker.TypeColors[i];
                    float rl = i == first ? 5f : 0f, rr = i == last ? 5f : 0f;
                    Round(new Rect(x, r.y, sw, r.height), new Color(c.r, c.g, c.b, 0.55f), new Vector4(rl, rr, rr, rl));
                    x += sw;
                }
            }
            float right = r.xMax - 5;
            ShadowLabel(new Rect(right - pctW, r.y, pctW, r.height), pct.ToString("0") + "%", _pct);
            ShadowLabel(new Rect(right - pctW - valW, r.y, valW, r.height), value, _right);
            ShadowLabel(new Rect(r.x + 6, r.y, r.width - pctW - valW - 14, r.height), name, _label);
            GUILayout.Space(2);
            if (Event.current.type == EventType.Repaint && r.Contains(Event.current.mousePosition))
                _hover = tooltip;
            return clicked;
        }

        float _copiedUntil;

        void CopyText(Tracker.Entry entry, bool all)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Hell Clock DPS Meter - " + _advSkill);
            foreach (var kv in entry.Uniques.OrderByDescending(k => k.Value.Value))
            {
                if (!all && kv.Key != _advKey) continue;
                var u = kv.Value;
                sb.AppendLine();
                sb.AppendLine($"== {u.Value:N0}{(u.Crit ? " (crit)" : "")} - seen {u.Count} times ==");
                foreach (var line in u.Chain)
                    sb.AppendLine(System.Text.RegularExpressions.Regex.Replace(line, "<[^>]*>", "").TrimEnd());
            }
            GUIUtility.systemCopyBuffer = sb.ToString();
            _copiedUntil = Time.unscaledTime + 2f;
        }

        static int Indent(string l)
        {
            int n = 0;
            while (n < l.Length && l[n] == ' ') n++;
            return n;
        }

        // Indentation defines the hierarchy; a line with a deeper line after it is collapsible
        void DrawChain(List<string> chain)
        {
            int hidden = int.MaxValue;
            for (int i = 0; i < chain.Count; i++)
            {
                string line = chain[i];
                if (line.Length == 0) { if (hidden == int.MaxValue) GUILayout.Space(6); continue; }
                int ind = Indent(line);
                if (ind <= hidden) hidden = int.MaxValue;
                if (hidden != int.MaxValue) continue;

                int j = i + 1;
                while (j < chain.Count && chain[j].Length == 0) j++;
                bool parent = j < chain.Count && Indent(chain[j]) > ind;
                string id = _advSkill + "|" + _advKey + "|" + i;
                bool closed = parent && _collapsed.Contains(id);

                GUILayout.BeginHorizontal();
                GUILayout.Space(ind * 3f);
                if (parent)
                {
                    if (Gui.IconButton(closed ? "\u25B6" : "\u25BC"))
                    {
                        if (closed) _collapsed.Remove(id); else _collapsed.Add(id);
                    }
                }
                else GUILayout.Space(ind == 0 ? 0f : 14f);
                GUILayout.Label(line.TrimStart(), _label);
                GUILayout.EndHorizontal();
                if (closed) hidden = ind;
            }
        }

        void DrawAdvanced(int id)
        {
            if (Event.current.type == EventType.Repaint)
            {
                var full = new Rect(0, 0, _advRect.width, _advRect.height);
                Round(full, new Color(1f, 1f, 1f, 0.16f), 10f);
                Round(new Rect(1, 1, full.width - 2, full.height - 2), new Color(0.07f, 0.07f, 0.09f, 0.97f), 9f);
            }
            if (Event.current.type == EventType.Repaint)
            {
                var gc = new Color(1f, 1f, 1f, 0.35f);
                foreach (var (gx, gy) in new[] { (0f, 0f), (1f, 0f), (0f, 1f), (1f, 1f) })
                    for (int k = 1; k <= 3; k++)
                    {
                        float ox = gx == 0 ? 3 + k * 3 : _advRect.width - 3 - k * 3, oy = gy == 0 ? 3 : _advRect.height - 3;
                        Round(new Rect(ox - 1, oy - (gy == 0 ? 0 : 1), 2, 2), gc, 1f);
                    }
            }
            Tracker.Skills.TryGetValue(_advSkill ?? "", out var entry);
            float w = _advRect.width - Pad * 2, h = _advRect.height - Pad * 2;
            const float listW = 200f;
            GUILayout.BeginArea(new Rect(Pad, Pad, w, h));
            GUILayout.BeginHorizontal();
            GUILayout.Label("ADVANCED: " + _advSkill, _title);
            GUILayout.FlexibleSpace();
            if (Time.unscaledTime < _copiedUntil) GUILayout.Label("<color=#8fe38f>Copied!</color>", _small);
            if (entry != null && _advKey != null && Gui.Button("Copy", 60)) CopyText(entry, false);
            if (entry != null && Gui.Button("Copy all", 70)) CopyText(entry, true);
            if (Gui.Button("Close", 60, accent: true)) _advSkill = null;
            GUILayout.EndHorizontal();
            Divider();

            if (entry == null || _advSkill == null)
            {
                GUILayout.Label("No data for this skill (meter was reset).", _hint);
                GUILayout.EndArea();
                GUI.DragWindow();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(listW));
            GUILayout.Label($"Unique damage numbers ({entry.Uniques.Count})", _small);
            Gui.BeginScroll(_listScroll, listW, h - 70);
            foreach (var kv in entry.Uniques.OrderByDescending(k => k.Value.Value))
            {
                var u = kv.Value;
                bool sel = kv.Key == _advKey;
                string text = (u.Crit ? "<color=#ffd27a>" : "") + u.Value.ToString("N0") + (u.Crit ? " crit</color>" : "") + $"  <color=#9a9aa5>x{u.Count}</color>";
                var tr = GUILayoutUtility.GetRect(1, 24, GUIStyle.none, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint)
                {
                    bool hov = tr.Contains(Event.current.mousePosition);
                    Round(tr, sel ? new Color(1f, 0.45f, 0.3f, 0.4f) : new Color(1f, 1f, 1f, hov ? 0.14f : 0.06f), 4f);
                }
                GUI.Label(new Rect(tr.x + 10, tr.y, tr.width - 10, tr.height), text, _vtab);
                if (GUI.Button(tr, GUIContent.none, GUIStyle.none)) _advKey = kv.Key;
                GUILayout.Space(2);
            }
            Gui.EndScroll(_listScroll);
            GUILayout.EndVertical();

            GUILayout.Space(8);
            GUILayout.BeginVertical();
            Gui.BeginScroll(_chainScroll, 0, h - 50);
            if (_advKey != null && entry.Uniques.TryGetValue(_advKey, out var sel2))
            {
                GUILayout.Label($"<b>{sel2.Value:N0}</b>{(sel2.Crit ? " (crit)" : "")}  seen {sel2.Count} times", _title);
                GUILayout.Space(4);
                DrawChain(sel2.Chain);
            }
            else GUILayout.Label("Select a damage number to see the modifier chain.", _hint);
            Gui.EndScroll(_chainScroll);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUI.DragWindow(new Rect(Grip, Grip, _advRect.width - 80 - Grip, 28 - Grip));
        }

        GUIStyle _vtab, _vtabSel, _fold;
        readonly HashSet<string> _collapsed = new HashSet<string>();

        string _hover = "", _hoverShown = "";

        void Stat(string l, string r)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(l, _small);
            GUILayout.FlexibleSpace();
            GUILayout.Label(r, _right);
            GUILayout.EndHorizontal();
        }

        static void Divider() => Gui.Divider();
    }
}
