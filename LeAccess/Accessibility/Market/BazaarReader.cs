using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using LE.UI.Bazaar;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Market;

/// <summary>Reads the Bazaar's listing cards and pagination buttons, and says what a search returned.</summary>
/// <remarks>Online data; missing fields read nothing. See reference/faction-vendor-panels.md#bazaar-market.</remarks>
internal static class BazaarReader
{
    private static string _drawn;
    private static string _lastFailure;

    internal static bool TryRead(GameObject element, bool details, out string text)
    {
        text = null;
        try
        {
            if (!IsAlive(element)) return false;
            var listing = element.GetComponentInParent<ListingUI>();
            if (IsAlive(listing)) { text = Listing(listing, details); return text != null; }
            var button = element.GetComponentInParent<PaginationButton>();
            if (IsAlive(button)) { text = Page(button); return text != null; }
            var ui = element.GetComponentInParent<BazaarUI>();
            return IsAlive(ui) && (BazaarStallReader.TryRead(element, ui, out text) || BazaarListReader.TryRead(element, ui, out text) ||
                BazaarControlReader.TryRead(element, ui, out text));
        }
        catch (Exception ex) { Report(ex); return false; }
    }

    /// <summary>Once per frame: says what a search returned when a new page of listings has been drawn.</summary>
    internal static void Poll()
    {
        if (!UiFocusOwnership.TryGetSelected(out var focused)) return;
        string text = ResultsNotice(focused, out string id);
        if (text != null && ScreenReader.TrySpeak(text, interrupt: false))
        {
            _drawn = id;
            Plugin.Log.LogInfo($"[Bazaar] {text}");
            return;
        }
        var ui = IsAlive(focused) ? focused.GetComponentInParent<BazaarUI>() : null;
        string listed = IsAlive(ui) ? BazaarListReader.Notice(ui) : null;
        if (listed != null && ScreenReader.TrySpeak(listed, interrupt: false)) Plugin.Log.LogInfo($"[Bazaar] {listed}");
    }

    private static string ResultsNotice(GameObject focused, out string id)
    {
        id = null;
        try
        {
            var ui = IsAlive(focused) ? focused.GetComponentInParent<BazaarUI>() : null;
            if (!IsAlive(ui)) return null;
            bool stall = ui.CurrentUIState != BazaarUI.UI_STATE.BROWSING;
            bool block = ui.CurrentListingsView == BazaarUI.LISTINGS_VIEW.BLOCK;
            bool loading = block ? Alive(ui.listingsBlockView)?.IsLoadingIndicatorActive == true : Alive(ui.listingsListView)?.IsLoadingIndicatorActive == true;
            if (loading) { _drawn = null; return null; }
            bool empty = block ? Alive(ui.listingsBlockView)?.IsNoResultPanelActive == true : Alive(ui.listingsListView)?.IsNoResultPanelActive == true;
            empty |= stall && (ui.IsEmptyStallPanelActive || IsAlive(ui.myStallNoResults) && ui.myStallNoResults.activeInHierarchy);
            var response = block ? Alive(ui.listingsBlockView)?.CurrentQueryItemsResponseDrawn : Alive(ui.listingsListView)?.CurrentQueryItemsResponseDrawn;
            id = empty ? "none" : response is { WasCollected: false } ? $"{response.Pointer}:{response.currentPage}:{response.totalListings}" : null;
            if (id == null) return null;
            id = $"{ui.CurrentUIState}:{id}";
            if (id == _drawn) return null;
            int total = empty ? 0 : response.totalListings, page = empty ? 0 : response.currentPage, pages = empty ? 0 : response.totalPages;
            return stall ? MarketText.StallView(ui.CurrentUIState == BazaarUI.UI_STATE.MY_STALL_SOLD, total, page, pages) :
                MarketText.Results(total, page, pages);
        }
        catch (Exception ex) { Report(ex); return null; }
    }

    internal static void Reset() { _drawn = null; BazaarListReader.Reset(); }

    private static string Listing(ListingUI listing, bool details)
    {
        string gold = IsAlive(listing.goldValueHolder) && listing.goldValueHolder.activeInHierarchy ? Visible(listing.goldValueText) : null;
        string favor = IsAlive(listing.favorValueHolder) && listing.favorValueHolder.activeInHierarchy ? Visible(listing.favorValueText) : null;
        Place(listing, out int position, out int count);
        BazaarStallReader.OwnListing(listing, out string state, out string action);
        var summary = new ListingSummary(Visible(listing.nameText), Visible(listing.typeText), Visible(listing.levelText), gold, favor,
            position, count, state, action);
        return summary.Name == null ? null : MarketText.Listing(summary, details ? Details(listing) : null);
    }

    // Implicits, affixes and potentials, in the order the card shows them.
    private static string Details(ListingUI listing)
    {
        var parts = new List<string>();
        AddAll(parts, listing.implicitText);
        AddAll(parts, listing.affixesText);
        parts.Add(MarketText.Potential("market.forging_potential", Shown(listing.forgingPotential, listing.forgingPotentialText)));
        parts.Add(MarketText.Potential("market.legendary_potential", Shown(listing.legendaryPotential, listing.legendaryPotentialText)));
        parts.Add(MarketText.Potential("market.weavers_will", Shown(listing.weaversWill, listing.weaversWillText)));
        if (IsAlive(listing.corruptedLabel) && listing.corruptedLabel.activeInHierarchy) parts.Add(MarketText.Corrupted());
        parts.Add(Visible(listing.listedAtText));
        return LeAccess.Accessibility.ScreenContent.ScreenContentTracker.Combine(parts);
    }

    private static void AddAll(List<string> parts, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<TextMeshProUGUI> labels)
    {
        if (labels is null || labels.WasCollected) return;
        foreach (var label in labels) parts.Add(Visible(label));
    }

    private static string Shown(GameObject holder, TMP_Text value) =>
        IsAlive(holder) && holder.activeInHierarchy ? Visible(value) : null;

    // Cards are siblings under the view's scroll content; only the drawn (active) ones count.
    private static void Place(ListingUI listing, out int position, out int count)
    {
        position = count = 0;
        var parent = listing.transform.parent;
        var all = IsAlive(parent) ? parent.GetComponentsInChildren<ListingUI>(false) : null;
        if (all == null || all.WasCollected) return;
        count = all.Length;
        for (int i = 0; i < count; i++)
            if (all[i].Pointer == listing.Pointer) position = i + 1;
    }

    private static string Page(PaginationButton button)
    {
        var controls = button.GetComponentInParent<PaginationControls>();
        if (IsAlive(controls) && Is(controls.previousButton, button)) return MarketText.PageStep(false);
        if (IsAlive(controls) && Is(controls.nextButton, button)) return MarketText.PageStep(true);
        return MarketText.Page(button.pageNumber, IsAlive(controls) ? LastPage(controls) : 0, button.IsButtonSelected);
    }

    private static int LastPage(PaginationControls controls)
    {
        var last = controls.lastPageButton;
        return IsAlive(last) && last.gameObject.activeInHierarchy ? last.pageNumber : 0;
    }

    private static bool Is(PaginationButton a, PaginationButton b) => IsAlive(a) && a.Pointer == b.Pointer;

    private static T Alive<T>(T value) where T : UnityEngine.Object => IsAlive(value) ? value : null;

    private static string Visible(TMP_Text label) =>
        TextUtils.IsVisibleLabel(label) && !string.IsNullOrWhiteSpace(label.text) ? TextUtils.CleanText(label.text) : null;

    private static void Report(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Bazaar reading unavailable: {ex.Message}");
    }
}
