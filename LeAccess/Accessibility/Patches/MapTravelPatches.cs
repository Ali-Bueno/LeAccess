using HarmonyLib;
using LeAccess.Accessibility.Map;

namespace LeAccess.Accessibility.Patches;

// Cross on the world map presses the travel button, whose listener is the target waypoint's LoadWaypointScene.
[HarmonyPatch(typeof(MapPanel), nameof(MapPanel.HandleSubmitInput))]
internal static class MapSubmitPatch
{
    private static void Prefix() => MapTravelAnnouncer.BeginSubmit();
    private static void Postfix(MapPanel __instance, SubmitType type) => MapTravelAnnouncer.EndSubmit(__instance, type);
}

[HarmonyPatch(typeof(UIWaypointStandard), nameof(UIWaypointStandard.LoadWaypointScene))]
internal static class WaypointTravelPatch
{
    private static void Prefix(UIWaypointStandard __instance) => MapTravelAnnouncer.AnnounceTravel(__instance);
}

// Cross on the focused, completed epoch mastery panel acknowledges it and travels at once. The prologue has no
// relative branch, so the original may run.
[HarmonyPatch(typeof(EpochMasteryUI), nameof(EpochMasteryUI.HandleSubmitInput))]
internal static class EpochMasterySubmitPatch
{
    private static void Prefix(EpochMasteryUI __instance, SubmitType type) => EpochMasteryReader.AnnounceSubmit(__instance, type);
}
