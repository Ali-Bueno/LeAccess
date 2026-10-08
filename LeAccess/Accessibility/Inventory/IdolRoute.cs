using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Inventory;

/// <summary>Says on which equipment slots a Right press leaves for the idol altar or the idol grid.</summary>
/// <remarks>See reference/idols.md.</remarks>
internal static class IdolRoute
{
    // EquippedItemsNavigable.HandleNavigateInput: Right past the last column selects the altar from the
    // equipment's top row, the idol grid from the second and the crafting buttons from the rest.
    private const int AltarRow = 0, GridRow = 1;

    /// <summary>"Right: idols" on the selected equipment slot when that press leads there; otherwise null.</summary>
    internal static string Hint()
    {
        try
        {
            var selected = NavigationManager.currentlySelected;
            var equipment = selected is not null && !selected.WasCollected ? selected.TryCast<EquippedItemsNavigable>() : null;
            if (!IsAlive(equipment)) return null;
            var index = equipment.currentlySelectedIdx;
            var (target, key) = index.y switch
            {
                AltarRow => (NavigableID.IDOL_ALTAR, "idols.route_altar"),
                GridRow => (NavigableID.IDOLS_CONTAINER, "idols.route_grid"),
                _ => (default(NavigableID), null)
            };
            return key != null && LastInRow(equipment.rects, index) && IsShown(target) ? ModText.Get(key) : null;
        }
        catch (Exception) { return null; }
    }

    // The game's Right step skips empty cells and the rest of the same slot, so only those may lie to the right.
    private static bool LastInRow(Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase rects, Vector2Int index)
    {
        var grid = InventorySelectionReader.ReadGrid(rects);
        if (grid is null || index.x < 0 || index.y < 0 || index.x >= grid.GetLength(0) || index.y >= grid.GetLength(1))
            return false;
        var own = InventorySelectionReader.ReadCell(rects, index);
        for (int x = index.x + 1; x < grid.GetLength(0); x++)
        {
            var rect = InventorySelectionReader.ReadCell(rects, new Vector2Int { x = x, y = index.y })?.TryCast<RectTransform>();
            if (IsAlive(rect) && (own is null || rect.Pointer != own.Pointer)) return false;
        }
        return true;
    }

    private static bool IsShown(NavigableID id)
    {
        var manager = NavigationManager.instance;
        if (!IsAlive(manager) || !manager.GetNavigableFromID(id, out var navigable) ||
            navigable is null || navigable.WasCollected) return false;
        var container = navigable.TryCast<ItemContainerUI>();
        return IsAlive(container) && container.isActiveAndEnabled && UiVisibility.IsVisible(container.gameObject);
    }
}
