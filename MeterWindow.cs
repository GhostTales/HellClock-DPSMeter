using System;
using System.Linq;
using UnityEngine;

namespace DPSMeter
{
    internal class MeterWindow : MonoBehaviour
    {
        const float W = 300f;
        Rect _rect = new Rect(20, 200, W, 100);
        bool _visible = true;
        GUIStyle _win, _title, _big, _small, _name, _val, _hint;
        Texture2D _accent, _bg, _header, _card, _bar, _track;

        static readonly Color Accent = new Color(0.85f, 0.25f, 0.2f);

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

        void Update()
        {
            if (Input.GetKeyDown(Plugin.ToggleKey.Value)) _visible = !_visible;
            if (Input.GetKeyDown(Plugin.ResetKey.Value)) Tracker.Reset();
        }

        void Init()
        {
            if (_bg != null) return;
            _accent = Tex(Accent);
            _bg = Tex(new Color(0.07f, 0.06f, 0.06f, 0.92f));
            _header = Tex(new Color(0.35f, 0.1f, 0.08f, 1f));
            _card = Tex(new Color(1f, 1f, 1f, 0.06f));
            _bar = Tex(new Color(0.75f, 0.22f, 0.18f, 0.85f));
            _track = Tex(new Color(1f, 1f, 1f, 0.05f));

            _title = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _title.normal.textColor = new Color(1f, 0.85f, 0.7f);
            _big = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _big.normal.textColor = Color.white;
            _small = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
            _small.normal.textColor = new Color(0.7f, 0.65f, 0.6f);
            _name = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
            _name.normal.textColor = Color.white;
            _val = new GUIStyle(_name) { alignment = TextAnchor.MiddleRight };
            _hint = new GUIStyle(_small) { fontSize = 9 };
        }

        void OnGUI()
        {
            if (!_visible) return;
            Init();
            if (_win == null)
            {
                _win = new GUIStyle(GUI.skin.window);
                _win.normal.background = _bg;
                _win.onNormal.background = _bg;
                _win.padding = new RectOffset(0, 0, 0, 6);
            }
            var ws = _win;
            _rect.height = 0;
            _rect = GUILayout.Window(918273, _rect, Draw, GUIContent.none, ws, GUILayout.Width(W));
            _rect.x = Mathf.Clamp(_rect.x, 0, Screen.width - W);
            _rect.y = Mathf.Clamp(_rect.y, 0, Screen.height - 40);
        }

        void Draw(int id)
        {
            // Header
            var h = GUILayoutUtility.GetRect(W, 26);
            GUI.DrawTexture(h, _header);
            GUI.DrawTexture(new Rect(h.x, h.yMax - 2, h.width, 2), _accent);
            GUI.Label(new Rect(h.x + 8, h.y, 160, h.height), "DPS METER", _title);
            bool active = Tracker.Start >= 0f && Time.time - Tracker.Last <= Plugin.IdleTimeout.Value;
            GUI.Label(new Rect(h.xMax - 108, h.y, 100, h.height), active ? "● IN COMBAT" : "○ IDLE", _val);
            GUI.DragWindow(h);

            GUILayout.Space(6);

            // Stat cards
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            Card("DPS", Fmt(Tracker.Dps));
            Card("TOTAL", Fmt(Tracker.Total));
            Card("TIME", Tracker.Elapsed.ToString("0.0") + "s");
            GUILayout.Space(6);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            Card("BEST HIT", Fmt(Tracker.Highest));
            Card("HITS", Tracker.Hits.ToString());
            Card("CRIT", (Tracker.Hits > 0 ? 100.0 * Tracker.Crits / Tracker.Hits : 0).ToString("0") + "%");
            GUILayout.Space(6);
            GUILayout.EndHorizontal();

            if (Plugin.ShowBreakdown.Value)
            {
                GUILayout.Space(8);
                var sh = GUILayoutUtility.GetRect(W, 16);
                GUI.Label(new Rect(sh.x + 8, sh.y, 150, sh.height), "SKILL BREAKDOWN", _hint);
                GUI.Label(new Rect(sh.xMax - 158, sh.y, 150, sh.height), "DAMAGE  ·  %  ·  DPS", new GUIStyle(_hint) { alignment = TextAnchor.MiddleRight });

                var list = Tracker.Skills.OrderByDescending(k => k.Value.Total).Take(8).ToList();
                if (list.Count == 0)
                {
                    GUILayout.Label("Hit something to start tracking", _small);
                }
                double top = list.Count > 0 ? list[0].Value.Total : 1;
                float el = Math.Max(Tracker.Elapsed, 1f);
                foreach (var kv in list)
                {
                    var r = GUILayoutUtility.GetRect(W - 12, 20);
                    r.x += 6; r.width -= 12;
                    GUI.DrawTexture(r, _track);
                    float frac = (float)(kv.Value.Total / top);
                    GUI.DrawTexture(new Rect(r.x, r.y, r.width * frac, r.height), _bar);
                    double pct = 100.0 * kv.Value.Total / Math.Max(Tracker.Total, 1);
                    GUI.Label(new Rect(r.x + 4, r.y, r.width * 0.5f, r.height), kv.Key, _name);
                    GUI.Label(new Rect(r.x, r.y, r.width - 4, r.height),
                        $"{Fmt(kv.Value.Total)}  ·  {pct:0}%  ·  {Fmt(kv.Value.Total / el)}", _val);
                    GUILayout.Space(2);
                }
            }

            GUILayout.Space(4);
            GUILayout.Label($"{Plugin.ToggleKey.Value} hide   ·   {Plugin.ResetKey.Value} reset   ·   drag header to move", _hint);
        }

        void Card(string label, string value)
        {
            var r = GUILayoutUtility.GetRect(0, 44, GUILayout.ExpandWidth(true));
            r.x += 2; r.width -= 4;
            GUI.DrawTexture(r, _card);
            GUI.Label(new Rect(r.x, r.y + 2, r.width, 26), value, _big);
            GUI.Label(new Rect(r.x, r.y + 28, r.width, 14), label, _small);
        }
    }
}
