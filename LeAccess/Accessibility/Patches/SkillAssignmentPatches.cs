using HarmonyLib;
using LeAccess.Accessibility.Skills;

namespace LeAccess.Accessibility.Patches;

// Square here is the game's "swap action bar skills": it closes the panel without keeping the highlighted skill.
// Cross on a tree node does nothing for an unspecialised skill or without points; the mod says why.
[HarmonyPatch(typeof(SkillsPanelManager), nameof(SkillsPanelManager.HandleSubmitInput))]
internal static class SkillSwapPatch
{
    private static void Prefix(SkillsPanelManager __instance, SubmitType type)
    {
        SkillSwapCarry.BeforeSubmit(type);
        SkillTreeState.BeforeSubmit(__instance, type);
    }
}
