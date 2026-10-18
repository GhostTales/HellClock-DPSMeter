using System;
using System.Collections.Generic;
using System.Linq;
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
            public Sprite Icon; // null when the source has no icon
            public readonly double[] ByType = new double[TypeCount];
        }

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

        static readonly HashSet<string> SeenHits = new HashSet<string>();

        public static void Reset()
        {
            Skills.Clear();
            SeenHits.Clear();
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

            var relic = RelicOf(d);
            string name = relic?.Name ?? SkillName(d);
            if (!Skills.TryGetValue(name, out var e)) Skills[name] = e = new Entry();
            if (e.Icon == null) e.Icon = relic?.Icon ?? SourceIcon(d);
            if (Plugin.LogUniqueHits.Value) LogUniqueHit(d, name, e.Icon, value);
            e.Total += value;
            e.Hits++;
            if (d.IsCritical) e.Crits++;
            if (value > e.Max) e.Max = value;
            if (d.IsDamageOverTime) DotTotal += value;

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

        // Name and icon of the skills that dealt damage of this type, biggest contributor first. Skills without an icon are skipped.
        public static List<KeyValuePair<string, Sprite>> IconsForType(int type, int max = int.MaxValue)
        {
            var icons = new List<KeyValuePair<string, Sprite>>();
            foreach (var kv in Skills.OrderByDescending(s => s.Value.ByType[type]))
            {
                if (kv.Value.ByType[type] <= 0) break;
                if (kv.Value.Icon != null) icons.Add(new KeyValuePair<string, Sprite>(kv.Key, kv.Value.Icon));
                if (icons.Count >= max) break;
            }
            return icons;
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

        // Icon of the skill that owns the damage (the summon's parent skill, not the basic attack it
        // uses), then the skill that dealt it, then the status icon for damage over time
        static Sprite SourceIcon(DamageInstance d)
        {
            try
            {
                // Damage over time is listed under its status (Bleeding, Ignited...), so it uses the status icon
                var dot = d.DamageSourceInfo?.DamageOverTimeID ?? -1;
                if (dot != -1)
                {
                    var statusIcon = HellClock.Constants.Collections.STATUS_DEFINITION_COLLECTION.Reference.GetElement(dot)?.StatusDisplayInfo.Icon;
                    if (statusIcon != null) return statusIcon;
                }
                return IconOf(d.SourceSkillID) ?? IconOf(d.SkillID);
            }
            catch { }
            return null;
        }

        // Relic (or affix) whose effect or effect area dealt this damage. Damage over time keeps its status.
        static RelicSources.Info RelicOf(DamageInstance d)
        {
            try
            {
                if (d.IsDamageOverTime) return null;
                return RelicSources.FromEffect(EffectContextPatch.Current)
                    ?? RelicSources.FromAreaName(d.DamageSourceInfo?.DamageSourceName);
            }
            catch { return null; }
        }

        static Sprite IconOf(int skillId) => skillId == -1 ? null : HellClock.Skill.SkillDefinition.GetSkillDefinition(skillId)?.Icon;

        static string SkillDefLabel(int skillId)
        {
            if (skillId == -1) return "none";
            try
            {
                var def = HellClock.Skill.SkillDefinition.GetSkillDefinition(skillId);
                if (def == null) return $"{skillId} (no definition)";
                return $"{skillId} '{def.name}' icon={(def.Icon != null ? def.Icon.name : "NONE")}";
            }
            catch (Exception ex)
            {
                return $"{skillId} (error: {ex.Message})";
            }
        }

        // Logs the first hit of every distinct source so missing or wrong icons can be traced
        static void LogUniqueHit(DamageInstance d, string name, Sprite icon, double value)
        {
            try
            {
                var info = d.DamageSourceInfo;
                string attacker = d.Attacker?.LogName ?? "null";
                string key = $"{name}|{d.SkillID}|{d.SourceSkillID}|{info?.DamageOverTimeID}|{attacker}";
                if (!SeenHits.Add(key)) return;
                Plugin.Logger.LogInfo(
                    $"[Hit] entry='{name}' value={value:0.#} crit={d.IsCritical} dot={d.IsDamageOverTime} area={info?.IsAreaDamage} melee={info?.IsMeleeDamage}\n" +
                    $"      attacker={attacker} ({d.Attacker?.GetType().Name}) target={d.Target?.GetType().Name}\n" +
                    $"      sourceName='{info?.DamageSourceName}' sourceKey='{info?.DamageSourceKey}' damageSourceId={info?.DamageSourceID} dotId={info?.DamageOverTimeID}\n" +
                    $"      skill={SkillDefLabel(d.SkillID)}\n" +
                    $"      sourceSkill={SkillDefLabel(d.SourceSkillID)}\n" +
                    $"      chosenIcon={(icon != null ? icon.name : "NONE")}");
                if (icon == null) Plugin.Logger.LogWarning($"[Hit] no icon found for '{name}', it will not appear in the icon row");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Hit] logging failed: {ex.Message}");
            }
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
