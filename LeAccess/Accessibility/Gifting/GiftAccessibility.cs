using System;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.ScreenContent;
using LE.UI.PanelSystem;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Gifting;

/// <summary>Speaks the item gifting panel: its text on opening, the selected stop and its state, and what Square
/// in the inventory beside it did.</summary>
/// <remarks>Polled like the forge: the panel keeps its stops in its own index. See
/// reference/social-reading.md#item-gifting-2026-10-07.</remarks>
internal static class GiftAccessibility
{
    private static readonly ScreenContentTracker Tracker = new();
    private static ItemGiftPopupNavigable _open;
    private static bool _introduced;
    private static bool _leadRaised;
    private static bool _away;
    private static string _key;
    private static ItemContainerUI _dropContainer;
    private static Vector2Int _dropSlot;
    private static IntPtr _dropItem;
    private static string _dropName;
    private static string _before;
    private static string _lastFailure;

    internal static bool HasActiveContent { get; private set; }

    /// <summary>Every frame, before the inventory reads: with the panel open but focus elsewhere (Cross on the empty
    /// slot goes to the inventory), the inventory's next brief says how to bring an item and come back.</summary>
    internal static void Observe()
    {
        try
        {
            bool open = OpenPanel() is not null;
            bool away = open && !GiftPanelReader.TryGet(out _);
            if (away == _away) return;
            _away = away;
            if (away)
            {
                InventoryAccessibility.Lead(GiftText.InventoryHint());
                _leadRaised = true;
                var navigation = NavigationManager.currentlySelected?.TryCast<Component>();
                Plugin.Log.LogInfo($"[Gift] panel open, focus on {(IsAlive(navigation) ? navigation.gameObject.name : "nothing")}");
            }
            // Notices raised for the inventory are stale once the panel has focus again.
            else if (_leadRaised) { InventoryAccessibility.Lead(null); _leadRaised = false; }
        }
        catch (Exception ex) { _away = false; ReportFailure(ex); }
    }

    /// <summary>The generic announcer stays silent inside the panel while its navigable owns focus.</summary>
    internal static bool TryHandleFocus(GameObject element)
    {
        try { return IsAlive(element) && GiftPanelReader.TryGet(out var panel) && element.transform.IsChildOf(panel.Navigable.transform); }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    internal static void Update(bool readDetails = false)
    {
        try
        {
            TrackOpenPanel();
            HasActiveContent = false;
            if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible || ScreenContentAnnouncer.HasActiveContent ||
                DialogueAnnouncer.HasActiveContent || ControllerMenuAccessibility.HasActiveContent ||
                !GiftPanelReader.TryGet(out var panel) || !GiftPanelReader.TryReadStop(panel, out var stop))
            { Tracker.Reset(); _key = null; return; }
            HasActiveContent = true;
            _open = panel.Navigable;
            if (_key != stop.Key) { Tracker.Reset(); _key = stop.Key; }
            string brief = _introduced ? stop.Text :
                ScreenContentTracker.Combine(new[] { GiftPanelReader.Intro(panel), GiftText.Hint(), stop.Text });
            Tracker.Observe(1, brief, ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame, out string announcement,
                acknowledge: false);
            if (readDetails) announcement = ScreenContentTracker.Combine(new[] { GiftPanelReader.Intro(panel), stop.Text });
            if (string.IsNullOrWhiteSpace(announcement) || !ScreenReader.TrySpeak(announcement, interrupt: true)) return;
            Tracker.Acknowledge(1, brief);
            _introduced = true;
            Plugin.Log.LogInfo($"[{(readDetails ? "Gift details" : "Gift")}] {announcement}");
        }
        catch (Exception ex) { Tracker.Reset(); _key = null; ReportFailure(ex); }
    }

    /// <summary>Before a controller drop or quick move in an inventory container while the gifting panel is open.</summary>
    internal static void BeforeDrop(ItemContainerUI container)
    {
        _dropItem = IntPtr.Zero;
        try
        {
            var panel = OpenPanel();
            if (panel is null || !IsAlive(container) || Same(container, panel._giftItemContainer)) return;
            var items = container.Container;
            var entry = items is null || items.WasCollected ? null : items.GetEntryAt(container.currentFocusSlot);
            var data = entry is null || entry.WasCollected ? null : entry.data;
            if (data is null || data.WasCollected) return;
            _dropContainer = container;
            _dropSlot = container.currentFocusSlot;
            _dropItem = data.Pointer;
            _dropName = Forge.ForgeCellReader.NameOf(data);
            _before = GiftPanelReader.SlotItem(panel);
        }
        catch (Exception ex) { _dropItem = IntPtr.Zero; ReportFailure(ex); }
    }

    /// <summary>After it: a changed gift slot received the item; an unchanged slot with the item still in place and
    /// nothing on the cursor means the game refused the move without a word.</summary>
    internal static void AfterDrop(ItemContainerUI container)
    {
        if (_dropItem == IntPtr.Zero) return;
        var item = _dropItem;
        _dropItem = IntPtr.Zero;
        try
        {
            var panel = OpenPanel();
            if (panel is null || !IsAlive(container) || !IsAlive(_dropContainer) || container.Pointer != _dropContainer.Pointer) return;
            string after = GiftPanelReader.SlotItem(panel);
            if (after != null && after != _before)
            {
                InventoryAccessibility.Lead(GiftText.MovedIn(after));
                _leadRaised = true;
                Plugin.Log.LogInfo($"[Gift] moved in: {after}");
                return;
            }
            var items = container.Container;
            var entry = items is null || items.WasCollected ? null : items.GetEntryAt(_dropSlot);
            var cursor = ItemContainersManager.Instance is { } manager && IsAlive(manager) ? manager.cursor : null;
            if (entry is null || entry.WasCollected || entry.data is null || entry.data.WasCollected || entry.data.Pointer != item ||
                cursor is null || cursor.WasCollected || cursor.HasContent()) return;
            ScreenReader.Speak(GiftText.MoveRefused(), true);
            Plugin.Log.LogInfo($"[Gift] not moved: {_dropName}");
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void Reset()
    {
        Tracker.Reset();
        _key = null;
        _open = null;
        _introduced = false;
        _leadRaised = false;
        _away = false;
        _dropItem = IntPtr.Zero;
        _dropContainer = null;
        HasActiveContent = false;
    }

    // Closing the panel makes the next opening introduce it again.
    private static void TrackOpenPanel()
    {
        if (_open is null) return;
        if (IsAlive(_open) && _open.isActiveAndEnabled && _open.gameObject.activeInHierarchy) return;
        _open = null;
        _introduced = false;
    }

    // The game opens the inventory beside the panel, which keeps the panel lookup off ordinary frames.
    private static ItemGiftingPanel OpenPanel()
    {
        var ui = UIBase.instance;
        var panels = IsAlive(ui) && ui.IsInventoryOpen() ? ui.PanelSystem : null;
        var panel = IsAlive(panels) ? panels.GetPanelIfOpen<ItemGiftingPanel>() : null;
        return IsAlive(panel) ? panel : null;
    }

    private static bool Same(ItemContainerUI a, ItemContainerUI b) => IsAlive(b) && a.Pointer == b.Pointer;

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Gift feedback unavailable: {ex}");
    }
}
