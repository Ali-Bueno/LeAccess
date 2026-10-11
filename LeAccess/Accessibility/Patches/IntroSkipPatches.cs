using HarmonyLib;
using LeAccess.Accessibility.Navigation;

namespace LeAccess.Accessibility.Patches;

// The intro movie skips on "any key"; see reference/runtime-feedback.md. Postfix only: the original always runs.
[HarmonyPatch(typeof(ButtonInputListenerManager), nameof(ButtonInputListenerManager.IsAnyKeyDown))]
internal static class IntroSkipKeyPatch
{
    private static void Postfix(ref bool __result) => IntroSkipGuard.Review(ref __result);
}
