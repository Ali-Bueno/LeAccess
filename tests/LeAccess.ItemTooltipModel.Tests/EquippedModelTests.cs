using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Items;
using LE.UI.Tooltips;

internal static class EquippedModelTests
{
    internal static void Run(Action<bool, string> check)
    {
        LeAccess.I18n.ModText.SetLanguage("en");
        UIBase.instance = null;
        UIBase.Shop = null;
        var item = new ItemDataUnpacked { LoreText = "A sturdy crown." };
        var helmet = new ItemContainerUI
        {
            identifier = ContainerID.EQ_HELMET,
            Container = new() { Type = ContainerType.EQUIPPED, Id = ContainerID.EQ_HELMET, Entry = new() { data = item } }
        };
        var replacement = new ItemDataUnpacked();
        var gloves = new ItemContainerUI
        {
            identifier = ContainerID.EQ_GLOVES,
            Container = new() { Type = ContainerType.EQUIPPED, Id = ContainerID.EQ_GLOVES, Entry = new() { data = replacement } }
        };
        var grid = new Il2CppSystem.Array { Cells = new Il2CppSystem.Object[2, 1] { { helmet }, { gloves } } };
        var equipment = new EquippedItemsNavigable { navigables = grid };
        NavigationManager.currentlySelected = equipment;
        ItemTooltipContentBuilder.Result = new() { Name = "Iron crown", PrimaryType = "Helmet", SecondaryType = "Rare" };
        ItemTooltipContentBuilder.Result.Implicits.Add(new() { Text = "+12 armor" });
        check(ItemTooltipModelReader.TryRead(item, helmet, out string text) && text == "Iron crown. Helmet. Rare. +12 armor. A sturdy crown.",
            "Source-linked native model builds actual equipped-slot details without a direct container navigation owner");
        check(ItemTooltipContentBuilder.Result.SlotType == TooltipItemManager.SlotType.EQUIPPED,
            "Equipment array selection builds the game's equipped tooltip slot");
        check(!ItemTooltipModelReader.TryRead(replacement, gloves, out _), "Non-selected equipped slot cannot build its details");
        helmet.currentlySelectedItem = new() { data = replacement };
        check(ItemTooltipModelReader.TryRead(item, helmet, out _) && !ItemTooltipModelReader.TryRead(replacement, helmet, out _),
            "Details ignore stale local hover data and require actual equipment source identity");
        var copy = new ItemDataUnpacked { Id = item.Id };
        check(ItemTooltipModelReader.OwnsSelection(copy, helmet), "Same item ID in a different native wrapper retains equipment ownership");
        ItemTooltipContentBuilder.BuildAction = () => helmet.Container.Entry = new() { data = replacement };
        check(!ItemTooltipModelReader.TryRead(item, helmet, out text) && text is null,
            "Equipment swapped during native model construction discards the old item's detail result");
        helmet.Container.Entry = new() { data = item };
        ItemTooltipContentBuilder.BuildAction = () => equipment.currentlySelectedIdx = new() { x = 1 };
        check(!ItemTooltipModelReader.TryRead(item, helmet, out _), "Moving between equipped slots during detail building rejects stale output");
        equipment.currentlySelectedIdx = default;
        ItemTooltipContentBuilder.BuildAction = () => equipment.gameObject.activeInHierarchy = false;
        check(!ItemTooltipModelReader.TryRead(item, helmet, out _), "Closing equipment while native details are built rejects them");
        equipment.gameObject.activeInHierarchy = true;
        ItemTooltipContentBuilder.BuildAction = null;
        UIBase.instance = new();
        UIBase.Shop = new();
        ItemTooltipContentBuilder.VendorResult = new() { IsForPurchase = false };
        ItemTooltipContentBuilder.VendorResult.Prices.Add(new() { Text = "10 gold" });
        check(MerchantItemReader.ReadContext(item, helmet) == LeAccess.I18n.ModText.Format("merchant.sell_price", ItemTooltipContentBuilder.VendorResult.Prices[0].Text), "Selected worn equipment retains verified merchant sale-price context");
        ItemTooltipContentBuilder.VendorAction = () => helmet.Container.Entry = null;
        check(MerchantItemReader.ReadContext(item, helmet) is null, "Removing equipment during merchant-price read rejects stale sale context");
        ItemTooltipContentBuilder.VendorAction = null;
        NavigationManager.currentlySelected = null;
        UIBase.instance = null; UIBase.Shop = null;
    }
}
