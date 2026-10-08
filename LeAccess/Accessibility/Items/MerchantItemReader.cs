using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using LE.UI.PanelSystem;
using LE.UI.Tooltips;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Items;

internal static class MerchantItemReader
{
    internal static string ReadContext(ItemData item, ItemContainerUI container)
    {
        try
        {
            if (!ItemTooltipModelReader.OwnsSelection(item, container) ||
                !TryGetContext(container, out var slot, out var priceKey, out bool purchase)) return null;
            var unpacked = item.getAsUnpacked();
            if (unpacked is null || unpacked.WasCollected ||
                unpacked.Pointer != item.Pointer && !unpacked.IdEquals(item)) return null;
            var vendor = ItemTooltipContentBuilder.GetVendorInfo(unpacked, slot);
            if (vendor is null || vendor.WasCollected || vendor.IsForPurchase != purchase ||
                vendor.Prices is null || vendor.Prices.WasCollected) return null;
            var prices = new List<string>();
            bool cannotAfford = false;
            foreach (var price in vendor.Prices)
            {
                if (price is null || price.WasCollected) continue;
                string text = TextUtils.CleanText(price.ToSearchableString());
                if (string.IsNullOrWhiteSpace(text)) continue;
                prices.Add(text);
                cannotAfford |= purchase && !price.CanAfford;
            }
            if (prices.Count == 0 || !ItemTooltipModelReader.OwnsSelection(item, container) ||
                !TryGetContext(container, out var currentSlot, out var currentKey, out _) ||
                currentSlot != slot || currentKey != priceKey) return null;
            return ScreenContentTracker.Combine(new[]
            {
                ModText.Format(priceKey, ScreenContentTracker.Combine(prices)),
                cannotAfford ? ModText.Get("merchant.cannot_afford") : null
            });
        }
        catch (Exception) { return null; }
    }

    internal static bool TryGetShopSlot(ItemContainerUI container, out TooltipItemManager.SlotType slot) =>
        TryGetContext(container, out slot, out _, out bool purchase) && purchase;

    private static bool TryGetContext(ItemContainerUI container, out TooltipItemManager.SlotType slot,
        out string priceKey, out bool purchase)
    {
        slot = TooltipItemManager.SlotType.UNDEFINED;
        priceKey = null;
        purchase = false;
        if (Panels.VendorReader.TryPrice(container, out slot, out priceKey)) { purchase = true; return true; }
        var ui = UIBase.instance;
        if (!IsAlive(ui) || !IsAlive(container)) return false;
        var panel = UIBase.GetPanelIfOpen<ShopPanel>();
        if (!IsAlive(panel) || !panel.isActiveAndEnabled || !UiVisibility.IsVisible(panel.gameObject)) return false;
        var source = container.Container;
        if (source is null || source.WasCollected) return false;
        if (source.Type == ContainerType.SHOP)
        {
            bool buyback = IsAlive(panel._buyback) && panel._buyback.Pointer == container.Pointer;
            bool shop = IsAlive(panel._shop) && panel._shop.Pointer == container.Pointer;
            if (!buyback && !shop) return false;
            slot = TooltipItemManager.SlotType.SHOP;
            purchase = true;
            priceKey = buyback ? "merchant.buyback_price" : "merchant.buy_price";
            return true;
        }
        if (source.Type == ContainerType.INVENTORY) slot = TooltipItemManager.SlotType.INVENTORY;
        else if (source.Type == ContainerType.EQUIPPED) slot = TooltipItemManager.SlotType.EQUIPPED;
        else return false;
        priceKey = "merchant.sell_price";
        return true;
    }
}
