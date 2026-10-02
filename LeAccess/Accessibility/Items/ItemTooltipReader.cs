using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LE.UI.Tooltips;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Items;

internal static class ItemTooltipReader
{
    internal static bool TryReadForItem(ItemData expectedItem, GameObject expectedContainer, out string text)
    {
        text = null;
        var system = GetSystem();
        if (UsesModernTooltip(system))
            return IsModernVisible(system) && TryReadModern(system, expectedItem, expectedContainer, null, null, out text);
        if (!TryGetVisible(out var manager, out var tooltip)) return false;
        var parameters = manager.activeParameters;
        if (parameters is null || parameters.WasCollected ||
            parameters._SlotType_k__BackingField == TooltipItemManager.SlotType.GROUND ||
            !MatchesItem(parameters._Item_k__BackingField, expectedItem)) return false;
        var opener = parameters._Opener_k__BackingField;
        if (!IsAlive(opener) || !IsAlive(expectedContainer) ||
            !IsAlive(opener.transform) || !IsAlive(expectedContainer.transform) ||
            !opener.transform.IsChildOf(expectedContainer.transform)) return false;
        text = Read(tooltip);
        return !string.IsNullOrWhiteSpace(text);
    }

    internal static bool TryReadForOpener(GameObject expectedOpener, TooltipItemManager.SlotType expectedSlot,
        out string text, ItemData expectedItem = null)
    {
        text = null;
        var system = GetSystem();
        if (UsesModernTooltip(system))
            return IsModernVisible(system) && TryReadModern(system, expectedItem, null, expectedOpener, expectedSlot, out text);
        if (!TryGetVisible(out var manager, out var tooltip) || !IsAlive(expectedOpener)) return false;
        var parameters = manager.activeParameters;
        if (parameters is null || parameters.WasCollected || parameters._SlotType_k__BackingField != expectedSlot)
            return false;
        var opener = parameters._Opener_k__BackingField;
        if (!IsAlive(opener) || opener.Pointer != expectedOpener.Pointer ||
            expectedItem is not null && !MatchesItem(parameters._Item_k__BackingField, expectedItem)) return false;
        text = Read(tooltip);
        return !string.IsNullOrWhiteSpace(text);
    }

    internal static bool IsPreparingForItem(ItemData item, GameObject expectedContainer)
    {
        var system = GetSystem();
        if (!IsAlive(system)) return false;
        var current = system._activeItemTooltipParameters;
        if (MatchesParameters(current, item, expectedContainer, null, null) &&
            MatchesItem(system.ActiveItemTooltipItem, item)) return !system.IsItemTooltipReady;
        var manager = system._tooltipItemManager;
        if (!IsAlive(manager) || !manager.active || !IsAlive(expectedContainer) ||
            manager.activeInitializationStage == TooltipItemManager.TooltipInitializationStage.None ||
            manager.activeInitializationStage == TooltipItemManager.TooltipInitializationStage.Completed) return false;
        var parameters = manager.activeParameters;
        if (parameters is null || parameters.WasCollected ||
            parameters._SlotType_k__BackingField == TooltipItemManager.SlotType.GROUND ||
            !MatchesItem(parameters._Item_k__BackingField, item)) return false;
        var opener = parameters._Opener_k__BackingField;
        return IsAlive(opener) && IsAlive(opener.transform) && IsAlive(expectedContainer.transform) &&
            opener.transform.IsChildOf(expectedContainer.transform);
    }

    private static bool MatchesItem(ItemData actual, ItemData expected) =>
        actual is not null && !actual.WasCollected && expected is not null && !expected.WasCollected &&
        (actual.Pointer == expected.Pointer || actual.IdEquals(expected));

