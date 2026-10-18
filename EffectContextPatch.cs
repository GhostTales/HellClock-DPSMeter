using System;
using HarmonyLib;
using HellClock.Skill.Effect;

namespace DPSMeter
{
    // Tracks the skill effect being triggered so damage dealt by it can be attributed to the relic that owns it
    [HarmonyPatch(typeof(SkillEffectInstance), nameof(SkillEffectInstance.InternalTryToTriggerEffect))]
    internal static class EffectContextPatch
    {
        public static SkillEffectData Current;

        static void Prefix(SkillEffectData skillEffectData, out SkillEffectData __state)
        {
            __state = Current;
            Current = skillEffectData;
        }

        // Runs even when the effect throws, so the context never leaks into later hits
        static Exception Finalizer(Exception __exception, SkillEffectData __state)
        {
            Current = __state;
            return __exception;
        }
    }
}
