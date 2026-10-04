using System;
using System.Collections.Generic;
using System.Text;
using HellClock.Damage;
using HellClock.Stats;
using HellClock.Status;

namespace DPSMeter
{
    // Snapshots the full damage formula (stat bases, every active modifier, skill modifiers, target mitigation) for one hit
    internal static class Breakdown
    {
        static readonly string[] TypeNames = { "Physical", "Fire", "Plague", "Lightning" };
        static readonly EDamageTypeDefinition[] Types =
        {
            EDamageTypeDefinition.PHYSICAL, EDamageTypeDefinition.FIRE,
            EDamageTypeDefinition.PLAGUE, EDamageTypeDefinition.LIGHTNING
        };

        const string Cond = "#ffd27a", Skill = "#9ad0ff", Dim = "#9a9aa5", Good = "#8fe38f", Bad = "#ff8f8f";

        public static List<string> Build(DamageInstance d)
        {
            var lines = new List<string>();
            try
            {
                var info = d.DamageSourceInfo;
                var atk = d.Attacker?.Sheet;
                var tgt = d.Target?.Sheet;
                if (info == null || atk == null) return lines;
                var src = info.SourceStatModifiers;
                var dist = info.DamageTypeDistribution;

                lines.Add($"<b>Source</b> damage multiplier x{info.BaseDamageModifier:0.###}"
                    + (info.BaseDamageOverride.HasValue ? $", base override {info.BaseDamageOverride.Value:0.##}" : "")
                    + (info.IsDamageOverTime ? ", damage over time" : "")
                    + (info.IsAreaDamage ? ", area" : "")
                    + (d.IsCritical ? ", <color=#ffd27a>CRITICAL</color>" : ""));
                if (src != null && src.Count > 0)
                    lines.Add($"<color={Dim}>{src.Count} skill-specific stat modifiers apply (shown as [skill])</color>");

                for (int i = 0; i < Types.Length; i++)
                {
                    var t = Types[i];
                    if (dist == null || !dist.HasDamageOfType(t)) continue;
                    var def = DamageDealer.DamageTypes[t].DamageTypeDefinition;
                    float share = i == 0 ? dist.PhysicalDamageDistribution : i == 1 ? dist.FireDamageDistribution
                        : i == 2 ? dist.PlagueDamageDistribution : dist.LightningDamageDistribution;
                    double add = i == 0 ? dist.AdditionalPhysicalDamage : i == 1 ? dist.AdditionalFireDamage
                        : i == 2 ? dist.AdditionalPlagueDamage : dist.AdditionalLightningDamage;

                    lines.Add("");
                    lines.Add($"<b>{TypeNames[i]}</b>  distribution {share * 100:0.#}%" + (add != 0 ? $", flat +{add:0.##}" : ""));
                    Chain(lines, atk.GetStat(EStatDefinition.BaseDamage), src);
                    Chain(lines, atk.GetStat(def.EAdditionalDamageStat), src);
                    foreach (var s in def.ModifierEStats) Chain(lines, atk.GetStat(s), src);

                    DamageInstanceBase b = null;
                    foreach (var x in d.DamageBases) if (x.EDamageTypeDefinition == t) b = x;
                    if (b != null)
                    {
                        if (d.IsCritical)
                        {
                            Chain(lines, atk.GetStat(EStatDefinition.CriticalDamage), src);
                            lines.Add($"  before crit {b.NonCriticalValue:0.##}  ->  after crit {b.Value:0.##}");
                        }
                        if (tgt != null)
                        {
                            lines.Add($"<color={Dim}>Target mitigation</color>");
                            Chain(lines, tgt.GetStat(def.EDefenseStat), null, true);
                            Chain(lines, tgt.GetStat(EStatDefinition.DamageResistance), null, true);
                            if (info.IsDamageOverTime)
                                Chain(lines, tgt.GetStat(EStatDefinition.DamageOverTimeResistance), null, true);
                        }
                        lines.Add(b.Ignored
                            ? $"  <color={Bad}>Ignored ({b.EDamageIgnoredSource})</color>"
                            : $"  <b>Final {TypeNames[i]}: {b.Value:0.##}</b>");
                    }
                }
                lines.Add("");
                lines.Add($"<b>Total hit: {d.Value:0.##}</b>");
            }
            catch (Exception e)
            {
                lines.Add("Breakdown failed: " + e.Message);
            }
            return lines;
        }

        static void Chain(List<string> lines, Stat stat, List<StatModifierDefinition> src, bool target = false)
        {
            if (stat == null) return;
            string name = ((EStatDefinition)stat.StatDefinition.HashID).ToString();
            var sb = new List<string>();

            foreach (var key in stat.GetKeys())
            {
                if (!stat.TryGetModifier(key, out var m)) continue;
                string label = Describe(key, out bool conditional);
                sb.Add(Row(label, m.ModifierType, m.Value, conditional ? Cond : null));
            }
            if (src != null)
            {
                foreach (var m in src)
                {
                    if (m.EStatDefinition != (EStatDefinition)stat.StatDefinition.HashID) continue;
                    sb.Add(Row("[skill] modifier", m.ModifierType, m.Value, Skill));
                }
            }

            double final = stat.ValueWithModifier(src);
            if (sb.Count == 0 && (final == 0 || final == 1)) return;

            lines.Add($"  {name}: base {stat.BaseValue:0.###}  =  <b>{final:0.###}</b>");
            foreach (var s in sb) lines.Add(s);
        }

        static string Row(string label, EModifierType type, double value, string color)
        {
            string op;
            switch (type)
            {
                case EModifierType.Additive: op = value >= 0 ? "+" + value.ToString("0.###") : value.ToString("0.###"); break;
                case EModifierType.Multiplicative: op = "x" + value.ToString("0.###"); break;
                case EModifierType.MultiplicativeAdditive: op = "x(1+" + (value - 1).ToString("0.###") + ") add-mult"; break;
                case EModifierType.BaseOverride: op = "base = " + value.ToString("0.###"); break;
                default: op = "set " + value.ToString("0.###"); break;
            }
            string l = color != null ? $"<color={color}>{label}</color>" : label;
            return $"      {op}   {l}";
        }

        // Status effects are the "conditionals": they only exist while their condition holds
        static string Describe(object key, out bool conditional)
        {
            conditional = false;
            switch (key)
            {
                case null: return "?";
                case string s: return s;
                case StatusEffect se:
                    conditional = true;
                    return "[status] " + (se.StatusDefinition != null ? se.StatusDefinition.name : "?") + (se.StackAmount > 1 ? " x" + se.StackAmount : "");
                case UnityEngine.Object o: return o.name;
                case Modifier _: return "(linked stat)";
                case HellClock.Skill.Skill sk: return "[skill] " + sk.SourceKey;
            }
            string str = key.ToString();
            var type = key.GetType();
            if (str == type.ToString()) str = type.Name;
            return str;
        }
    }
}
