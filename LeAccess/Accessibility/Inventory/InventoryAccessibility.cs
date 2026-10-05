using System;
using LeAccess.I18n;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Items;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Inventory;

internal static class InventoryAccessibility
{
    private static readonly InventoryAnnouncementState Announcements = new();
    private static readonly ScreenContentTracker Comparison = new();
    private static readonly InventoryGoldNotice Gold = new();
    private static string _lastFailure;
    private static string _contextKey, _context, _position;
    private static float _nextContextRead;
    private static string _detailsKey;

    internal static bool HasPendingContent => HasActiveContent && (Announcements.HasPendingContent || _detailsKey != null);

    internal static bool HasActiveContent
    {
        get { try { return IsAlive(GetFocusedContainer()); } catch (Exception) { return false; } }
    }

    internal static bool TryHandleFocus(GameObject element)
    {
        try
        {
            var container = GetFocusedContainer();
            return IsAlive(container) && InventorySelectionReader.OwnsElement(container, element);
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    internal static void Update(bool readDetails = false)
    {
        try
        {
            ObserveGold();
            var container = GetFocusedContainer();
            if (!IsAlive(container)) { Reset(); return; }
            InventoryDropFocus.Guard(container);
            bool known = InventorySelectionReader.TryGetEntry(container, out var entry);
            string slotName = EquipmentSlotText.Read(container.identifier);
            string key, text;
            ItemData selectedData = null;
            bool hasTooltip = false;
            if (!known || entry is not null && (entry.data is null || entry.data.WasCollected))
            {
                key = $"{container.GetInstanceID()}:unavailable";
                text = ModText.Get("inventory.selection_unavailable");
            }
            else if (entry is null)
            {
                var slot = container.currentFocusSlot;
                key = $"{container.GetInstanceID()}:empty:{slot.x}:{slot.y}";
                text = ModText.Get("inventory.empty_slot");
            }
            else
            {
                var data = entry.data;
                selectedData = data;
                var itemPointer = data is not null && !data.WasCollected ? data.Pointer : IntPtr.Zero;
                key = $"{container.GetInstanceID()}:item:{entry.Pointer}:{itemPointer}";
                text = null;
                if (data is not null && !data.WasCollected)
                {
                    var unpacked = data.getAsUnpacked();
                    if (unpacked is not null && !unpacked.WasCollected &&
                        (unpacked.Pointer == data.Pointer || unpacked.IdEquals(data)))
                    {
                        string identity = ScreenContentTracker.Combine(new[]
                        {
                            TextUtils.CleanText(unpacked.FullName), TextUtils.CleanText(unpacked.LocalisedBaseTypeName)
                        });
                        if (!string.IsNullOrWhiteSpace(identity))
                            text = ScreenContentTracker.Combine(new[] { identity,
                                InventorySelectionReader.IsEquipmentSlot(container.identifier) ? ModText.Get("inventory.equipped") : null });
                    }
                }
                if (string.IsNullOrWhiteSpace(text)) text = ModText.Get("inventory.selection_unavailable");
            }
            text = ScreenContentTracker.Combine(new[] { slotName, text });
            if (_detailsKey != key) _detailsKey = null;
            if (readDetails) _detailsKey = key;
            bool requestDetails = _detailsKey == key && ScreenReader.IsAvailable() &&
                (readDetails || !ScreenReader.SpokeThisFrame);
            if (selectedData is not null && !selectedData.WasCollected)
            {
                if (_contextKey != key || requestDetails || Time.unscaledTime >= _nextContextRead)
                {
                    _contextKey = key;
                    _nextContextRead = Time.unscaledTime + 0.2f;
                    _context = ScreenContentTracker.Combine(new[]
                    {
                        MerchantItemReader.ReadContext(selectedData, container),
                        ScreenContentTracker.Combine(InventoryActionReader.ReadForItem(selectedData, container))
                    });
                    _position = InventoryNavigation.TryGetPosition(container, entry, out int place, out int count)
                        ? ModText.Format("list.position", place, count) : null;
                }
                text = ScreenContentTracker.Combine(new[] { text, _position, _context });
            }
            else { _contextKey = _context = _position = null; }
            string gold = Gold.For(key);
            if (gold != null) text = ScreenContentTracker.Combine(new[] { gold, text });
            string requestedText = text;
            if (requestDetails && selectedData is not null && !selectedData.WasCollected)
                hasTooltip = ItemTooltipReader.TryReadForItem(selectedData, container.gameObject, out requestedText);
            if (requestDetails && !hasTooltip && selectedData is not null && !selectedData.WasCollected)
            {
                hasTooltip = ItemTooltipModelReader.TryRead(selectedData, container, out requestedText);
                if (!hasTooltip)
                {
                    requestedText = ScreenContentTracker.Combine(new[] { text, ModText.Get("inventory.details_unavailable") });
                    string failure = ItemTooltipModelReader.LastFailure;
                    if (failure != null && failure != _lastFailure)
                    {
                        _lastFailure = failure;
                        Plugin.Log.LogWarning($"Manual inventory details unavailable: {failure}");
                    }
                }
            }
            var currentContainer = GetFocusedContainer();
            if (!IsAlive(currentContainer) || currentContainer.Pointer != container.Pointer ||
                selectedData is not null && !InventorySelectionReader.OwnsSelection(selectedData, container))
            { Reset(); return; }
            if (requestDetails && hasTooltip && slotName != null)
                requestedText = ScreenContentTracker.Combine(new[] { slotName, requestedText });
            if (!requestDetails && AnnounceComparison(selectedData, key, text)) return;
            bool announced = Announcements.Observe(key, text,
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame, out string announcement, acknowledge: false);
            if (requestDetails && !string.IsNullOrWhiteSpace(requestedText))
            {
                Announcements.Observe(key, text, false, out _, acknowledge: false);
                announcement = requestedText;
            }
            else if (!announced) return;
            if (!ScreenReader.TrySpeak(announcement, interrupt: true)) return;
            Announcements.Acknowledge(key, text);
            if (!requestDetails) Gold.Spoken(key);
            _detailsKey = null;
            Plugin.Log.LogInfo($"[{(requestDetails ? "Inventory details" : "Inventory")}] {announcement}");
        }
        catch (Exception ex) { Announcements.Reset(); _detailsKey = null; ReportFailure(ex); }
    }

    // The comparison exists only while the game's compare action is held; it speaks once each time it appears,
    // after the item's own brief when navigation reached a new item with the action still held.
    private static bool AnnounceComparison(ItemData item, string key, string brief)
    {
        if (item is null || item.WasCollected || !ItemComparisonReader.TryRead(item, out _, out string comparison))
        { Comparison.Reset(); return false; }
        if (!Comparison.Observe(1, comparison, ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame,
            out string announcement, acknowledge: false) || announcement is null) return true;
        if (!Announcements.HasSpoken(key, brief)) announcement = ScreenContentTracker.Combine(new[] { brief, announcement });
        if (!ScreenReader.TrySpeak(announcement, interrupt: true)) return true;
        Comparison.Acknowledge(1, comparison);
        Gold.Spoken(key);
        Announcements.Observe(key, brief, false, out _, acknowledge: false);
        Announcements.Acknowledge(key, brief);
        Plugin.Log.LogInfo($"[Inventory comparison] {announcement}");
        return true;
    }

    // Tracked every frame, so a panel closed while another window held focus still ends its notice.
    // A failure here only loses the gold notice, never the item reading.
    private static void ObserveGold()
    {
        try
        {
            var ui = UIBase.instance;
            // Vendors open with the inventory; gameplay frames cost two checks.
            bool open = IsAlive(ui) && (ui.IsInventoryOpen() || ui.IsShopOpen());
            bool shop = open && VendorOpen(ui);
            Gold.Observe(open, shop, open && Vitals.GoldReport.TryRead(out int amount) ? amount : null);
        }
        catch (Exception ex) { Gold.Reset(); ReportFailure(ex); }
    }

    // Every vendor the game trades through a gold-tracking container, plus the bazaar.
    private static bool VendorOpen(UIBase ui) =>
        ui.IsShopOpen() || ui.IsGamblingOpen() || ui.IsSoulGamblingOpen() || ui.IsDungeonKeysTraderPanelOpen() ||
        ui.IsFactionGamblingCoFOpen() || ui.IsFactionGamblingMGOpen() || ui.IsFactionTheWeaverVendorPanelOpen() ||
        ui.IsHunterVendorPanelOpen() || ui.IsTimeglassVendorPanelOpen() || ui.IsBazaarPanelOpen();

    private static ItemContainerUI GetFocusedContainer()
    {
        if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible ||
            ScreenContentAnnouncer.HasActiveContent || DialogueAnnouncer.HasActiveContent ||
            ControllerMenuAccessibility.HasActiveContent) return null;
        return InventorySelectionReader.GetFocusedContainer();
    }

    /// <summary>Speaks the current selection again, as when a move cannot go further.</summary>
    internal static void RepeatCurrent() => Announcements.Reset();

    internal static void Reset()
    {
        Announcements.Reset(); Comparison.Reset(); _lastFailure = null;
        _contextKey = _context = _position = null; _nextContextRead = 0;
        _detailsKey = null;
    }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Inventory feedback unavailable: {ex.Message}");
    }
}
