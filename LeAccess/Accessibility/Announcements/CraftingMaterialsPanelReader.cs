using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

/// <summary>The crafting materials panel's own stops: the shard filters and the shard list's section stops.</summary>
/// <remarks>See reference/crafting-reading.md#crafting-materials-from-the-inventory-2026-10-07.</remarks>
internal static class CraftingMaterialsPanelReader
{
    internal static bool TryRead(GameObject element, out string text)
    {
        text = null;
        try
        {
            var panel = element.GetComponentInParent<CraftingMaterialsPanelUI>();
            if (!IsAlive(panel) || !panel.isActiveAndEnabled || !UiVisibility.IsVisible(element)) return false;
            return TryReadFilter(panel, element, out text) || TryReadSection(panel, element, out text);
        }
        catch (System.Exception) { text = null; return false; }
    }

    // From the filters the game moves Up to the runes and glyphs and Down to the shards (HandleNavigateInput).
    private static bool TryReadFilter(CraftingMaterialsPanelUI panel, GameObject element, out string text)
    {
        text = null;
        var tab = element.GetComponent<TabUIElement>();
        if (!IsAlive(tab) || !Same(tab, panel.allAffixes) && !Same(tab, panel.prefixesOnly) && !Same(tab, panel.suffixesOnly))
            return false;
        string caption = TextUtils.GetTextFromHierarchy(element);
        if (caption == null) return false;
        text = ScreenContentTracker.Combine(new[]
        {
            ModText.Format(Same(tab, panel.currentlySelected) ? "crafting.filter_selected" : "crafting.filter", caption),
            Label(panel.noShardNotice, panel.gameObject), ModText.Get("crafting.materials_directions")
        });
        return true;
    }

    // The shard list stops on each section's container between rows; that stop speaks the section's heading.
    private static bool TryReadSection(CraftingMaterialsPanelUI panel, GameObject element, out string text)
    {
        text = null;
        var group = element.GetComponent<PooledVerticalLayoutGroup>();
        if (!IsAlive(group)) return false;
        Transform header;
        if (Same(group, panel._availableAffixesGroup)) header = panel.unusedAffixesHeader;
        else if (Same(group, panel._appliedAffixesGroup)) header = panel.appliedAffixesHeader;
        else if (Same(group, panel._incompatibleAffixesGroup)) header = panel.incompatibleAffixesHeader;
        else return false;
        // A known section never falls back to its object name, even without a readable heading.
        text = IsAlive(header) && header.IsChildOf(panel.transform) ? TextUtils.GetTextFromHierarchy(header.gameObject) : null;
        return true;
    }

    private static string Label(TMP_Text label, GameObject owner)
    {
        string value = TextUtils.IsVisibleLabel(label) && label.transform.IsChildOf(owner.transform) ? TextUtils.CleanText(label.text) : null;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool Same(Component first, Component second) => IsAlive(first) && IsAlive(second) && first.Pointer == second.Pointer;
}
