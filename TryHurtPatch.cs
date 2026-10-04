using System;
using HarmonyLib;
using HellClock.Character;
using HellClock.Damage;

namespace DPSMeter
{
    [HarmonyPatch(typeof(LifeController), nameof(LifeController.TryHurt))]
    internal static class TryHurtPatch
    {
        static void Postfix(DamageInstance damage)
        {
            try
            {
                if (damage == null || damage.Target == null || damage.Attacker == null) return;
                // The player's IsAlly is false, so filter on enemy-vs-non-enemy instead
                if (!(damage.Target is HellClock.Character.Enemy.EnemyController target) || target.IsAlly) return;
                if (damage.Attacker is HellClock.Character.Enemy.EnemyController attacker && !attacker.IsAlly) return;
                Tracker.Record(damage);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e);
            }
        }
    }
}
