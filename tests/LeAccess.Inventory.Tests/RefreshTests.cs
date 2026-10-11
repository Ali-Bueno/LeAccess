using LeAccess.Accessibility;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Items;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Vitals;
using LeAccess.I18n;

// An online inventory sync rebuilds every entry and leaves the grid without a selection until the end of that frame
// (reference/inventory-drop-focus.md#refresh-after-an-online-sync-2026-10-09).
internal static class RefreshTests
{
    internal static void Run(Action<bool, string> check)
    {
        RunState(check);
        var savedUi = UIBase.instance;
        UIBase.instance = null;
        RunReader(check);
        RunDrop(check);
        RunGold(check);
        UIBase.instance = savedUi;
    }

    private static void RunState(Action<bool, string> check)
    {
        var state = new InventoryAnnouncementState();
        int said = 0;
        // One frame of the inventory reader: speech is acknowledged with the reader's count after it.
        string Frame(string selection, string text, string identity)
        {
            if (!state.Observe(selection, text, true, out var speech, acknowledge: false, identity, said)) return null;
            state.Acknowledge(selection, text, ++said);
            return speech;
        }
        string Settle(string selection, string text, string identity) =>
            Frame(selection, text, identity) ?? Frame(selection, text, identity);
        string Item(string name, int place, int count) =>
            ScreenContentTracker.Combine(new[] { name, ListPosition.Text(place, count) });
        string empty = ModText.Get("inventory.empty_slot");

        check(Settle("sword", Item("Sword", 2, 5), "Sword") == Item("Sword", 2, 5), "The focused item is read once");
        Frame("empty", empty, empty);
        check(Settle("sword", Item("Sword", 2, 5), "Sword") is null && state.Resumed && !state.HasPendingContent,
            "One unspoken frame without a selection never repeats the same item");
        Frame("empty", empty, empty);
        check(Settle("sword", Item("Sword", 2, 4), "Sword") is null, "A refresh that also changes only the count stays silent");
        state.Interrupt();
        check(Settle("sword", Item("Sword", 2, 4), "Sword") is null, "A grid that loses focus for a moment keeps the item silent");
        Frame("empty", empty, empty);
        said++;
        check(Settle("sword", Item("Sword", 2, 4), "Sword") == Item("Sword", 2, 4), "Anything said in between reads the item again");
        check(Settle("shield", Item("Shield", 3, 4), "Shield") == Item("Shield", 3, 4) &&
            Settle("sword", Item("Sword", 2, 4), "Sword") == Item("Sword", 2, 4), "Returning from another spoken item reads it");
        Frame("empty", empty, empty);
        check(Settle("sword", Item("Forged sword", 2, 4), "Forged sword") == Item("Forged sword", 2, 4),
            "A changed item after the gap is read");
        state.Reset();
        check(Settle("sword", Item("Forged sword", 2, 4), "Forged sword") == Item("Forged sword", 2, 4),
            "A deliberate repeat still speaks");
    }

    private static ItemContainerEntry Entry(string name, int x = 0) =>
        new() { data = new ItemDataUnpacked { Id = "refresh " + name, FullName = name }, Position = new() { x = x, y = 0 } };

    private static void Pump()
    {
        InventoryAccessibility.Update();
        InventoryAccessibility.Update();
    }

    // The sync: removals leave no selection for one update, then a new wrapper of the same item is focused.
    private static void Sync(ItemContainerUI container, string name)
    {
        container.currentlySelectedItem = null;
        InventoryAccessibility.Update();
        container.currentlySelectedItem = Entry(name);
        Pump();
    }

