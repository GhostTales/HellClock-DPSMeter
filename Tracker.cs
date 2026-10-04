using System;
using System.Collections.Generic;
using HellClock.Damage;
using UnityEngine;

namespace DPSMeter
{
    internal static class Tracker
    {
        internal class Entry
        {
            public double Total;
            public int Hits;
            public int Crits;
            public double Max;
        }

        public static readonly Dictionary<string, Entry> Skills = new Dictionary<string, Entry>();
        public static double Total;
        public static double Highest;
        public static int Hits;
        public static int Crits;
        public static float Start = -1f;
        public static float Last;

        public static void Reset()
        {
            Skills.Clear();
            Total = Highest = 0;
            Hits = Crits = 0;
            Start = -1f;
        }

        public static float Elapsed
        {
            get
            {
                if (Start < 0f) return 0f;
                float end = Time.time - Last > Plugin.IdleTimeout.Value ? Last : Time.time;
                return Mathf.Max(end - Start, 0.01f);
            }
        }

        public static double Dps => Start < 0f ? 0 : Total / Math.Max(Elapsed, 1f);

        public static void Record(DamageInstance d)
        {
            double value = d.Value;
            if (Plugin.CapOverkill.Value && d.TargetCurrentLife > 0.0)
                value = Math.Min(value, d.TargetCurrentLife + d.TargetCurrentBarrier);
            if (value <= 0.0) return;

            float now = Time.time;
            if (Start < 0f || now - Last > Plugin.IdleTimeout.Value)
            {
                if (Start >= 0f) Reset();
                Start = now;
            }
            Last = now;

            Total += value;
            Hits++;
            if (d.IsCritical) Crits++;
            if (value > Highest) Highest = value;

            string name = SkillName(d);
            if (!Skills.TryGetValue(name, out var e)) Skills[name] = e = new Entry();
            e.Total += value;
            e.Hits++;
            if (d.IsCritical) e.Crits++;
            if (value > e.Max) e.Max = value;
        }

        static string SkillName(DamageInstance d)
        {
            try
            {
                var info = d.DamageSourceInfo;
                string n = info?.DamageSourceName;
                if (string.IsNullOrEmpty(n) && d.SkillID != -1)
                    n = HellClock.Skill.SkillDefinition.GetSkillDefinition(d.SkillID)?.name;
                if (string.IsNullOrEmpty(n)) n = "Unknown";
                return d.IsDamageOverTime ? n + " (DoT)" : n;
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
