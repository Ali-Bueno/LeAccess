using LeAccess.Accessibility.Items;
using LeAccess.I18n;
using LE.UI.PanelSystem;
using LE.UI.Tooltips;

internal static class MerchantTests
{
    internal static void Run(Action<bool, string> check)
    {
        var item = new ItemDataUnpacked();
        var container = new ItemContainerUI
        {
            Container = new IItemContainer { Type = ContainerType.SHOP },
            currentlySelectedItem = new ItemContainerEntry { data = item }
        };
        var panel = new ShopPanel { _shop = container, _buyback = new ItemContainerUI() };
        var price = new ItemTooltipPriceInfo { Text = "250 oro" };
        var vendor = new ItemTooltipVendorInfo();
        vendor.Prices.Add(price);
        UIBase.instance = new UIBase();
        UIBase.Shop = panel;
        NavigationManager.currentlySelected = container;
        ItemTooltipContentBuilder.VendorResult = vendor;
        int modelCalls = ItemTooltipContentBuilder.Calls;
        string Price(string key, string text = "250 oro") => ModText.Format(key, text);
        check(MerchantItemReader.ReadContext(item, container) == Price("merchant.buy_price"),
            "Open shop uses native currency price and purchase context");
        check(ItemTooltipContentBuilder.VendorSlot == TooltipItemManager.SlotType.SHOP &&
            ItemTooltipContentBuilder.Calls == modelCalls, "Automatic merchant context builds only vendor information");
        price.CanAfford = false;
        check(MerchantItemReader.ReadContext(item, container).Contains(ModText.Get("merchant.cannot_afford")),
            "Native affordability state is announced");
        price.CanAfford = true;
        var secondPrice = new ItemTooltipPriceInfo { Text = "5 favores", CanAfford = false };
        vendor.Prices.Add(secondPrice);
        check(MerchantItemReader.ReadContext(item, container).Contains("250 oro. 5 favores") &&
            MerchantItemReader.ReadContext(item, container).Contains(ModText.Get("merchant.cannot_afford")),
            "Each native currency and any insufficient balance are preserved");
        vendor.Prices.Remove(secondPrice);
        var packed = new ItemData { Id = item.Id, Unpacked = item };
        container.currentlySelectedItem.data = packed;
        check(MerchantItemReader.ReadContext(packed, container) == Price("merchant.buy_price"), "Packed selected merchant item retains identity");
        packed.Unpacked = new ItemDataUnpacked();
        check(MerchantItemReader.ReadContext(packed, container) is null, "Wrong unpacked merchant item is rejected");
        container.currentlySelectedItem.data = item;

        foreach (string gate in new[] { "closed", "hidden panel", "disabled panel", "foreign shop", "empty slot", "foreign focus", "hidden container", "collected source" })
        {
            UIBase.Shop = gate == "closed" ? null : panel;
            panel.gameObject.activeInHierarchy = gate != "hidden panel";
            panel.isActiveAndEnabled = gate != "disabled panel";
            panel._shop = gate == "foreign shop" ? new ItemContainerUI() : container;
            container.currentlySelectedItem = gate == "empty slot" ? null : new ItemContainerEntry { data = item };
            NavigationManager.currentlySelected = gate == "foreign focus" ? new ItemContainerUI() : container;
            container.gameObject.activeInHierarchy = gate != "hidden container";
            container.Container.WasCollected = gate == "collected source";
            int calls = ItemTooltipContentBuilder.VendorCalls;
            check(MerchantItemReader.ReadContext(item, container) is null && ItemTooltipContentBuilder.VendorCalls == calls,
                "Merchant native price lookup blocked by " + gate);
        }
        container.Container.WasCollected = false;
        panel._shop = new ItemContainerUI();
        panel._buyback = container;
        check(MerchantItemReader.ReadContext(item, container) == Price("merchant.buyback_price"), "Buyback ownership changes context without guessing a tab index");
        check(ItemTooltipModelReader.TryGetParameters(item, container, out var slot, out var mode) &&
            slot == TooltipItemManager.SlotType.SHOP && mode == TooltipMode.DefaultInfo,
            "Owned shop and buyback get native SHOP tooltip slot without a visible tooltip");
        ItemTooltipContentBuilder.Result = new ItemTooltipInfo { Name = "Lanza", PrimaryType = "Raro", Description = "Daño de lanza" };
        check(ItemTooltipModelReader.TryRead(item, container, out string details) && details.Contains("Daño de lanza") &&
            details.Contains(Price("merchant.buyback_price")), "Manual merchant model reads properties and current price");
        ItemTooltipContentBuilder.Result.IsUnidentified = true;
        check(ItemTooltipModelReader.TryRead(item, container, out details) && !details.Contains("Daño de lanza") &&
            details.Contains("250 oro"), "Unidentified merchant item keeps price without revealing properties");
        ItemTooltipContentBuilder.Result.IsUnidentified = false;

        foreach (var type in new[] { ContainerType.INVENTORY, ContainerType.EQUIPPED })
        {
            container.Container.Type = type;
            vendor.IsForPurchase = false;
            check(MerchantItemReader.ReadContext(item, container) == Price("merchant.sell_price"), "Own " + type + " item reads sale price while shop is open");
        }
        vendor.IsForPurchase = true;
        check(MerchantItemReader.ReadContext(item, container) is null, "Unexpected purchase model cannot become a sale price");
        container.Container.Type = ContainerType.STASH;
        check(MerchantItemReader.ReadContext(item, container) is null, "Unrelated open stash cannot claim merchant sale context");
        container.Container.Type = ContainerType.SHOP;
        vendor.Prices.WasCollected = true;
        check(MerchantItemReader.ReadContext(item, container) is null, "Collected price list is rejected");
        vendor.Prices.WasCollected = false;
        price.WasCollected = true;
        check(MerchantItemReader.ReadContext(item, container) is null, "Collected or missing price cannot become an empty price claim");
        price.WasCollected = false;
        ItemTooltipContentBuilder.VendorAction = () => container.currentlySelectedItem = null;
        check(MerchantItemReader.ReadContext(item, container) is null, "Selection change during price lookup discards stale context");
        container.currentlySelectedItem = new ItemContainerEntry { data = item };
        ItemTooltipContentBuilder.VendorAction = () => UIBase.Shop = null;
        check(MerchantItemReader.ReadContext(item, container) is null, "Shop closing during price lookup discards stale context");
        UIBase.Shop = panel;
        ItemTooltipContentBuilder.VendorAction = () => throw new InvalidOperationException("Native vendor unavailable");
        check(MerchantItemReader.ReadContext(item, container) is null, "Native price failure returns no partial announcement");
        ItemTooltipContentBuilder.VendorAction = null;
        ModText.SetLanguage("es");
        check(MerchantItemReader.ReadContext(item, container) == Price("merchant.buyback_price"), "Merchant labels follow selected catalog language");
        ModText.SetLanguage("en");
        UIBase.Shop = null;
        check(!MerchantItemReader.TryGetShopSlot(container, out _), "Closed merchant no longer grants model slot fallback");
    }
}
