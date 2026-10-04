using System;
using System.Collections.Generic;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using Anchor = LeAccess.Accessibility.Inventory.InventoryNavigationOrder.Anchor;

namespace LeAccess.Accessibility.Inventory;

/// <summary>Moves the backpack and merchant cursors item by item instead of the native cell steps that wrap around.</summary>
/// <remarks>See reference/inventory-drop-focus.md#controller-navigation-2026-10-04.</remarks>
internal static class InventoryNavigation
{
    private static readonly List<(Anchor Item, ItemContainerEntry Entry)> Items = new();
    private static readonly List<Anchor> Order = new();
    private static string _lastFailure;

    /// <summary>Handles a d-pad step in an item grid; false lets the game's own step run.</summary>
    internal static bool TryNavigate(ItemContainerUI container, Direction direction)
    {
        try
        {
            // A held item moves its anchor through the game's own placement navigation.
            if (!IsItemList(container) || container.singleSlotControllerNavigation || HoldsItem() ||
                !TryReadItems(container, out var size)) return false;
            int current = CurrentIndex(container);
            var slot = container.currentFocusSlot;
            var move = InventoryNavigationOrder.Plan(Order, current, slot.x, slot.y, direction);
            switch (move.Kind)
            {
                case InventoryNavigationOrder.MoveKind.Select:
                    var target = Items[move.Index];
                    container.currentFocusSlot = new Vector2Int { x = target.Item.Column, y = target.Item.Top };
                    container.SetFocusItem(target.Entry);
                    return true;
                case InventoryNavigationOrder.MoveKind.Stay:
                    InventoryAccessibility.RepeatCurrent();
                    return true;
                case InventoryNavigationOrder.MoveKind.Exit:
                    // From the top row the game's Up leaves upward from this column: equipment, crafting buttons or shop tabs.
                    int column = current >= 0 ? Items[current].Item.Column : slot.x;
                    container.currentFocusSlot = new Vector2Int { x = column, y = size.y - 1 };
                    return false;
                default:
                    return false;
            }
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    /// <summary>The item's place in its grid's reading order.</summary>
    internal static bool TryGetPosition(ItemContainerUI container, ItemContainerEntry entry, out int position, out int count)
    {
        position = count = 0;
        if (entry is null || entry.WasCollected || !IsItemList(container) || !TryReadItems(container, out _)) return false;
        int index = IndexOf(entry);
        if (index < 0) return false;
        position = index + 1;
        count = Items.Count;
        return true;
    }

    // Grids whose native edges exit only upward: the backpack and the ordinary merchant's stock and buyback.
    private static bool IsItemList(ItemContainerUI container)
    {
        var items = IsAlive(container) ? container.Container : null;
        if (items is null || items.WasCollected) return false;
        var id = items.GetContainerID();
        if (id == ContainerID.INVENTORY) return true;
        // Faction shops override the merchant's edge handling.
        return id is ContainerID.SHOP or ContainerID.SHOP_BUYBACK && container.TryCast<LE.Factions.ShopContainerUI>() is null;
    }

    private static bool HoldsItem()
    {
        var manager = ItemContainersManager.Instance;
        var cursor = IsAlive(manager) ? manager.cursor : null;
        return cursor is not null && !cursor.WasCollected && cursor.HasContent();
    }

    private static bool TryReadItems(ItemContainerUI container, out Vector2Int size)
    {
        Items.Clear();
        Order.Clear();
        var items = container.Container;
        size = items.GetSize();
        var content = items.GetContent();
        if (content is null || content.WasCollected) return false;
        for (int i = 0; i < content.Count; i++)
        {
            var entry = content[i];
            if (entry is null || entry.WasCollected) continue;
            var position = entry.Position;
            // Position is the item's bottom-left cell.
            Items.Add((new Anchor(position.x, position.y + Math.Max(entry.size.y, 1) - 1), entry));
        }
        Items.Sort((a, b) => InventoryNavigationOrder.Compare(a.Item, b.Item));
        foreach (var item in Items) Order.Add(item.Item);
        return true;
    }

    // The game's mouse-hover refresh can move only the logical slot, so the spoken item comes first.
    private static int CurrentIndex(ItemContainerUI container)
    {
        int index = IndexOf(container.currentlySelectedItem);
        return index >= 0 ? index : IndexOf(container.Container.GetEntryAt(container.currentFocusSlot));
    }

    private static int IndexOf(ItemContainerEntry entry)
    {
        if (entry is null || entry.WasCollected) return -1;
        for (int i = 0; i < Items.Count; i++)
            if (Items[i].Entry.Pointer == entry.Pointer) return i;
        return -1;
    }

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Inventory navigation fell back to the game: {ex}");
    }
}
