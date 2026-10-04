using System;
using System.Linq;
using UnityEngine;

namespace DPSMeter
{
    internal class MeterWindow : MonoBehaviour
    {
        const float Width = 300f;

        Rect _rect = new Rect(20, 200, Width, 120);
        bool _visible = true;
        int _tab;
        static readonly string[] Tabs = { "Skills", "Damage Types" };

        GUIStyle _pct, _label, _right, _title, _hint, _big, _small, _tabBtn;
        float _lastH = 100;
        Texture2D _bg, _panel, _bar, _line;

        static Texture2D Tex(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        static string Fmt(double v)
        {
            if (v >= 1e9) return (v / 1e9).ToString("0.##") + "B";
            if (v >= 1e6) return (v / 1e6).ToString("0.##") + "M";
            if (v >= 1e3) return (v / 1e3).ToString("0.##") + "K";
            return v.ToString("0");
        }

        static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        void Update()
        {
            if (Input.GetKeyDown(Plugin.ToggleKey.Value)) _visible = !_visible;
            if (Input.GetKeyDown(Plugin.ResetKey.Value)) Tracker.Reset();
        }

        void Init()
        {
            _bg = Tex(new Color(0.07f, 0.07f, 0.09f, 0.94f));
            _panel = Tex(new Color(1f, 1f, 1f, 0.06f));
            _bar = Tex(Color.white);
            _line = Tex(new Color(1f, 1f, 1f, 0.15f));

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

            _tabBtn = new GUIStyle(GUI.skin.button) { fontSize = 11, fontStyle = FontStyle.Bold };
        }

        void OnGUI()
        {
            if (!_visible) return;
            if (_label == null || _bg == null) Init();

            _rect.height = 0;
            _rect = GUILayout.Window(918273, _rect, Draw, "", GUILayout.Width(Width));
            _lastH = _rect.height;
            _rect.x = Mathf.Clamp(_rect.x, 0, Screen.width - 40);
            _rect.y = Mathf.Clamp(_rect.y, 0, Screen.height - 40);
        }

        void Draw(int id)
        {
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(new Rect(0, 0, Width, Mathf.Max(_lastH, 40)), _bg);
            GUILayout.BeginHorizontal();
            GUILayout.Label("DPS METER", _title);
            GUILayout.FlexibleSpace();
            GUILayout.Label(Tracker.Elapsed.ToString("0.0") + "s", _right, GUILayout.Width(60));
            GUILayout.EndHorizontal();
            Divider();

            // Headline numbers
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label(Fmt(Tracker.Dps), _big);
            GUILayout.Label("damage per second", _small);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUILayout.Width(140));
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
                GUILayout.BeginHorizontal();
                for (int i = 0; i < Tabs.Length; i++)
                {
                    var old = GUI.color;
                    GUI.color = i == _tab ? new Color(1f, 0.45f, 0.35f) : new Color(1f, 1f, 1f, 0.6f);
                    if (GUILayout.Button(Tabs[i], _tabBtn)) _tab = i;
                    GUI.color = old;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4);

                if (Tracker.Total <= 0) GUILayout.Label("Waiting for damage...", _hint);
                else if (_tab == 0) DrawSkills();
                else DrawTypes();
            }

            if (Event.current.type == EventType.Layout) { _hoverShown = _hover; _hover = ""; }
            Divider();
            GUILayout.Label(_hoverShown.Length > 0 ? _hoverShown : " ", _hint);
            GUILayout.Label($"{Plugin.ToggleKey.Value}: hide   {Plugin.ResetKey.Value}: reset   drag to move", _hint);
            GUI.DragWindow();
        }

        static string TypeSplit(double[] by)
        {
            double sum = by.Sum();
            if (sum <= 0) return "";
            var parts = Enumerable.Range(0, Tracker.TypeCount).Where(i => by[i] > 0).OrderByDescending(i => by[i])
                .Select(i => $"{Tracker.TypeNames[i]} {100.0 * by[i] / sum:0}%");
            return string.Join(" / ", parts);
        }

        void DrawSkills()
        {
            foreach (var kv in Tracker.Skills.OrderByDescending(k => k.Value.Total).Take(8))
            {
                var e = kv.Value;
                string split = TypeSplit(e.ByType);
                Bar(kv.Key, Fmt(e.Total), 100.0 * e.Total / Tracker.Total, e.ByType,
                    $"{split} · {e.Hits} hits · {(e.Hits > 0 ? 100.0 * e.Crits / e.Hits : 0):0}% crit · max {Fmt(e.Max)}");
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
        void Bar(string name, string value, double pct, double[] byType, string tooltip)
        {
            Rect r = GUILayoutUtility.GetRect(1, 22, GUIStyle.none, GUILayout.ExpandWidth(true));
            const float pctW = 40f, valW = 56f;
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(r, _panel);
                double sum = byType.Sum();
                float w = r.width * Mathf.Clamp01((float)(pct / 100.0));
                float x = r.x;
                var old = GUI.color;
                for (int i = 0; i < Tracker.TypeCount && sum > 0; i++)
                {
                    if (byType[i] <= 0) continue;
                    float sw = w * (float)(byType[i] / sum);
                    var c = Tracker.TypeColors[i];
                    GUI.color = new Color(c.r, c.g, c.b, 0.4f);
                    GUI.DrawTexture(new Rect(x, r.y, sw, r.height), _bar);
                    GUI.color = new Color(c.r, c.g, c.b, 1f);
                    GUI.DrawTexture(new Rect(x, r.yMax - 2, sw, 2), _bar);
                    x += sw;
                }
                GUI.color = old;
            }
            float right = r.xMax - 5;
            ShadowLabel(new Rect(right - pctW, r.y, pctW, r.height), pct.ToString("0") + "%", _pct);
            ShadowLabel(new Rect(right - pctW - valW, r.y, valW, r.height), value, _right);
            ShadowLabel(new Rect(r.x + 6, r.y, r.width - pctW - valW - 14, r.height), name, _label);
            GUILayout.Space(2);
            if (Event.current.type == EventType.Repaint && r.Contains(Event.current.mousePosition))
                _hover = tooltip;
        }

        string _hover = "", _hoverShown = "";

        void Stat(string l, string r)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(l, _small);
            GUILayout.FlexibleSpace();
            GUILayout.Label(r, _right);
            GUILayout.EndHorizontal();
        }

        void Divider()
        {
            GUILayout.Space(3);
            Rect r = GUILayoutUtility.GetRect(1, 1, GUIStyle.none, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint) GUI.DrawTexture(r, _line);
            GUILayout.Space(3);
        }
    }
}
