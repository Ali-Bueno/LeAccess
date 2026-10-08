using LeAccess.Accessibility.Panels;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using LE.Data;
using LE.UI.Bazaar;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Market;

/// <summary>Reads the player's own stall: listed / sold tabs, the sold-items indicator, back and clear buttons, and own listings' state.</summary>
/// <remarks>Listing state comes from <c>ItemListingData.State</c>. See reference/faction-vendor-panels.md#bazaar-market.</remarks>
internal static class BazaarStallReader
{
    internal static bool TryRead(GameObject element, BazaarUI ui, out string text)
    {
        text = null;
        bool sold = ui.CurrentUIState == BazaarUI.UI_STATE.MY_STALL_SOLD;
        if (Inside(ui.listedButton, element)) text = PanelText.Tab(Caption(ui.listedButton), ui.CurrentUIState == BazaarUI.UI_STATE.MY_STALL, 1, 2);
        else if (Inside(ui.soldButton, element)) text = PanelText.Tab(Caption(ui.soldButton), sold, 2, 2);
        else if (IsAlive(ui.soldItemsIndicator) && Inside(ui.soldItemsIndicator.showStallButton, element))
            text = ScreenContentTracker.Combine(new[] { MarketText.SoldWaiting(Visible(ui.soldItemsIndicator.soldAmountText)),
                Caption(ui.soldItemsIndicator.showStallButton) ?? ModText.Get("market.view_stall") });
        else if (IsAlive(ui.backButtonHolder) && element.transform.IsChildOf(ui.backButtonHolder.transform))
            text = TextUtils.GetTextFromHierarchy(element) ?? ModText.Get("market.stall_back");
        else if (Inside(ui.clearButton, element)) text = Caption(ui.clearButton) ?? ModText.Get("market.clear_filters");
        return text != null;
    }

    /// <summary>State and action of a listing in the player's own stall; both null for other sellers' listings.</summary>
    internal static void OwnListing(ListingUI listing, out string state, out string action)
    {
        state = action = null;
        var data = listing.listingData;
        if (data is not { WasCollected: false } || !data.IsOwnedByRequestingUser) return;
        state = MarketText.ListingStatus(data.State switch
        {
            BazaarItemState.Sold => ListingState.Sold,
            BazaarItemState.Redeemed => ListingState.Redeemed,
            BazaarItemState.Cancelled => ListingState.Cancelled,
            _ => ListingState.Active
        });
        if (IsAlive(listing.button) && listing.button.gameObject.activeInHierarchy) action = MarketText.ListingAction(Visible(listing.buttonText));
    }

    private static string Caption(Component button) => IsAlive(button) ? TextUtils.GetTextFromHierarchy(button.gameObject) : null;

    private static bool Inside(Component root, GameObject element) => IsAlive(root) && element.transform.IsChildOf(root.transform);

    private static string Visible(TMP_Text label) =>
        TextUtils.IsVisibleLabel(label) && !string.IsNullOrWhiteSpace(label.text) ? TextUtils.CleanText(label.text) : null;
}
