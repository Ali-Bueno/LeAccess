using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

internal static class CraftingMaterialReader
{
    internal static bool TryRead(GameObject element, out string text, out bool unavailable)
    {
        text = null;
        unavailable = false;
        try
        {
            if (!CraftingSelectionReader.TryReadSelected(NavigationManager.currentlySelected, out var selected, out _) ||
                element.Pointer != selected.Pointer) return false;
            var material = selected.GetComponentInParent<MaterialContainerUI>();
            if (!IsAlive(material) || !material.isActiveAndEnabled || !UiVisibility.IsVisible(material.gameObject)) return false;
            var raw = material.container;
            var source = raw is not null && !raw.WasCollected ? raw.TryCast<SingleSubTypeContainer>() : null;
            if (source is null || source.WasCollected || source.GetContainerID() != material.identifier) return false;
            var types = source.allowedBaseType;
            var localization = Localization.Items;
            if (types is null || types.WasCollected || types.Length != 1 || source.allowedSubID < 0 ||
                localization is null || localization.WasCollected) return false;
            string name = TextUtils.CleanText(localization.GetSubTypeName(types[0], source.allowedSubID));
            if (string.IsNullOrWhiteSpace(name)) return false;
            var label = material.externalItemNumber;
            string quantity = TextUtils.IsVisibleLabel(label) && label.transform.IsChildOf(material.transform)
                ? TextUtils.CleanText(label.text) : null;
            text = ScreenContentTracker.Combine(new[] { name,
                string.IsNullOrWhiteSpace(quantity) ? null : ModText.Format("crafting.quantity", quantity) });
            unavailable = !material.interactable;
            return true;
        }
        catch (System.Exception) { text = null; unavailable = false; return false; }
    }
}
