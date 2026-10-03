using HarmonyLib;
using LeAccess.Accessibility.Navigation.Guidance;

namespace LeAccess.Accessibility.Patches;

// Native initialization is inlined; Unity's actual update callback discovers the live indicator.
[HarmonyPatch(typeof(RageOfMorditasGauntletWarningPointerVisualController), nameof(RageOfMorditasGauntletWarningPointerVisualController.FixedUpdate))]
internal static class EncounterReturnPointerPatch
{
    private static void Postfix(RageOfMorditasGauntletWarningPointerVisualController __instance) =>
        EncounterGuidanceTargets.Track(__instance);
}
