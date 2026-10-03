using LeAccess.Accessibility.Announcements;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Inventory;

internal static class InventorySelectionReader
{
    internal static ItemContainerUI GetFocusedContainer()
    {
        var selected = NavigationManager.currentlySelected;
        if (selected is null || selected.WasCollected) return null;
        var container = selected.TryCast<ItemContainerUI>();
        if (!IsAlive(container))
        {
            var equipment = selected.TryCast<EquippedItemsNavigable>();
            if (!IsAlive(equipment) || !equipment.isActiveAndEnabled ||
                !UiVisibility.IsVisible(equipment.gameObject)) return null;
            var cell = ReadCell(equipment.navigables, equipment.currentlySelectedIdx);
            container = cell?.TryCast<ItemContainerUI>();
            if (!IsAlive(container) || !IsEquipmentSlot(container.identifier)) return null;
        }
        return IsAlive(container) && container.isActiveAndEnabled && UiVisibility.IsVisible(container.gameObject)
            ? container : null;
    }

    internal static bool OwnsElement(ItemContainerUI container, GameObject element)
    {
        if (!IsAlive(container) || !IsAlive(element) || !UiVisibility.IsVisible(element)) return false;
        if (Contains(container.gameObject, element)) return true;
        var selected = NavigationManager.currentlySelected;
        var equipment = selected is not null && !selected.WasCollected ? selected.TryCast<EquippedItemsNavigable>() : null;
        if (!IsAlive(equipment)) return false;
        // Native equipment focus highlights rects[x,y], which can be separate from the item container.
        var cell = ReadCell(equipment.rects, equipment.currentlySelectedIdx);
        var rect = cell?.TryCast<RectTransform>();
        return IsAlive(rect) && UiVisibility.IsVisible(rect.gameObject) && Contains(rect.gameObject, element);
    }

    internal static bool TryGetEntry(ItemContainerUI container, out ItemContainerEntry entry)
    {
        entry = null;
        if (!IsAlive(container)) return false;
        if (IsEquipmentSlot(container.identifier))
        {
            // Read the actual slot after swaps/removal, not a retained controller-hover entry.
            var source = container.container;
            if (source is null || source.WasCollected || source.Type != ContainerType.EQUIPPED ||
                source.GetContainerID() != container.identifier) return false;
            entry = source.GetEntryAt(container.currentFocusSlot);
        }
        else
        {
            var source = container.Container;
            if (source is null || source.WasCollected) return false;
            var hovered = container.currentlySelectedItem;
            if (hovered is not null && hovered.WasCollected) return false;
            if (hovered is not null && source.Type is ContainerType.STASH or ContainerType.SHOP)
            {
                // Stash tabs and shop stock can change before the retained hover entry is refreshed.
                var item = hovered.data;
                var stored = source.GetEntryAt(hovered.Position);
                var actual = stored is not null && !stored.WasCollected ? stored.data : null;
                if (item is null || item.WasCollected || actual is null || actual.WasCollected ||
                    !MatchesItem(actual, item)) return false;
                entry = stored;
            }
            else entry = hovered;
        }
        return entry is null || !entry.WasCollected;
    }

    internal static bool OwnsSelection(ItemData item, ItemContainerUI container)
    {
        if (item is null || item.WasCollected || !IsAlive(container)) return false;
        var selected = GetFocusedContainer();
        if (!IsAlive(selected) || selected.Pointer != container.Pointer ||
            !TryGetEntry(container, out var entry) || entry is null) return false;
        var actual = entry.data;
        return actual is not null && !actual.WasCollected &&
            MatchesItem(actual, item);
    }

    private static bool MatchesItem(ItemData actual, ItemData item)
    {
        if (actual.Pointer == item.Pointer) return true;
        // IdEquals compares serialized bytes and accepts null/null or empty/empty.
        // Those cannot identify distinct packed/unpacked wrappers.
        var actualId = actual.id;
        var itemId = item.id;
        return actualId is not null && !actualId.WasCollected && actualId.Length > 0 &&
            itemId is not null && !itemId.WasCollected && itemId.Length > 0 && actual.IdEquals(item);
    }

    internal static bool IsEquipmentSlot(ContainerID id) => id is
        ContainerID.EQ_HELMET or ContainerID.EQ_ARMOR or ContainerID.EQ_WEAPON or ContainerID.EQ_OFFHAND or
        ContainerID.EQ_GLOVES or ContainerID.EQ_BELT or ContainerID.EQ_BOOTS or ContainerID.EQ_RIGHTRING or
        ContainerID.EQ_LEFTRING or ContainerID.EQ_AMULET or ContainerID.EQ_RELIC;

    private static Il2CppSystem.Object ReadCell(Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase raw, Vector2Int index)
    {
        if (raw is null || raw.WasCollected) return null;
        var cells = raw.TryCast<Il2CppSystem.Array>();
        if (cells is null || cells.WasCollected || cells.Rank != 2 || index.x < 0 || index.y < 0 ||
            index.x >= cells.GetLength(0) || index.y >= cells.GetLength(1)) return null;
        // EquippedItemsNavigable.Select uses [x,y]; see reference/equipped-items.md.
        var cell = cells.GetValue(index.x, index.y);
        return cell is not null && !cell.WasCollected ? cell : null;
    }

    private static bool Contains(GameObject root, GameObject element) =>
        IsAlive(root.transform) && IsAlive(element.transform) && element.transform.IsChildOf(root.transform);
}
