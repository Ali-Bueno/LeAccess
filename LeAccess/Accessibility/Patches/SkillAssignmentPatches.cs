using HarmonyLib;
using LeAccess.Accessibility.Skills;

namespace LeAccess.Accessibility.Patches;

// Square here is the game's "swap action bar skills": it closes the panel without keeping the highlighted skill.
[HarmonyPatch(typeof(SkillsPanelManager), nameof(SkillsPanelManager.HandleSubmitInput))]
internal static class SkillSwapPatch
{
    private static void Prefix(SubmitType type) => SkillSwapCarry.BeforeSubmit(type);
}
