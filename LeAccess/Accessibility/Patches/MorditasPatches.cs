using System;
using HarmonyLib;
using LeAccess.Accessibility.Morditas;
using UnityEngine;

namespace LeAccess.Accessibility.Patches;

// Read-only observers of the Rage of Morditas visuals, which run for the host and online clients alike.
// Targets and their native bodies are checked in reference/rage-of-morditas.md#hooks.
internal static class MorditasPatches
{
    [HarmonyPatch(typeof(RageOfMorditasHuntVisualController), nameof(RageOfMorditasHuntVisualController.OnStateUpdate))]
    internal static class HuntState
    {
        private static void Postfix(RageOfMorditasHuntVisualController __instance) => MorditasProbe.Hunt(__instance);
    }

    [HarmonyPatch(typeof(RageOfMorditasGauntletVisualController), nameof(RageOfMorditasGauntletVisualController.OnStateUpdate))]
    internal static class GauntletState
    {
        private static void Postfix(RageOfMorditasGauntletVisualController __instance) =>
            MorditasProbe.Circle(__instance, null, null);
    }

    [HarmonyPatch(typeof(RageOfMorditasGauntletVisualController), nameof(RageOfMorditasGauntletVisualController.OnInGauntletStatusChanged))]
    internal static class InGauntlet
    {
        private static void Postfix(RageOfMorditasGauntletVisualController __instance, bool status) =>
            MorditasProbe.Circle(__instance, status, null);
    }

    [HarmonyPatch(typeof(RageOfMorditasGauntletVisualController), nameof(RageOfMorditasGauntletVisualController.OnAllPlayersOutOfGauntletChanged))]
    internal static class AllOut
    {
        private static void Postfix(RageOfMorditasGauntletVisualController __instance, bool allPlayersOut) =>
            MorditasProbe.Circle(__instance, null, allPlayersOut);
    }

    [HarmonyPatch(typeof(RageOfMorditasHuntVisualController), nameof(RageOfMorditasHuntVisualController.HandleBloodTrails))]
    internal static class BloodTrailsDrawn
    {
        private static void Postfix(RageOfMorditasHuntVisualController __instance,
            Il2CppSystem.Collections.Generic.IReadOnlyList<Vector3> targetPositions) =>
            MorditasProbe.TrailsDrawn(__instance, targetPositions);

        // The mod's only interface-typed hook argument: if its detour cannot be built, the rest still loads.
        private static Exception Cleanup(Exception ex)
        {
            if (ex != null) Plugin.Log.LogWarning($"[Morditas] blood trail hook unavailable: {ex.Message}");
            return null;
        }
    }
}
