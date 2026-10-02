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

    internal static void Update()
    {
        try
        {
            var container = GetFocusedContainer();
            if (!IsAlive(container)) { Announcements.Reset(); return; }
            var entry = container.currentlySelectedItem;
            string key, text;
            if (entry is null || entry.WasCollected)
            {
                var slot = container.currentFocusSlot;
                key = $"{container.GetInstanceID()}:empty:{slot.x}:{slot.y}";
                text = ModText.Get("inventory.empty_slot");
            }
            else
            {
                var data = entry.data;
                var itemPointer = data is not null && !data.WasCollected ? data.Pointer : IntPtr.Zero;
                key = $"{container.GetInstanceID()}:item:{entry.Pointer}:{itemPointer}";
                text = null;
                if (data is not null && !data.WasCollected &&
                    !ItemTooltipReader.TryReadForItem(data, container.gameObject, out text))
                {
                    // A stalled tooltip must not withhold the selected item's own localized identity.
                    var unpacked = data.getAsUnpacked();
                    if (unpacked is not null && !unpacked.WasCollected)
                        text = ScreenContentTracker.Combine(new[]
                        {
                            TextUtils.CleanText(unpacked.FullName), TextUtils.CleanText(unpacked.LocalisedBaseTypeName)
                        });
                }
            }
            if (!Announcements.Observe(key, text, ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame,
                out string announcement)) return;
            ScreenReader.Speak(announcement, interrupt: true);
            Plugin.Log.LogInfo($"[Inventory] {announcement}");
        }
        catch (Exception ex) { Announcements.Reset(); ReportFailure(ex); }
    }

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

    internal static void Reset() { Announcements.Reset(); _lastFailure = null; }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Inventory feedback unavailable: {ex.Message}");
    }
}
