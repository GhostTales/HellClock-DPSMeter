using System;
using System.Collections.Generic;
using System.Linq;
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
                _owners = BuildOwners(atk, tgt);
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

        // Modifier -> (stat that owns it, key it was registered under); lets linked/proxied modifiers be traced to their origin
        static Dictionary<Modifier, KeyValuePair<Stat, object>> _owners = new Dictionary<Modifier, KeyValuePair<Stat, object>>();

        static Dictionary<Modifier, KeyValuePair<Stat, object>> BuildOwners(params CharacterSheet[] sheets)
        {
            var map = new Dictionary<Modifier, KeyValuePair<Stat, object>>();
            foreach (var sheet in sheets)
            {
                if (sheet == null) continue;
                foreach (EStatDefinition e in Enum.GetValues(typeof(EStatDefinition)))
                {
                    var st = sheet.GetStat(e);
                    if (st == null) continue;
                    foreach (var key in st.GetKeys())
                        if (!(key is Modifier) && st.TryGetModifier(key, out var m) && !map.ContainsKey(m))
                            map[m] = new KeyValuePair<Stat, object>(st, key);
                }
            }
            return map;
        }

        static readonly System.Text.RegularExpressions.Regex RelicKey =
            new System.Text.RegularExpressions.Regex(@"^Relic\d{5}Affix\d{3}$");
        static readonly System.Text.RegularExpressions.Regex Tags = new System.Text.RegularExpressions.Regex("<[^>]*>");

        static readonly System.Text.RegularExpressions.Regex NumberTokens =
            new System.Text.RegularExpressions.Regex(@"[+\-]?\d+([.,]\d+)?%?");

        internal static string Loc(string key)
        {
            try { return Tags.Replace(RosettaStone.Interpret(key), "").Trim(); }
            catch { return key; }
        }

        internal static string DescribeKey(string key)
        {
            if (!RelicKey.IsMatch(key)) return key;
            try
            {
                var items = HellClock.Player.PlayerController.Instance.PlayerRelicInventoryManager.AllItems;
                foreach (var it in items)
                {
                    if (!(it is HellClock.RelicInventory.RelicInventoryItem relic)) continue;
                    foreach (var fx in relic.AllAffixEffects)
                        if (key.Equals(fx.RelicAffixKey as string))
                            return "[relic] " + NumberTokens.Replace(Loc(fx.GetLocalizedDescription((HellClock.RelicInventory.Affixes.EAffixDescriptionOption)0)), "#");
                }
            }
            catch { }
            return key;
        }

        static string StatName(Stat st)
        {
            var id = (EStatDefinition)st.StatDefinition.HashID;
            return System.Text.RegularExpressions.Regex.Replace(id.ToString(), "(?<=[a-z])(?=[A-Z])", " ");
        }

        static void Chain(List<string> lines, Stat stat, List<StatModifierDefinition> src, bool target = false)
        {
            if (stat == null) return;
            string name = ((EStatDefinition)stat.StatDefinition.HashID).ToString();
            var items = new List<(string Label, EModifierType Type, double Value, string Color)>();

            foreach (var key in stat.GetKeys())
            {
                if (!stat.TryGetModifier(key, out var m)) continue;
                string label = Describe(key, m, out bool conditional, 0);
                items.Add((label, m.ModifierType, m.Value, conditional ? Cond : null));
            }
            if (src != null)
            {
                foreach (var m in src)
                {
                    if (m.EStatDefinition != (EStatDefinition)stat.StatDefinition.HashID) continue;
                    items.Add((SourceModifierNames.Describe(m) ?? "[skill] modifier", m.ModifierType, m.Value, Skill));
                }
            }

            // Modifiers that change nothing (+0, 0%) are noise
            items.RemoveAll(i => i.Type == EModifierType.Additive ? Math.Abs(i.Value) < 1e-9
                : (i.Type == EModifierType.Multiplicative || i.Type == EModifierType.MultiplicativeAdditive) && Math.Abs(i.Value - 1) < 1e-9);

            double final = stat.ValueWithModifier(src);
            if (items.Count == 0 && (final == 0 || final == 1)) return;

            lines.Add($"  {name}: base {stat.BaseValue:0.###}  =  <b>{final:0.###}</b>");

            // Same kind of modifier (ignoring roll values) collapses into one entry with a list of its values
            foreach (var g in items.GroupBy(i => (i.Type, i.Label)).OrderBy(g => g.Key.Type == EModifierType.BaseOverride ? 0 : g.Key.Type == EModifierType.Additive ? 1 : g.Key.Type == EModifierType.MultiplicativeAdditive ? 2 : 3))
            {
                var list = g.ToList();
                if (list.Count == 1)
                {
                    lines.Add(Row(list[0].Label, list[0].Type, list[0].Value, list[0].Color));
                    continue;
                }
                string c = list[0].Color;
                string total = list[0].Type == EModifierType.Additive ? "  (total +" + list.Sum(i => i.Value).ToString("0.###") + ")"
                    : list[0].Type == EModifierType.MultiplicativeAdditive ? "  (total " + (list.Sum(i => i.Value - 1) * 100).ToString("0.##") + "% [+])" : "";
                lines.Add($"      {list.Count}x " + (c != null ? $"<color={c}>{list[0].Label}</color>" : list[0].Label) + total);
                foreach (var v in list.OrderByDescending(i => i.Value))
                    lines.Add("            " + OpText(v.Type, v.Value));
            }
        }

        static string OpText(EModifierType type, double value)
        {
            switch (type)
            {
                case EModifierType.Additive: return value >= 0 ? "+" + value.ToString("0.###") : value.ToString("0.###");
                case EModifierType.Multiplicative: return ((value - 1) * 100).ToString("0.##") + "% [x]";
                case EModifierType.MultiplicativeAdditive: return ((value - 1) * 100).ToString("0.##") + "% [+]";
                case EModifierType.BaseOverride: return "override: " + value.ToString("0.###");
                default: return "set " + value.ToString("0.###");
            }
        }

        static string Row(string label, EModifierType type, double value, string color)
        {
            string l = color != null ? $"<color={color}>{label}</color>" : label;
            return $"      {OpText(type, value)}   {l}";
        }

        // Status effects are the "conditionals": they only exist while their condition holds
        static Dictionary<HellClock.SkillTree.SkillTreeNodeDefinition, HellClock.SkillTree.SkillTreeDefinition> _nodeTrees;

        static string DescribeNode(HellClock.SkillTree.SkillTreeNode node) => DescribeNodeDef(node.SkillTreeNodeDefinition);

        static void EnsureTrees()
        {
            if (_nodeTrees != null) return;
            _nodeTrees = new Dictionary<HellClock.SkillTree.SkillTreeNodeDefinition, HellClock.SkillTree.SkillTreeDefinition>();
            foreach (var t in UnityEngine.Resources.FindObjectsOfTypeAll<HellClock.SkillTree.SkillTreeDefinition>())
                foreach (var n in t.Nodes)
                    if (n != null) _nodeTrees[n] = t;
        }

        internal static string DescribeAffixOwner(object affix)
        {
            try
            {
                EnsureTrees();
                foreach (var def in _nodeTrees.Keys)
                    if (def.Affixes.Contains(affix as HellClock.SkillTree.SkillTreeNodeAffixDefinition)) return DescribeNodeDef(def);
            }
            catch { }
            return null;
        }

        internal static string DescribeNodeDef(HellClock.SkillTree.SkillTreeNodeDefinition def)
        {
            string name = null;
            try { name = def?.PrettyName; } catch { }
            name = string.IsNullOrEmpty(name) ? def?.name : Tags.Replace(name, "").Trim();

            string kind = "skill tree", tree = null;
            try
            {
                EnsureTrees();
                if (def != null && _nodeTrees.TryGetValue(def, out var t2))
                {
                    if (t2 is HellClock.SkillTree.ConstellationSkillTreeDefinition c)
                    {
                        kind = "constellation";
                        tree = string.IsNullOrEmpty(c.NameKey) ? null : Loc(c.NameKey);
                    }
                    else if (t2 is HellClock.SkillTree.GreatBellSkillTreeDefinition) kind = "great bell";
                    else if (t2 is HellClock.Penance.PenanceSkillTreeDefinition) kind = "penance";
                    else if (t2 is HellClock.Level.Nightmares.NightmareSkillTreeDefinition) kind = "nightmare";
                }
            }
            catch { }
            return $"[{kind}] " + (tree != null ? tree + ": " : "") + name;
        }

        static string Describe(object key, Modifier mod, out bool conditional, int depth)
        {
            conditional = false;
            switch (key)
            {
                case null: return "?";
                case string s: return SourceModifierNames.DescribeKey(s) ?? DescribeKey(s);
                case Stat st: return "stat " + StatName(st);
                case StatModifierDefinition smd: return "enemy modifier on " + smd.EStatDefinition;
                case Modifier lm:
                    if (depth < 4 && _owners.TryGetValue(lm, out var own))
                    {
                        return Describe(own.Value, lm, out conditional, depth + 1);
                    }
                    return "linked from another stat";
                case HellClock.SkillTree.SkillTreeNode node:
                    return DescribeNode(node);
                case StatusEffect se:
                    conditional = true;
                    return "[status] " + (se.StatusDefinition != null ? se.StatusDefinition.name : "?") + (se.StackAmount > 1 ? " x" + se.StackAmount : "");
                case UnityEngine.Object o: return o.name;
                case HellClock.Skill.Skill sk: return "[skill] " + sk.SourceKey;
            }
            string str = key.ToString();
            var type = key.GetType();
            if (str == type.ToString()) return type.Name;
            return type.Name + " " + str;
        }
    }
}
