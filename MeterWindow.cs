using System;
using System.Linq;
using UnityEngine;

namespace DPSMeter
{
    internal class MeterWindow : MonoBehaviour
    {
        Rect _rect = new Rect(20, 200, 260, 120);
        bool _visible = true;
        GUIStyle _label, _right;

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

        void OnGUI()
        {
            if (!_visible) return;
            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true };
                _right = new GUIStyle(_label) { alignment = TextAnchor.MiddleRight };
            }
            _rect.height = 0;
            _rect = GUILayout.Window(918273, _rect, Draw, "DPS Meter", GUILayout.Width(260));
        }

        void Draw(int id)
        {
            Row("DPS", Fmt(Tracker.Dps));
            Row("Total", Fmt(Tracker.Total));
            Row("Time", Tracker.Elapsed.ToString("0.0") + "s");
            Row("Highest hit", Fmt(Tracker.Highest));
            Row("Hits / Crit", Tracker.Hits + " / " + (Tracker.Hits > 0 ? (100.0 * Tracker.Crits / Tracker.Hits).ToString("0") : "0") + "%");

            if (Plugin.ShowBreakdown.Value && Tracker.Total > 0)
            {
                GUILayout.Space(4);
                foreach (var kv in Tracker.Skills.OrderByDescending(k => k.Value.Total).Take(8))
                {
                    double pct = 100.0 * kv.Value.Total / Tracker.Total;
                    Row(kv.Key, Fmt(kv.Value.Total) + " (" + pct.ToString("0") + "%)");
                }
            }
            GUILayout.Label($"<size=10>{Plugin.ToggleKey.Value}: hide  {Plugin.ResetKey.Value}: reset</size>", _label);
            GUI.DragWindow();
        }

        void Row(string l, string r)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(l, _label);
            GUILayout.Label(r, _right);
            GUILayout.EndHorizontal();
        }
    }
}
