using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using HarmonyLib;
using HellClock.Skill.Effect;
using HellClock.Stats;

namespace DPSMeter
{
    // Skill-source modifiers carry no name of their own, so remember which skill effect added each one
    internal static class SourceModifierNames
    {
        static readonly ConditionalWeakTable<StatModifierDefinition, string> Names = new ConditionalWeakTable<StatModifierDefinition, string>();

        public static string Get(StatModifierDefinition def) => Names.TryGetValue(def, out var n) ? n : null;

        internal static void Set(StatModifierDefinition def, string name)
        {
            Names.Remove(def);
            Names.Add(def, name);
        }

        internal static string Humanize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = Regex.Replace(s, "\\(Clone\\)|SkillEffect|Effect", "").Replace('_', ' ').Trim();
            return Regex.Replace(s, "(?<=[a-z])(?=[A-Z])", " ");
        }
    }

    [HarmonyPatch(typeof(SkillEffectTriggerInfo), nameof(SkillEffectTriggerInfo.AddStatModifier))]
    internal static class AddStatModifierPatch
    {
        static void Postfix(ref SkillEffectTriggerInfo __instance, StatModifierDefinition statModifierDefinition)
        {
            try
            {
                if (statModifierDefinition == null) return;
                string skill = __instance.Skill?.SourceKey;
                string effect = SourceModifierNames.Humanize(__instance.SkillEffectData?.name);
                string name = string.IsNullOrEmpty(effect) ? skill : string.IsNullOrEmpty(skill) ? effect : skill + ": " + effect;
                if (!string.IsNullOrEmpty(name)) SourceModifierNames.Set(statModifierDefinition, name);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e);
            }
        }
    }
}
