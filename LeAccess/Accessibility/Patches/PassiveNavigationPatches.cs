using System;
using HarmonyLib;
using LeAccess.Accessibility.Passives;
using UnityEngine;

namespace LeAccess.Accessibility.Patches;

// The native moves keep searching the same row or column, skipping nearer nodes; see reference/passive-reading.md.
// Both prefixes always skip the original: calling HandleNavigateInput through its detour crashes the game.
[HarmonyPatch(typeof(PassiveTreeNavigable), nameof(PassiveTreeNavigable.HandleNavigateInput))]
internal static class PassiveNavigatePatch
{
    private static bool Prefix(PassiveTreeNavigable __instance, Direction direction)
    {
        PassiveTreeNavigation.Navigate(__instance, direction);
        return false;
    }
}

[HarmonyPatch(typeof(PassiveTreeNavigable), nameof(PassiveTreeNavigable.HandleLeftStickInput))]
internal static class PassiveStickPatch
{
    private static bool Prefix(PassiveTreeNavigable __instance, Vector3 input)
    {
        PassiveTreeNavigation.Navigate(__instance, input);
        return false;
    }
}

[HarmonyPatch(typeof(PassiveTreeNavigable), nameof(PassiveTreeNavigable.OnSelected), new Type[0])]
internal static class PassiveSelectedPatch
{
    private static void Postfix(PassiveTreeNavigable __instance) => PassiveTreeNavigation.AfterSelected(__instance);
}

[HarmonyPatch(typeof(PassiveTreeNavigable), nameof(PassiveTreeNavigable.OnSelected), typeof(Vector2Int))]
internal static class PassiveSelectedAtPatch
{
    private static void Postfix(PassiveTreeNavigable __instance) => PassiveTreeNavigation.AfterSelected(__instance);
}
