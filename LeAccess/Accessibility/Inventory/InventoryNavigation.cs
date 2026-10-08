using System;
using System.Collections.Generic;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using Anchor = LeAccess.Accessibility.Inventory.InventoryNavigationOrder.Anchor;
using Edges = LeAccess.Accessibility.Inventory.InventoryNavigationOrder.Edges;

namespace LeAccess.Accessibility.Inventory;

/// <summary>Moves the backpack, merchant and idol cursors item by item instead of the native cell steps.</summary>
/// <remarks>See reference/inventory-drop-focus.md#controller-navigation-2026-10-04 and reference/idols.md.</remarks>
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
            var exits = ListExits(container);
            if (exits == Edges.None || container.singleSlotControllerNavigation || HoldsItem() ||
                !TryReadItems(container, out var size)) return false;
            int current = CurrentIndex(container);
            var slot = container.currentFocusSlot;
            var move = InventoryNavigationOrder.Plan(Order, current, slot.x, slot.y, direction, exits);
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
                    // From the border cell on that side the game's own step leaves the grid, as from this item's row or column.
                    var from = current >= 0 ? Items[current].Item : new Anchor(slot.x, slot.y);
                    container.currentFocusSlot = direction switch
                    {
                        Direction.UP => new Vector2Int { x = from.Column, y = size.y - 1 },
                        Direction.DOWN => new Vector2Int { x = from.Column, y = 0 },
                        Direction.LEFT => new Vector2Int { x = 0, y = from.Top },
                        _ => new Vector2Int { x = size.x - 1, y = from.Top }
                    };
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
        if (entry is null || entry.WasCollected || ListExits(container) == Edges.None || !TryReadItems(container, out _))
            return false;
        int index = IndexOf(entry);
        if (index < 0) return false;
        position = index + 1;
        count = Items.Count;
        return true;
    }

    // Grids read as item lists, with the sides where the game's step leaves them; their other edges wrap.
    private static Edges ListExits(ItemContainerUI container)
    {
        var items = IsAlive(container) ? container.Container : null;
        if (items is null || items.WasCollected) return Edges.None;
        var id = items.GetContainerID();
        // The backpack and the ordinary merchant's stock and buyback exit only upward; faction shops override the edges.
        if (id == ContainerID.INVENTORY ||
            id is ContainerID.SHOP or ContainerID.SHOP_BUYBACK && container.TryCast<LE.Factions.ShopContainerUI>() is null)
            return Edges.Up;
        // IdolsContainerUI.DpadContextChange: left to the equipment, up to the altar, down to the crafting buttons.
        return id == ContainerID.IDOLS && IdolGrid.Is(container) ? Edges.Left | Edges.Up | Edges.Down : Edges.None;
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
