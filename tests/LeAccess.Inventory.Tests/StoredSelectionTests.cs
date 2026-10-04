using LeAccess.Accessibility;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Items;
using UnityEngine;

internal static class StoredSelectionTests
{
    internal static void Run(Action<bool, string> check)
    {
        var oldItem = new ItemDataUnpacked { Id = "old stored item", FullName = "Old stored sword" };
        var newItem = new ItemDataUnpacked { Id = "current stored item", FullName = "Current stored shield" };
        var retained = new ItemContainerEntry { data = oldItem, Position = new() { x = 3, y = 2 } };
        var stored = new ItemContainerEntry { data = newItem, Position = retained.Position };
        var container = new ItemContainerUI { currentlySelectedItem = retained,
            currentFocusSlot = new() { x = 1, y = 1 } };
        NavigationManager.currentlySelected = container;
        UIBase.instance = null;
        foreach (var kind in new[] { ContainerType.STASH, ContainerType.SHOP })
        {
            container.container = new() { Type = kind, Entry = stored };
            InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
            check(!InventorySelectionReader.TryGetEntry(container, out _) &&
                !InventorySelectionReader.OwnsSelection(oldItem, container),
                "Retained hover data cannot identify a different item in the active " + kind + " container");
            InventoryAccessibility.Update(); InventoryAccessibility.Update();
            check(ScreenReader.Messages.Single() == LeAccess.I18n.ModText.Get("inventory.selection_unavailable"),
                "Retained " + kind + " item is reported unavailable instead of speaking a previous tab/stock item");
            container.container.Entry = null;
            check(!InventorySelectionReader.TryGetEntry(container, out _),
                "Removed " + kind + " item cannot remain selected through a stale hover entry");
            container.container.Entry = new() { data = oldItem, Position = retained.Position };
            check(InventorySelectionReader.TryGetEntry(container, out var current) && current == container.container.Entry,
                "Active " + kind + " entry replaces its matching retained UI wrapper");
            check(container.container.LastReadPosition.x == retained.Position.x &&
                container.container.LastReadPosition.y == retained.Position.y,
                "Stored-item validation uses actual item position independently of the controller grid focus coordinate: " + kind);
            container.container.Entry.WasCollected = true;
            check(!InventorySelectionReader.TryGetEntry(container, out _), "Collected stored entry is rejected: " + kind);
            container.container.Entry.WasCollected = false;
            container.container.Entry.data = new ItemData { Id = oldItem.Id, Unpacked = oldItem };
            check(InventorySelectionReader.TryGetEntry(container, out _) && InventorySelectionReader.OwnsSelection(oldItem, container),
                "Packed/unpacked native identity remains valid in " + kind);
            foreach (var missingId in new[] { null, "" })
            {
                var retainedId = oldItem.Id;
                oldItem.Id = missingId;
                container.container.Entry.data = new ItemData { Id = missingId };
                check(!InventorySelectionReader.TryGetEntry(container, out _) &&
                    !InventorySelectionReader.OwnsSelection(oldItem, container),
                    "Distinct items with missing serialized IDs cannot establish retained ownership: " + kind);
                container.container.Entry.data = oldItem;
                check(InventorySelectionReader.TryGetEntry(container, out _) &&
                    InventorySelectionReader.OwnsSelection(oldItem, container),
                    "Exact native pointer still establishes ownership while an ID is unavailable: " + kind);
                oldItem.Id = retainedId;
            }
            container.container.Entry.data = null;
            check(!InventorySelectionReader.TryGetEntry(container, out _), "Missing stored item data cannot establish ownership: " + kind);
            container.container.Entry = new() { data = oldItem, Position = retained.Position };
            ItemTooltipModelReader.Text = "Old stored sword. Properties";
            ItemTooltipModelReader.OnRead = () => container.container.Entry = stored;
            InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
            InventoryAccessibility.Update(readDetails: true);
            check(ScreenReader.Messages.Count == 0 && !InventoryAccessibility.HasPendingContent,
                "Changing current stored contents during manual inspection cancels retained output: " + kind);
            ItemTooltipModelReader.OnRead = null;
            ItemTooltipModelReader.Text = null;
        }
        container.container.WasCollected = true;
        check(!InventorySelectionReader.TryGetEntry(container, out _), "Collected current backing container rejects its retained selection");
        container.container = null;
        check(!InventorySelectionReader.TryGetEntry(container, out _), "Missing current backing container rejects its retained selection");
        ItemTooltipModelReader.Text = "Old stored sword. Stale properties";
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
        InventoryAccessibility.Update(readDetails: true);
        check(ScreenReader.Messages.Single() == LeAccess.I18n.ModText.Get("inventory.selection_unavailable"),
            "Manual details cannot read an old tooltip when the backing container is absent");
        ItemTooltipModelReader.Text = null;
        ItemTooltipModelReader.Reads = 0;
        ItemTooltipModelReader.LastItem = null;
        ItemTooltipModelReader.LastContainer = null;
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear(); NavigationManager.currentlySelected = null;
    }
}
