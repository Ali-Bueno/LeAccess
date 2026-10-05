using LeAccess.Accessibility;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Vitals;
using LeAccess.I18n;

internal static class GoldNoticeTests
{
    internal static void Run(Action<bool, string> check)
    {
        string Gold(int amount) => ModText.Format("gold.amount", amount);
        var notice = new InventoryGoldNotice();
        notice.Observe(false, false, null);
        check(notice.For("a") is null, "Nothing is said while no inventory or shop is open");
        notice.Observe(true, false, 500);
        check(notice.For("a") == Gold(500), "Opening the inventory raises the gold");
        notice.Observe(true, false, 500);
        check(notice.For("b") == Gold(500), "Until spoken, the notice follows the selection as focus settles");
        notice.Spoken("b");
        check(notice.For("b") == Gold(500) && notice.For("c") is null, "Once spoken it stays on that selection and ends when it moves");
        notice.Observe(true, false, 450);
        check(notice.For("c") is null, "Outside a shop a gold change stays silent");
        notice.Observe(true, true, 450);
        check(notice.For("c") == Gold(450), "A shop opening over the open inventory raises the gold");
        notice.Spoken("c"); notice.For("d");
        notice.Observe(true, true, 300);
        check(notice.For("d") == Gold(300), "A purchase or sale in the shop raises the new gold");
        notice.Observe(false, false, null); notice.Observe(true, false, null);
        check(notice.For("e") is null, "Without readable gold nothing is added");

        // Through the reader: the first item after opening is led by the gold, later items are not.
        var ui = new UIBase { InventoryOpen = true };
        UIBase.instance = ui; GoldReport.Amount = 1200;
        var item = new ItemDataUnpacked { Id = "gold-item", FullName = "Gold sword" };
        var container = new ItemContainerUI { currentlySelectedItem = new() { data = item } };
        NavigationManager.currentlySelected = container;
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Single() == ScreenContentJoin(Gold(1200), "Gold sword"),
            "Opening the inventory reads the gold before the first item: " + string.Join(" | ", ScreenReader.Messages));
        var other = new ItemDataUnpacked { Id = "gold-other", FullName = "Plain shield" };
        container.currentlySelectedItem = new() { data = other };
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Last() == "Plain shield", "The next item is read alone");
        ui.ShopOpen = true; GoldReport.Amount = 900;
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Last() == ScreenContentJoin(Gold(900), "Plain shield"),
            "A shop opening (or a purchase) repeats the item with the current gold");
        ui.ShopOpen = false; GoldReport.Amount = 900;
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        ui.GamblingOpen = true; GoldReport.Amount = 600;
        InventoryAccessibility.Update(); InventoryAccessibility.Update();
        check(ScreenReader.Messages.Last() == ScreenContentJoin(Gold(600), "Plain shield"),
            "Other vendors, such as the gambler, are treated like the shop");
        ui.GamblingOpen = ui.InventoryOpen = false; UIBase.instance = null; GoldReport.Amount = null;
        NavigationManager.currentlySelected = null;
        InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
    }

    private static string ScreenContentJoin(params string[] parts) =>
        LeAccess.Accessibility.ScreenContent.ScreenContentTracker.Combine(parts);
}
