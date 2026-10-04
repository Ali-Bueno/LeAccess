using System;
using System.Collections.Generic;
using LE.UI.Tooltips;
using IconSprite = LE.UI.Tooltips.ItemTooltipAssetList.ItemTooltipModifierIconSprite;

namespace LeAccess.Accessibility.Items;

/// <summary>Remembers the comparison icon the game draws on each tooltip modifier line; the icon is visual only.</summary>
internal static class ItemComparisonIcons
{
    // Native ItemTooltip.ShowComparison sets per-line icons through SetIcon; OnDataUpdate draws Info.IconSprite instead.
    private static readonly Dictionary<IntPtr, IconSprite> Overrides = new();

    internal static void Set(ItemTooltipModifier modifier, IconSprite icon)
    {
        if (modifier is not null && !modifier.WasCollected) Overrides[modifier.Pointer] = icon;
    }

    internal static void Clear(ItemTooltipModifier modifier)
    {
        if (modifier is not null && !modifier.WasCollected) Overrides.Remove(modifier.Pointer);
    }

    internal static IconSprite? Get(ItemTooltipModifier modifier)
    {
        if (modifier is null || modifier.WasCollected) return null;
        if (Overrides.TryGetValue(modifier.Pointer, out var icon)) return icon;
        var info = modifier.Info;
        return info is null || info.WasCollected ? null : info.IconSprite;
    }

    internal static void Reset() => Overrides.Clear();
}
