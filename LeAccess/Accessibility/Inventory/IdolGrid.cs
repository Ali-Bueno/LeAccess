using System;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using SlotState = IdolsItemContainer.IdolSlotState;

namespace LeAccess.Accessibility.Inventory;

/// <summary>Speech for the equipped idol grid and the idol altar above it, from the game's own slot states.</summary>
/// <remarks>See reference/idols.md.</remarks>
internal static class IdolGrid
{
    internal static bool Is(ItemContainerUI container) =>
        IsAlive(container) && container.TryCast<IdolsContainerUI>() is not null && Slots(container) is not null;

    /// <summary>The altar slot's name; null for any other container.</summary>
    internal static string SlotName(ItemContainerUI container) =>
        IsAlive(container) && container.TryCast<IdolsInventoryContainerUI>() is not null ? ModText.Get("idols.altar") : null;

    /// <summary>The grid's name, its equipped idols, free slots and slots still locked; null outside the grid.</summary>
    internal static string Summary(ItemContainerUI container)
    {
        try
        {
            var slots = Is(container) ? Slots(container) : null;
            var content = slots?.GetContent();
            if (content is null || content.WasCollected) return null;
            int idols = 0, free = 0, locked = 0;
            for (int i = 0; i < content.Count; i++)
                if (content[i] is { WasCollected: false }) idols++;
            var size = slots.GetSize();
            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                    switch (slots.GetSlotState(x, y, out _))
                    {
                        case SlotState.Unlocked when slots.GetEntryAt(new Vector2Int { x = x, y = y }) is null: free++; break;
                        case SlotState.Locked: locked++; break;
                    }
            return ScreenContentTracker.Combine(new[]
            {
                ModText.Get("idols.grid"), Count("idols.equipped", idols), Count("idols.free", free),
                locked > 0 ? Count("idols.locked", locked) : null
            });
        }
        catch (Exception) { return null; }
    }

    /// <summary>An empty cell: a free slot, a slot still locked, or no slot in the altar's shape; null outside the grid.</summary>
    internal static string EmptyCell(ItemContainerUI container, Vector2Int cell)
    {
        try
        {
            var slots = Is(container) ? Slots(container) : null;
            if (slots is null || !Inside(slots.GetSize(), cell.x, cell.y)) return null;
            return slots.GetSlotState(cell.x, cell.y, out bool refracted) switch
            {
                SlotState.Unlocked => ModText.Get(refracted ? "idols.free_refracted_slot" : "idols.free_slot"),
                SlotState.Locked => ModText.Get("idols.locked_slot"),
                _ => ModText.Get("idols.no_slot")
            };
        }
        catch (Exception) { return null; }
    }

    /// <summary>An equipped idol's width and height, and whether it covers a refracted slot; null outside the grid.</summary>
    internal static string Describe(ItemContainerUI container, ItemContainerEntry idol)
    {
        try
        {
            var slots = Is(container) ? Slots(container) : null;
            if (slots is null || idol is null || idol.WasCollected) return null;
            var grid = slots.GetSize();
            var corner = idol.Position;
            var size = idol.size;
            bool refracted = false;
            for (int x = corner.x; x < corner.x + size.x; x++)
                for (int y = corner.y; y < corner.y + size.y; y++)
                    if (Inside(grid, x, y))
                    {
                        slots.GetSlotState(x, y, out bool cell);
                        refracted |= cell;
                    }
            return ScreenContentTracker.Combine(new[]
            {
                ModText.Format("idols.size", size.x, size.y), refracted ? ModText.Get("idols.on_refracted") : null
            });
        }
        catch (Exception) { return null; }
    }

    private static IdolsItemContainer Slots(ItemContainerUI container)
    {
        var items = container.Container;
        var idols = items is not null && !items.WasCollected ? items.TryCast<IdolsItemContainer>() : null;
        return idols is not null && !idols.WasCollected ? idols : null;
    }

    private static bool Inside(Vector2Int size, int x, int y) => x >= 0 && y >= 0 && x < size.x && y < size.y;

    private static string Count(string key, int count) => ModText.Format(count == 1 ? key + "_one" : key + "_many", count);
}
