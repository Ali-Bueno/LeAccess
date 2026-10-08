using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

internal static class CraftingMaterialReader
{
    // Key template of ItemTooltipContentBuilder.GetDescription (build 25764382, 0x1FEC390): base type, then subtype.
    private const string DescriptionKey = "Item_SubType_Description_{0}_{1}";

    internal static bool TryRead(GameObject element, bool includeDetails, out string text, out bool unavailable)
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
                string.IsNullOrWhiteSpace(quantity) ? null : ModText.Format("crafting.quantity", quantity),
                includeDetails ? Description(types[0], source.allowedSubID) : null });
            unavailable = !material.interactable;
            return true;
        }
        catch (System.Exception) { text = null; unavailable = false; return false; }
    }

    // The game's own tooltip description: localized by key, the item list's text as fallback; needs no item in stock.
    private static string Description(int baseType, int subType)
    {
        var items = ItemList.get();
        var entry = items is not null && !items.WasCollected ? items.GetNonEquipmentSubItem(baseType, subType) : null;
        if (entry is null || entry.WasCollected) return null;
        string text = TextUtils.CleanText(Localization.GetText(string.Format(DescriptionKey, baseType, subType), entry.description));
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
