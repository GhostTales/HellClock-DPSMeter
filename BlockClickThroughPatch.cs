using System.Collections.Generic;
using HarmonyLib;
using UnityEngine.EventSystems;

namespace DPSMeter
{
    // The game ignores clicks when EventSystem says the pointer is over UI; IMGUI is invisible to it.
    [HarmonyPatch(typeof(EventSystem), nameof(EventSystem.IsPointerOverGameObject), new System.Type[0])]
    internal static class BlockClickThroughPatch
    {
        static void Postfix(ref bool __result)
        {
            if (!__result && MeterWindow.IsMouseOver) __result = true;
        }
    }

    [HarmonyPatch(typeof(EventSystem), nameof(EventSystem.IsPointerOverGameObject), new[] { typeof(int) })]
    internal static class BlockClickThroughPointerIdPatch
    {
        static void Postfix(ref bool __result)
        {
            if (!__result && MeterWindow.IsMouseOver) __result = true;
        }
    }

    // Input modules find the clicked uGUI element through RaycastAll; emptying it hides UI behind the meter
    [HarmonyPatch(typeof(EventSystem), nameof(EventSystem.RaycastAll))]
    internal static class BlockRaycastPatch
    {
        static void Postfix(List<RaycastResult> raycastResults)
        {
            if (MeterWindow.IsMouseOver) raycastResults.Clear();
        }
    }
}
