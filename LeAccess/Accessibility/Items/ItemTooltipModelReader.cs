using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LE.UI.Tooltips;
using LeAccess.Accessibility.Inventory;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Items;

internal static class ItemTooltipModelReader
{
    internal static string LastFailure { get; private set; }

    // Called only for explicit inspection, never from ordinary selection polling.
    internal static bool TryRead(ItemData expectedItem, ItemContainerUI container, out string text)
    {
        text = null;
        LastFailure = null;
        try
        {
            if (!OwnsSelection(expectedItem, container)) return false;
            if (!TryGetParameters(expectedItem, container, out var slot, out var mode)) return false;
            var unpacked = expectedItem.getAsUnpacked();
            if (!Matches(unpacked, expectedItem)) return false;
            // The native builder creates presentation data; it does not open a tooltip
            // or modify the item. See reference/item-tooltip-model.md.
            var info = ItemTooltipContentBuilder.GetItemTooltipInfo(unpacked, slot, mode);
            if (info is null || info.WasCollected || !Matches(info.Item, expectedItem) ||
                info.SlotType != slot || info.TooltipMode != mode) return false;
            string result = ScreenContentTracker.Combine(new[] { Read(info), MerchantItemReader.ReadContext(expectedItem, container) });
            if (string.IsNullOrWhiteSpace(result) || !OwnsSelection(expectedItem, container)) return false;
            text = result;
            return true;
        }
        catch (Exception ex) { LastFailure = $"{ex.GetType().Name}: {ex.Message}"; return false; }
    }

    internal static bool OwnsSelection(ItemData item, ItemContainerUI container)
    {
        return InventorySelectionReader.OwnsSelection(item, container);
    }

    internal static bool TryGetParameters(ItemData item, ItemContainerUI container,
        out TooltipItemManager.SlotType slot, out TooltipMode mode)
    {
        slot = TooltipItemManager.SlotType.UNDEFINED;
        mode = TooltipMode.DefaultInfo;
        var ui = UIBase.instance;
        var system = IsAlive(ui) ? ui.TooltipSystem : null;
        var parameters = IsAlive(system) ? system._activeItemTooltipParameters : null;
        if (parameters is not null && !parameters.WasCollected && Matches(parameters._Item_k__BackingField, item) &&
            IsAlive(parameters._Opener_k__BackingField) &&
            parameters._Opener_k__BackingField.Pointer == container.gameObject.Pointer &&
            parameters._SlotType_k__BackingField != TooltipItemManager.SlotType.GROUND &&
            parameters._SlotType_k__BackingField != TooltipItemManager.SlotType.UNDEFINED)
        {
            slot = parameters._SlotType_k__BackingField;
            mode = parameters._TooltipMode_k__BackingField;
            return true;
        }
        var source = container.Container;
        if (source is null || source.WasCollected) return false;
        if (MerchantItemReader.TryGetShopSlot(container, out slot)) return true;
        // These three mappings are the direct native OnItemHovered branches.
        switch (source.Type)
        {
            case ContainerType.INVENTORY: slot = TooltipItemManager.SlotType.INVENTORY; return true;
            case ContainerType.EQUIPPED: slot = TooltipItemManager.SlotType.EQUIPPED; return true;
            case ContainerType.STASH: slot = TooltipItemManager.SlotType.STASH; return true;
            default: return false;
        }
    }

    private static bool Matches(ItemData actual, ItemData expected) =>
        actual is not null && !actual.WasCollected && expected is not null && !expected.WasCollected &&
        (actual.Pointer == expected.Pointer || actual.IdEquals(expected));

    private static string Read(ItemTooltipInfo info)
    {
        var fragments = new List<string>();
        void Add(string value) => fragments.Add(TextUtils.CleanText(value));
        Add(info.Name);
        Add(info.PrimaryType);
        Add(info.SecondaryType);
        if (info.IsUnidentified) return ScreenContentTracker.Combine(fragments);
        Add(info.ToDescriptionString(false));
        var potential = info.PotentialInfo;
        if (potential is not null && !potential.WasCollected && potential.ShouldShow)
            Add(potential.ToSearchableString());
        if (info.IsWeapon)
        {
            Add(info.ToRangeWithLabelString(false));
            Add(info.ToBaseAttackRateWithLabelString(false));
        }
        AddModifiers(fragments, info.Implicits);
        AddAffixes(fragments, info.Prefixes);
        AddAffixes(fragments, info.Suffixes);
        AddAffixes(fragments, info.SealedAffixes);
        AddModifiers(fragments, info.UniqueModifiers);
        Add(info.SetName);
        var setItems = info.SetItems;
        if (setItems is not null && !setItems.WasCollected)
            foreach (var setItem in setItems)
                if (setItem is not null && !setItem.WasCollected) Add(setItem.ToItemNameString(false));
        var bonuses = info.SetBonuses;
        if (bonuses is not null && !bonuses.WasCollected)
            foreach (var bonus in bonuses)
                if (bonus is not null && !bonus.WasCollected)
                {
                    Add(bonus.GetSetRequirementString());
                    AddModifiers(fragments, bonus.Modifiers);
                }
        AddAffixes(fragments, info.AddsAffixes);
        var equipmentTypes = info.CanRollOn;
        if (equipmentTypes is not null && !equipmentTypes.WasCollected)
            foreach (var equipmentType in equipmentTypes)
                if (equipmentType is not null && !equipmentType.WasCollected) Add(equipmentType.Text);
        Add(info.Item.LoreText);
        Add(TooltipItemManager.ReqFaction(info.Item, info.SlotType));
        Add(info.ToRequirementsString(false));
        return ScreenContentTracker.Combine(fragments);
    }

    private static void AddModifiers(List<string> fragments,
        Il2CppSystem.Collections.Generic.List<ItemTooltipModifierInfo> modifiers)
    {
        if (modifiers is null || modifiers.WasCollected) return;
        foreach (var modifier in modifiers)
            if (modifier is not null && !modifier.WasCollected)
                fragments.Add(TextUtils.CleanText(modifier.ToModifierString(false)));
    }

    private static void AddAffixes(List<string> fragments,
        Il2CppSystem.Collections.Generic.List<ItemTooltipAffixInfo> affixes)
    {
        if (affixes is null || affixes.WasCollected) return;
        foreach (var affix in affixes)
        {
            if (affix is null || affix.WasCollected) continue;
            if (affix.ShowTier) fragments.Add(TextUtils.CleanText(affix.ToLocalizedTierString(false)));
            var first = affix.Modifier1;
            var second = affix.Modifier2;
            if (first is not null && !first.WasCollected) fragments.Add(TextUtils.CleanText(first.ToModifierString(false)));
            if (second is not null && !second.WasCollected) fragments.Add(TextUtils.CleanText(second.ToModifierString(false)));
        }
    }
}
