using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LeAccess.Accessibility.ScreenContent;
using LE.Factions;
using LE.UI.Generic.UnityUI;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Patches;

[HarmonyPatch]
internal static class TermsContentShownPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(LE.Tos.TosPanel), nameof(LE.Tos.TosPanel.OnOpen),
            new[] { typeof(LE.UI.PanelSystem.PanelSystem), typeof(LE.UI.PanelSystem.PanelLocation) });
        yield return AccessTools.Method(typeof(LE.Tos.TosPanel), nameof(LE.Tos.TosPanel.UpdateTosText), Type.EmptyTypes);
    }
    private static void Postfix(LE.Tos.TosPanel __instance) => ScreenContentAnnouncer.TrackTerms(__instance);
}

[HarmonyPatch(typeof(LE.Tos.TosPanel), nameof(LE.Tos.TosPanel.OnClose))]
internal static class TermsContentClosedPatch
{
    private static void Prefix(LE.Tos.TosPanel __instance)
    {
        if (IsAlive(__instance)) ScreenContentAnnouncer.Remove(__instance.gameObject);
    }
}

[HarmonyPatch]
internal static class TutorialTextShownPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(TutorialManager), nameof(TutorialManager.ActivateTooltip),
            new[] { typeof(string), typeof(RectTransform) });
        yield return AccessTools.Method(typeof(TutorialManager), nameof(TutorialManager.UpdateTooltipText), Type.EmptyTypes);
    }
    private static void Postfix(TutorialManager __instance) => ScreenContentAnnouncer.TrackTutorial(__instance);
}

[HarmonyPatch]
internal static class TutorialTextClosedPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(TutorialManager), nameof(TutorialManager.CloseTutorialTooltip), Type.EmptyTypes);
        yield return AccessTools.Method(typeof(TutorialManager), nameof(TutorialManager.OnDisable), Type.EmptyTypes);
    }
    private static void Prefix(TutorialManager __instance) => ScreenContentAnnouncer.RemoveTutorial(__instance);
}

[HarmonyPatch(typeof(IdolTutorialTooltipManager), nameof(IdolTutorialTooltipManager.Awake))]
internal static class IdolTutorialCreatedPatch
{
    private static void Postfix(IdolTutorialTooltipManager __instance)
    {
        try { if (IsAlive(__instance)) ScreenContentAnnouncer.Watch(__instance.tooltip); }
        catch (Exception) { }
    }
}

[HarmonyPatch(typeof(IdolTutorialTooltipManager), nameof(IdolTutorialTooltipManager.closeTooltip))]
internal static class IdolTutorialClosedPatch
{
    private static void Postfix(IdolTutorialTooltipManager __instance)
    {
        try { if (IsAlive(__instance)) ScreenContentAnnouncer.Remove(__instance.tooltip); }
        catch (Exception) { }
    }
}

[HarmonyPatch]
internal static class PopupContentShownPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(BasePopupNavigable), nameof(BasePopupNavigable.OnSelected), Type.EmptyTypes);
        // The indexed overload shares a native no-op with constructors; detouring it crashes startup.
        yield return AccessTools.Method(typeof(ConfirmationPanelNavigable), nameof(ConfirmationPanelNavigable.OnEnable), Type.EmptyTypes);
        yield return AccessTools.Method(typeof(ConfirmationPanelNavigable), nameof(ConfirmationPanelNavigable.OnSelected), Type.EmptyTypes);
        yield return AccessTools.Method(typeof(BinaryChoiceModalWindow), nameof(BinaryChoiceModalWindow.OnEnable), Type.EmptyTypes);
        yield return AccessTools.Method(typeof(BinaryChoiceModalWindow), nameof(BinaryChoiceModalWindow.OnAppeared), Type.EmptyTypes);
        yield return AccessTools.Method(typeof(AddLootFilterModal), nameof(AddLootFilterModal.Init), Type.EmptyTypes);
    }
    private static void Postfix(Component __instance) => ScreenContentAnnouncer.TrackPopup(__instance);
}

[HarmonyPatch]
internal static class PopupContentClosedPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(BinaryChoiceModalWindow), nameof(BinaryChoiceModalWindow.OnDisable), Type.EmptyTypes);
        yield return AccessTools.Method(typeof(AddLootFilterModal), nameof(AddLootFilterModal.OnClose), Type.EmptyTypes);
    }
    private static void Prefix(Component __instance)
    {
        ScreenContentAnnouncer.RemovePopup(__instance);
    }
}

[HarmonyPatch(typeof(MonolithEmpoweredPopup), nameof(MonolithEmpoweredPopup.EnablePopup), typeof(TimelineID))]
internal static class EmpoweredPopupShownPatch
{
    private static void Postfix(MonolithEmpoweredPopup __instance)
    {
        try { if (IsAlive(__instance)) ScreenContentAnnouncer.Track(__instance.panelInstance); }
        catch (Exception) { }
    }
}
