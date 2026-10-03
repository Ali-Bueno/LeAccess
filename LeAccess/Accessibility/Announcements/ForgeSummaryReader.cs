using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

internal static class ForgeSummaryReader
{
    internal static bool TryRead(GameObject element, out string text)
    {
        text = null;
        try
        {
            if (!UiFocusOwnership.Allows(element)) return false;
            var navigation = NavigationManager.currentlySelected;
            var forge = navigation is not null && !navigation.WasCollected ? navigation.TryCast<CraftingSlotManager>() : null;
            if (!IsAlive(forge) || !forge.isActiveAndEnabled ||
                !CraftingSelectionReader.TryReadSelected(navigation, out var selected, out _) ||
                selected.Pointer != element.Pointer) return false;
            if (!forge.canForge)
                text = Read(forge.cannotForgeText, forge.gameObject);
            else
            {
                string heading = Read(forge.typeOfForgeText, forge.gameObject);
                string potential = Read(forge.currentForgingPotentialText, forge.gameObject);
                text = ScreenContentTracker.Combine(new[] {
                    Read(forge.singleSuccessChanceString, forge.gameObject),
                    Read(forge.modifierDetails, forge.gameObject),
                    Read(forge.glyphsPreviewText, forge.gameObject),
                    heading is null || potential is null ? null : heading + ": " + potential });
            }
            return !string.IsNullOrWhiteSpace(text);
        }
        catch (System.Exception) { text = null; return false; }
    }

    private static string Read(TMP_Text label, GameObject owner)
    {
        if (!TextUtils.IsVisibleLabel(label) || !label.transform.IsChildOf(owner.transform)) return null;
        string raw = label.text;
        // An unresolved icon can carry essential meaning in the native preview.
        if (raw?.IndexOf("<sprite", System.StringComparison.OrdinalIgnoreCase) >= 0) return null;
        string value = TextUtils.CleanText(raw);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
