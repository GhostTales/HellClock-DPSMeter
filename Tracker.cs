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
            public readonly double[] ByType = new double[TypeCount];
            public readonly Dictionary<string, UniqueHit> Uniques = new Dictionary<string, UniqueHit>();
        }

        // One distinct damage number a skill has dealt, with the formula snapshot of its first occurrence
        internal class UniqueHit
        {
            public double Value;
            public bool Crit;
            public int Count;
            public List<string> Chain;
        }

        const int MaxUniques = 300;

        public const int TypeCount = 4;
        public static readonly string[] TypeNames = { "Physical", "Fire", "Plague", "Lightning" };
        public static readonly Color[] TypeColors =
        {
            new Color(0xF6 / 255f, 0x3D / 255f, 0x2C / 255f), // Physical
            new Color(0xF6 / 255f, 0x73 / 255f, 0x2C / 255f), // Fire
            new Color(0x00 / 255f, 0xD8 / 255f, 0x3A / 255f), // Plague
            new Color(0x2C / 255f, 0x94 / 255f, 0xF6 / 255f) // Lightning
        };
        public static readonly double[] TypeTotals = new double[TypeCount];
        public static readonly int[] TypeHits = new int[TypeCount];
        public static double DotTotal;
        public static double PeakDps;
        const float PeakMinSeconds = 3f; // avoids inflated values from the first hit

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
            Array.Clear(TypeTotals, 0, TypeCount);
            Array.Clear(TypeHits, 0, TypeCount);
            DotTotal = 0;
            PeakDps = 0;
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
            if (Elapsed >= PeakMinSeconds) PeakDps = Math.Max(PeakDps, Dps);
            if (value > Highest) Highest = value;

            string name = SkillName(d);
            if (!Skills.TryGetValue(name, out var e)) Skills[name] = e = new Entry();
            e.Total += value;
            e.Hits++;
            if (d.IsCritical) e.Crits++;
            if (value > e.Max) e.Max = value;
            if (d.IsDamageOverTime) DotTotal += value;

            string key = Math.Round(d.Value).ToString("0") + (d.IsCritical ? "c" : "");
            if (e.Uniques.TryGetValue(key, out var u)) u.Count++;
            else if (e.Uniques.Count < MaxUniques)
                e.Uniques[key] = new UniqueHit { Value = d.Value, Crit = d.IsCritical, Count = 1, Chain = Breakdown.Build(d) };

            var split = new double[TypeCount];
            SplitByType(d, value, split);
            for (int i = 0; i < TypeCount; i++)
            {
                if (split[i] <= 0) continue;
                TypeTotals[i] += split[i];
                TypeHits[i]++;
                e.ByType[i] += split[i];
            }
        }

        static void SplitByType(DamageInstance d, double value, double[] split)
        {
            double sum = 0;
            try
            {
                foreach (var b in d.DamageBases)
                {
                    int t = (int)b.EDamageTypeDefinition;
                    if (b.Ignored || t < 0 || t >= TypeCount || b.Value <= 0) continue;
                    split[t] += b.Value;
                    sum += b.Value;
                }
            }
            catch { sum = 0; }

            if (sum <= 0)
            {
                Array.Clear(split, 0, TypeCount);
                for (int i = 0; i < TypeCount; i++)
                    if ((d.DamageTypesBitMask & (1 << i)) != 0) { split[i] = value; return; }
                split[0] = value;
                return;
            }
            double scale = value / sum; // keeps overkill capping consistent
            for (int i = 0; i < TypeCount; i++) split[i] *= scale;
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
