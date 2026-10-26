using LeAccess.Accessibility.Items;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Endgame;

/// <summary>Reads the one-cell item containers that endgame panels show (Morditas rewards, Nemesis items).</summary>
internal static class ItemSlotReader
{
    /// <summary>The item entry the game keeps in the slot's single cell; null when empty.</summary>
    internal static ItemContainerEntry Entry(IItemContainer source) =>
        source is null || source.WasCollected ? null : source.GetEntryAt(Vector2Int.zero);

    internal static ItemContainerEntry Entry(ItemContainerUI container) => IsAlive(container) ? Entry(container.Container) : null;

    internal static ItemData Data(ItemContainerEntry entry) => entry is not null && !entry.WasCollected ? entry.data : null;

    /// <summary>The item's name and base type, or "empty slot".</summary>
    internal static string Identity(ItemContainerEntry entry) =>
        entry is null ? ModText.Get("endgame.slot_empty")
            : ItemIdentityReader.Read(Data(entry)) ?? ModText.Get("inventory.selection_unavailable");

    /// <summary>The item tooltip the game shows beside the focused slot, as the inventory reads it on request.</summary>
    internal static string Details(ItemData data, GameObject slot) =>
        data is not null && !data.WasCollected && IsAlive(slot) &&
        ItemTooltipReader.TryReadForItem(data, slot, out string text) ? text : null;
}
