using HarmonyLib;
using LeAccess.Accessibility.CharacterCreation;
using LE.UI.PanelSystem;

namespace LeAccess.Accessibility.Patches;

[HarmonyPatch(typeof(MainMenuPanel), nameof(MainMenuPanel.OnOpen), typeof(PanelSystem), typeof(PanelLocation))]
internal static class PauseMenuOpenedPatch
{
    private static void Prefix(MainMenuPanel __instance, PanelSystem panelSystem) =>
        PauseMenuFocusRecovery.Capture(__instance, panelSystem);
}

[HarmonyPatch(typeof(PanelSystem), nameof(PanelSystem.ClosePanelInternal), typeof(Panel), typeof(bool))]
internal static class PauseMenuClosedPatch
{
    private static void Prefix(PanelSystem __instance, Panel panel, bool isPreloading,
        out PauseMenuFocusRecovery.Snapshot __state) =>
        __state = PauseMenuFocusRecovery.BeginClose(__instance, panel, isPreloading);

    private static void Postfix(PauseMenuFocusRecovery.Snapshot __state, bool __result) =>
        PauseMenuFocusRecovery.CompleteClose(__state, __result);
}
