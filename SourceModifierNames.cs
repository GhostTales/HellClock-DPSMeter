using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using HarmonyLib;
using HellClock.Skill.Effect;
using HellClock.Stats;

namespace DPSMeter
{
    // Skill-effect stat modifiers carry no name of their own, so remember where each one came from
    internal static class SourceModifierNames
    {
        internal class Info
        {
            public object Origin;
            public string Effect, Trigger, Detail;
        }

        static readonly ConditionalWeakTable<object, Info> ByDef = new ConditionalWeakTable<object, Info>();
        static readonly Dictionary<string, Info> ByKey = new Dictionary<string, Info>();

        internal static Info GetOrCreate(object def) => ByDef.GetValue(def, _ => new Info());

        public static string Describe(StatModifierDefinition def) => ByDef.TryGetValue(def, out var i) ? Build(i) : null;

        public static string DescribeKey(string key) => ByKey.TryGetValue(key, out var i) ? Build(i) : null;

        internal static void SetKey(string key, Info i)
        {
            if (ByKey.Count > 5000) ByKey.Clear();
            ByKey[key] = i;
        }

        static string Build(Info i)
        {
            string origin = null;
            switch (i.Origin)
            {
                case null: break;
                case HellClock.SkillTree.SkillTreeNodeAffixDefinition a: origin = Breakdown.DescribeAffixOwner(a); break;
                case string str: origin = Breakdown.DescribeKey(str); break;
                case UnityEngine.Object o: origin = "[skill] " + o.name; break;
                default: origin = "[skill] " + i.Origin.GetType().Name; break;
            }
            if (origin == null) origin = "[skill] " + (i.Effect ?? "modifier");
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(i.Trigger)) parts.Add("on " + i.Trigger);
            if (!string.IsNullOrEmpty(i.Detail)) parts.Add(i.Detail);
            return parts.Count == 0 ? origin : origin + "  (" + string.Join(", ", parts) + ")";
        }

        internal static string Humanize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = Regex.Replace(s, "\\(Clone\\)", "").Replace('_', ' ');
            s = Regex.Replace(s, "(?<=[a-z])(?=[A-Z])", " ");
            return Regex.Replace(s, "\\s+", " ").Trim();
        }

        internal static void Record(SkillEffectData effect, ref SkillEffectTriggerInfo info, object key, StatModifierDefinition def)
        {
            var i = GetOrCreate(def);
            i.Effect = Humanize(effect?.name);

            var trig = effect?.EffectTrigger?.ToString() ?? effect?.CharacterEffectTrigger?.ToString();
            if (trig != null)
            {
                trig = Humanize(trig);
                if (trig.Contains("Generic"))
                {
                    var ev = Traverse.Create(effect).Field("_triggerTargetStringValue").GetValue<string>();
                    if (!string.IsNullOrEmpty(ev)) trig = ev;
                }
                i.Trigger = trig;
            }

            var skill = info.Skill;
            i.Origin = skill?.SourceKey;
            var behavior = info.SkillBehavior;
            if (skill != null && behavior != null)
            {
                foreach (var kv in skill.SkillBehaviors.Behaviors)
                    if (kv.Value == behavior) { i.Origin = kv.Key; break; }
            }
            if (key is string ks) SetKey(ks, i);
        }
    }

    [HarmonyPatch]
    internal static class RecordModifierPatch
    {
        static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(AddSkillStatModifierSkillEffectData), "AddModifier");
            yield return AccessTools.Method(typeof(AddCharacterStatModifierSkillEffectData), "AddModifier");
        }

        static void Postfix(SkillEffectData __instance, ref SkillEffectTriggerInfo skillEffectTriggerInfo, object key, StatModifierDefinition statModifierDefinition)
        {
            try
            {
                if (statModifierDefinition != null) SourceModifierNames.Record(__instance, ref skillEffectTriggerInfo, key, statModifierDefinition);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SkillEffectStatModifierDefinition), nameof(SkillEffectStatModifierDefinition.GetStatModifierDefinition))]
    internal static class ModifierValueSourcePatch
    {
        static void Postfix(SkillEffectStatModifierDefinition __instance, StatModifierDefinition __result)
        {
            try
            {
                if (__result == null) return;
                var t = Traverse.Create(__instance);
                string variable = t.Field("_value").Property("ValueOrName").GetValue<string>();
                var conv = t.Field("_useStatConversion").GetValue<EConversionStatModifierType>();
                var i = SourceModifierNames.GetOrCreate(__result);
                if (conv != EConversionStatModifierType.NoConversion)
                {
                    var stat = t.Field("_conversionStatDefinition").GetValue<EStatDefinition>();
                    i.Detail = "scales with " + SourceModifierNames.Humanize(stat.ToString());
                }
                else if (!string.IsNullOrEmpty(variable) && !float.TryParse(variable, out _))
                    i.Detail = "variable " + variable;
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e);
            }
        }
    }
}
