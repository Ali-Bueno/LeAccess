using HarmonyLib;
using LeAccess.Accessibility.Navigation;

namespace LeAccess.Accessibility.Patches;

// The game leaves controller mode on any held or released key, clearing menu focus; see
// reference/runtime-feedback.md. The prologue's first relative branch is at +0x15, so the original may run.
[HarmonyPatch(typeof(EpochInputManager), nameof(EpochInputManager.ControllerShouldBeActive))]
internal static class ControllerModePatch
{
    private static void Postfix(EpochInputManager __instance, ref bool __result) =>
        ControllerModeGuard.Review(__instance, ref __result);
}