    private static bool TryGetVisible(out TooltipItemManager manager, out UITooltipItem tooltip)
    {
        var system = GetSystem();
        manager = IsAlive(system) ? system._tooltipItemManager : null;
        tooltip = null;
        if (!IsAlive(system) || !system.IsItemTooltipReady || !IsAlive(manager) || !manager.active ||
            manager.activeInitializationStage != TooltipItemManager.TooltipInitializationStage.Completed) return false;
        var activeRect = system.ActiveItemTooltipRect;
        var legacyRect = manager.ActiveTooltipRect;
        if (!IsAlive(activeRect) || !IsAlive(legacyRect) || activeRect.Pointer != legacyRect.Pointer) return false;
        tooltip = manager.itemTooltip;
        return IsAlive(tooltip) && tooltip.tooltipActive && tooltip.contentVisible &&
            UiVisibility.IsVisible(tooltip.gameObject) && IsAlive(tooltip.content) &&
            UiVisibility.IsVisible(tooltip.content);
    }

    private static TooltipSystem GetSystem()
    {
        var ui = UIBase.instance;
        if (!IsAlive(ui)) return null;
        var system = ui.TooltipSystem;
        return IsAlive(system) && system.isActiveAndEnabled ? system : null;
    }

    private static bool IsModernVisible(TooltipSystem system) => IsAlive(system) &&
        IsAlive(system._itemTooltip) && system._itemTooltip.isActiveAndEnabled &&
        UiVisibility.IsVisible(system._itemTooltip.gameObject);

    private static bool UsesModernTooltip(TooltipSystem system)
    {
        if (!IsAlive(system) || !IsAlive(system._itemTooltip)) return false;
        var activeRect = system.ActiveItemTooltipRect;
        var modernRect = system._itemTooltip.RectTransform;
        return IsAlive(activeRect) && IsAlive(modernRect) && activeRect.Pointer == modernRect.Pointer;
    }

    private static bool TryReadModern(TooltipSystem system, ItemData item, GameObject container,
        GameObject opener, TooltipItemManager.SlotType? slot, out string text)
    {
        text = null;
        if (!system.IsItemTooltipReady ||
            !MatchesParameters(system._activeItemTooltipParameters, item, container, opener, slot)) return false;
        var tooltip = system._itemTooltip;
        var info = tooltip._info;
        var activeInfo = system._activeItemTooltipInfo;
        if (info is null || info.WasCollected || activeInfo is null || activeInfo.WasCollected ||
            info.Pointer != activeInfo.Pointer || !MatchesItem(info._Item_k__BackingField, system.ActiveItemTooltipItem) ||
            item is not null && !MatchesItem(info._Item_k__BackingField, item)) return false;
        text = ItemTooltipV2Reader.Read(tooltip);
        return !string.IsNullOrWhiteSpace(text);
    }

    private static bool MatchesParameters(TooltipItemManager.ItemTooltipParameters parameters, ItemData item,
        GameObject container, GameObject opener, TooltipItemManager.SlotType? slot)
    {
        if (parameters is null || parameters.WasCollected ||
            (slot.HasValue ? parameters._SlotType_k__BackingField != slot.Value :
                parameters._SlotType_k__BackingField == TooltipItemManager.SlotType.GROUND) ||
            item is not null && !MatchesItem(parameters._Item_k__BackingField, item)) return false;
        var actualOpener = parameters._Opener_k__BackingField;
        if (!IsAlive(actualOpener)) return false;
        if (slot.HasValue) return IsAlive(opener) && actualOpener.Pointer == opener.Pointer;
        return IsAlive(container) && IsAlive(actualOpener.transform) && IsAlive(container.transform) &&
            actualOpener.transform.IsChildOf(container.transform);
    }

    private static string Read(UITooltipItem tooltip)
    {
        // Comparison has its own root; only the selected item's primary content belongs in this reading.
        string text = ScreenContentReader.Read(tooltip.content, tooltip.nameText, tooltip.typeText);
        var footer = tooltip.requirementsFooter;
        if (IsAlive(footer) && UiVisibility.IsVisible(footer) && IsAlive(footer.transform) &&
            IsAlive(tooltip.content.transform) && !footer.transform.IsChildOf(tooltip.content.transform))
            text = ScreenContentTracker.Combine(new[] { text, ScreenContentReader.Read(footer, null, null) });
        return text;
    }
}
