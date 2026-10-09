using HarmonyLib;
using LeAccess.Accessibility.Map;

namespace LeAccess.Accessibility.Patches;

// D-pad up/down change the quest even while the quests are hidden; then they walk the destinations instead
// (reference/waypoint-map.md). The prologue has no relative branch, so the original may run.
[HarmonyPatch(typeof(MapPanel), nameof(MapPanel.HandleNavigateInput))]
internal static class MapNavigatePatch
{
    private static bool Prefix(MapPanel __instance, Direction direction) =>
        !MapLocationList.TryNavigate(__instance, direction);
}
