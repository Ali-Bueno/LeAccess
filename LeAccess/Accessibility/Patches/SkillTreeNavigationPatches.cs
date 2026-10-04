using HarmonyLib;
using LeAccess.Accessibility.Skills;
using UnityEngine;

namespace LeAccess.Accessibility.Patches;

// The native search takes the node nearest a point just ahead within a wide cone, so it zig-zags and can miss nodes;
// see reference/skill-tree-navigation.md. Neither prologue has a relative branch, so the originals may run.
[HarmonyPatch(typeof(SkillsPanelManager), nameof(SkillsPanelManager.ProcessLeftStickInput))]
internal static class SkillTreeMovePatch
{
    private static bool Prefix(SkillsPanelManager __instance, Vector3 input) =>
        !SkillTreeNavigation.TryNavigate(__instance, input);
}

// The native overview walk wraps around and mixes base-class and mastery skills in one row; see
// reference/skill-tree-navigation.md. The prologue's first relative branch is at +0x13, so the original may run.
[HarmonyPatch(typeof(SkillsPanelManager), nameof(SkillsPanelManager.HandleNavigateInput))]
internal static class SkillPanelMovePatch
{
    private static bool Prefix(SkillsPanelManager __instance, Direction direction) =>
        !SkillPanelGrid.TryNavigate(__instance, direction);
}

[HarmonyPatch(typeof(SkillsPanelManager), nameof(SkillsPanelManager.OnOpenSkillTree))]
internal static class SkillTreeOpenedPatch
{
    private static void Postfix(SkillsPanelManager __instance) => SkillTreeNavigation.AfterTreeOpened(__instance);
}
