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

[HarmonyPatch(typeof(SkillsPanelManager), nameof(SkillsPanelManager.OnOpenSkillTree))]
internal static class SkillTreeOpenedPatch
{
    private static void Postfix(SkillsPanelManager __instance) => SkillTreeNavigation.AfterTreeOpened(__instance);
}
