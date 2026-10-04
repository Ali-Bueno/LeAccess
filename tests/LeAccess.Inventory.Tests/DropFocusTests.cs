using LeAccess.Accessibility.Inventory;
using UnityEngine;

internal static class DropFocusTests
{
    internal static void Run(Action<bool, string> check)
    {
        var dropped = new ItemContainerEntry { Position = new() { x = 0, y = 0 } };
        var next = new ItemContainerEntry { Position = new() { x = 2, y = 0 } };
        var slots = new Dictionary<(int, int), ItemContainerEntry> { [(0, 0)] = dropped, [(2, 0)] = next };
        var container = new ItemContainerUI
        {
            container = new() { Type = ContainerType.INVENTORY, Slots = slots },
            currentFocusSlot = new() { x = 0, y = 0 }, NearestSlot = new() { x = 2, y = 0 }
        };
        ItemContainersManager.Instance = new ItemContainersManager();
        void Drop(bool removed = true)
        {
            InventoryDropFocus.BeforeDrop(container);
            if (removed) slots.Remove((container.currentFocusSlot.x, container.currentFocusSlot.y));
            container.forceUpdate = true;
            InventoryDropFocus.AfterDrop(container);
        }

        Time.unscaledTime = 10;
        Drop();
        check(container.currentFocusSlot.x == 2 && container.Focused.Count == 1 && container.Focused[0] == next,
            "A dropped item hands focus to the nearest remaining item through the game's own focus call");
        check(!container.forceUpdate, "The pending hover refresh that resets focus is disarmed");

        container.currentFocusSlot = new() { x = 0, y = 0 };
        Time.unscaledTime = 10.1f;
        InventoryDropFocus.Guard(container);
        check(container.currentFocusSlot.x == 2 && container.Focused.Count == 2,
            "A later non-navigation reset to the first slot is undone");
        InventoryDropFocus.Guard(container);
        check(container.Focused.Count == 2, "An unchanged guarded slot is left alone");

        InventoryDropFocus.CancelGuard();
        container.currentFocusSlot = new() { x = 0, y = 0 };
        InventoryDropFocus.Guard(container);
        check(container.currentFocusSlot.x == 0, "D-pad navigation ends the guard so the player's own move stands");

        slots[(0, 0)] = dropped; container.currentFocusSlot = new() { x = 0, y = 0 };
        Drop();
        container.currentFocusSlot = new() { x = 0, y = 0 };
        Time.unscaledTime = 11;
        InventoryDropFocus.Guard(container);
        check(container.currentFocusSlot.x == 0, "The guard expires shortly after the drop");

        int focused = container.Focused.Count;
        slots[(0, 0)] = dropped; container.currentFocusSlot = new() { x = 0, y = 0 };
        Drop(removed: false);
        check(container.Focused.Count == focused, "A refused or returned drop keeps the game's focus");
        ItemContainersManager.Instance.cursor.Holding = true;
        Drop();
        check(container.Focused.Count == focused, "An item still held on the cursor keeps the game's focus");
        ItemContainersManager.Instance.cursor.Holding = false;

        slots.Clear(); slots[(0, 0)] = dropped; container.currentFocusSlot = new() { x = 0, y = 0 };
        container.NearestSlot = new() { x = 0, y = 0 };
        Drop();
        check(container.Focused.Count == focused && container.currentFocusSlot.x == 0,
            "Dropping the last item leaves the emptied slot focused");

        container.currentFocusSlot = new() { x = 5, y = 5 };
        InventoryDropFocus.BeforeDrop(container);
        InventoryDropFocus.AfterDrop(container);
        check(container.Focused.Count == focused, "Drop on an empty slot changes nothing");
        ItemContainersManager.Instance = null;
    }
}
