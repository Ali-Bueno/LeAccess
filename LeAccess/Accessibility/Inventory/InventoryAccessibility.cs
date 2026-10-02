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
    private static string _lastFailure;
    private static string _contextKey, _context;
    private static float _nextContextRead;

    internal static bool HasPendingContent => HasActiveContent && Announcements.HasPendingContent;

    internal static bool HasActiveContent
    {
        get { try { return IsAlive(GetFocusedContainer()); } catch (Exception) { return false; } }
    }

    internal static bool TryHandleFocus(GameObject element)
    {
        try
        {
            var container = GetFocusedContainer();
            return IsAlive(container) && IsAlive(element) && IsAlive(element.transform) &&
                IsAlive(container.transform) && element.transform.IsChildOf(container.transform);
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    internal static void Update(bool readDetails = false)
    {
        try
        {
            var container = GetFocusedContainer();
            if (!IsAlive(container)) { Reset(); return; }
            var entry = container.currentlySelectedItem;
            string key, text;
            ItemData selectedData = null;
            bool hasTooltip = false;
            if (entry is null || entry.WasCollected)
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
                    if (unpacked is not null && !unpacked.WasCollected)
                        text = ScreenContentTracker.Combine(new[]
                        {
                            TextUtils.CleanText(unpacked.FullName), TextUtils.CleanText(unpacked.LocalisedBaseTypeName),
                            IsEquipmentSlot(container.identifier) ? ModText.Get("inventory.equipped") : null
                        });
                }
            }
            if (selectedData is not null && !selectedData.WasCollected)
            {
                if (_contextKey != key || readDetails || Time.unscaledTime >= _nextContextRead)
                {
                    _contextKey = key;
                    _nextContextRead = Time.unscaledTime + 0.2f;
                    _context = ScreenContentTracker.Combine(new[]
                    {
                        MerchantItemReader.ReadContext(selectedData, container),
                        ScreenContentTracker.Combine(InventoryActionReader.ReadForItem(selectedData, container))
                    });
                }
                text = ScreenContentTracker.Combine(new[] { text, _context });
            }
            else { _contextKey = _context = null; }
            string requestedText = text;
            if (readDetails && selectedData is not null && !selectedData.WasCollected)
                hasTooltip = ItemTooltipReader.TryReadForItem(selectedData, container.gameObject, out requestedText);
            if (readDetails && !hasTooltip && selectedData is not null && !selectedData.WasCollected)
            {
                if (!ItemTooltipModelReader.TryRead(selectedData, container, out requestedText))
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
            bool announced = Announcements.Observe(key, text,
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame, out string announcement);
            if (readDetails && ScreenReader.IsAvailable() && !string.IsNullOrWhiteSpace(requestedText))
            {
                Announcements.Observe(key, text, true, out _);
                announcement = requestedText;
            }
            else if (!announced) return;
            ScreenReader.Speak(announcement, interrupt: true);
            Plugin.Log.LogInfo($"[{(readDetails ? "Inventory details" : "Inventory")}] {announcement}");
        }
        catch (Exception ex) { Announcements.Reset(); ReportFailure(ex); }
    }

    private static bool IsEquipmentSlot(ContainerID id) => id is
        ContainerID.EQ_HELMET or ContainerID.EQ_ARMOR or ContainerID.EQ_WEAPON or ContainerID.EQ_OFFHAND or
        ContainerID.EQ_GLOVES or ContainerID.EQ_BELT or ContainerID.EQ_BOOTS or ContainerID.EQ_RIGHTRING or
        ContainerID.EQ_LEFTRING or ContainerID.EQ_AMULET or ContainerID.EQ_RELIC;

    private static ItemContainerUI GetFocusedContainer()
    {
        if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible ||
            ScreenContentAnnouncer.HasActiveContent || DialogueAnnouncer.HasActiveContent ||
            ControllerMenuAccessibility.HasActiveContent) return null;
        var selected = NavigationManager.currentlySelected;
        if (selected is null || selected.WasCollected) return null;
        var container = selected.TryCast<ItemContainerUI>();
        return IsAlive(container) && container.isActiveAndEnabled && UiVisibility.IsVisible(container.gameObject)
            ? container : null;
    }

    internal static void Reset()
    {
        Announcements.Reset(); _lastFailure = null;
        _contextKey = _context = null; _nextContextRead = 0;
    }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Inventory feedback unavailable: {ex.Message}");
    }
}