    private static void RunReader(Action<bool, string> check)
    {
        const string sword = "Refresh sword", shield = "Refresh shield", other = "Other control";
        var container = new ItemContainerUI { currentlySelectedItem = Entry(sword) };
        NavigationManager.currentlySelected = container;
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear(); LeAccess.Plugin.Log.Information.Clear();
        Pump();
        check(ScreenReader.Messages.Single() == sword, "The focused item is read");
        Sync(container, sword);
        check(ScreenReader.Messages.Count == 1 && LeAccess.Plugin.Log.Information.Any(line => line.StartsWith("[Inventory] same item")),
            "An online sync that rebuilds the same item never reads it again");

        ItemTooltipModelReader.Text = "Refresh sword details";
        InventoryAccessibility.Update(readDetails: true);
        check(ScreenReader.Messages.Last() == ItemTooltipModelReader.Text, "R3 reads the details");
        Sync(container, sword);
        check(ScreenReader.Messages.Count == 2, "A sync after R3 never cuts the details with the brief");
        ItemTooltipModelReader.Text = null;

        NavigationManager.currentlySelected = null;
        InventoryAccessibility.Update();
        NavigationManager.currentlySelected = container;
        Pump();
        check(ScreenReader.Messages.Count == 2, "A grid that loses focus for a moment keeps the item silent");

        NavigationManager.currentlySelected = null;
        InventoryAccessibility.Update();
        ScreenReader.TrySpeak(other, interrupt: true);
        NavigationManager.currentlySelected = container;
        Pump();
        check(ScreenReader.Messages.Last() == sword && ScreenReader.Messages.Count == 4,
            "Returning after other speech reads the item again");

        Sync(container, shield);
        check(ScreenReader.Messages.Last() == shield, "A different item after the sync is read");
        InventoryAccessibility.RepeatCurrent();
        Pump();
        check(ScreenReader.Messages.Count == 6 && ScreenReader.Messages.Last() == shield, "A deliberate repeat at a list end still speaks");
        NavigationManager.currentlySelected = null;
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
    }

    private static void RunDrop(Action<bool, string> check)
    {
        var dropped = Entry("Dropped helm");
        var next = Entry("Nearest ring", x: 2);
        var slots = new Dictionary<(int, int), ItemContainerEntry> { [(0, 0)] = dropped, [(2, 0)] = next };
        var container = new ItemContainerUI
        {
            container = new() { Type = ContainerType.INVENTORY, Id = ContainerID.INVENTORY, Slots = slots },
            currentFocusSlot = new() { x = 0, y = 0 }, NearestSlot = new() { x = 2, y = 0 }, currentlySelectedItem = dropped
        };
        string Item(string name, int place, int count) =>
            ScreenContentTracker.Combine(new[] { name, ListPosition.Text(place, count) });
        ItemContainersManager.Instance = new ItemContainersManager();
        NavigationManager.currentlySelected = container;
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
        Pump();
        check(ScreenReader.Messages.Single() == Item("Dropped helm", 1, 2), "The item to drop is read: " + ScreenReader.Messages.Last());
        InventoryDropFocus.BeforeDrop(container);
        slots.Remove((0, 0));
        InventoryDropFocus.AfterDrop(container);
        Pump();
        check(ScreenReader.Messages.Last() == Item("Nearest ring", 1, 1), "The deliberate move to the nearest item after a drop is read");
        container.currentlySelectedItem = null;
        InventoryAccessibility.Update();
        slots[(2, 0)] = next = Entry("Nearest ring", x: 2);
        container.SetFocusItem(next);
        Pump();
        check(ScreenReader.Messages.Count == 2, "The server's confirmation of the drop does not read the nearest item again");
        NavigationManager.currentlySelected = null; ItemContainersManager.Instance = null;
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
    }

    private static void RunGold(Action<bool, string> check)
    {
        const string sword = "Gold refresh sword", shield = "Gold refresh shield";
        var ui = new UIBase { InventoryOpen = true };
        UIBase.instance = ui; GoldReport.Amount = 700;
        var container = new ItemContainerUI { currentlySelectedItem = Entry(sword) };
        NavigationManager.currentlySelected = container;
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
        Pump();
        string led = ScreenContentTracker.Combine(new[] { ModText.Format("gold.amount", 700), sword });
        check(ScreenReader.Messages.Single() == led, "The first item after opening is led by the gold");
        Sync(container, sword);
        check(ScreenReader.Messages.Count == 1, "A sync gap keeps the gold lead and does not repeat the item");
        container.currentlySelectedItem = Entry(shield);
        Pump();
        container.currentlySelectedItem = Entry(sword);
        Pump();
        check(ScreenReader.Messages.Last() == sword && ScreenReader.Messages[1] == shield,
            "The gold lead ends once another item is spoken");
        UIBase.instance = null; GoldReport.Amount = null; NavigationManager.currentlySelected = null;
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
    }
}
