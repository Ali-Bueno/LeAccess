using System;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Inventory;

/// <summary>After a controller drop, focuses the nearest remaining item and keeps it there.</summary>
/// <remarks>See reference/inventory-drop-focus.md for the native drop path and the later reset.</remarks>
internal static class InventoryDropFocus
{
    // Long enough for the native hover refresh on the ticks after a drop; any d-pad move ends it sooner.
    private const float GuardSeconds = 0.5f;
    private static IntPtr _dropContainer;
    private static Vector2Int _dropSlot;
    private static IntPtr _guardContainer;
    private static Vector2Int _guardSlot;
    private static float _guardUntil;
    private static string _lastFailure;

    internal static void BeforeDrop(ItemContainerUI container)
    {
        _dropContainer = IntPtr.Zero;
        try
        {
            var items = IsAlive(container) ? container.Container : null;
            if (items is null || items.WasCollected) return;
            _dropSlot = container.currentFocusSlot;
            if (items.GetEntryAt(_dropSlot) is { WasCollected: false }) _dropContainer = container.Pointer;
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void AfterDrop(ItemContainerUI container)
    {
        try
        {
            if (_dropContainer == IntPtr.Zero || !IsAlive(container) || container.Pointer != _dropContainer) return;
            _dropContainer = IntPtr.Zero;
            var items = container.Container;
            var cursor = ItemContainersManager.Instance is { } manager && IsAlive(manager) ? manager.cursor : null;
            // A quick move already focused the nearest item; a refused drop returns the item or keeps it on the cursor.
            if (items is null || items.WasCollected || items.GetEntryAt(container.currentFocusSlot) is not null ||
                cursor is null || cursor.WasCollected || cursor.HasContent()) return;
            container.FocusNearestItem();
            var entry = items.GetEntryAt(container.currentFocusSlot);
            if (entry is null || entry.WasCollected) return;
            container.SetFocusItem(entry);
            container.forceUpdate = false;
            _guardContainer = container.Pointer;
            _guardSlot = container.currentFocusSlot;
            _guardUntil = Time.unscaledTime + GuardSeconds;
        }
        catch (Exception ex) { _guardContainer = IntPtr.Zero; ReportFailure(ex); }
    }

    internal static void CancelGuard() => _guardContainer = IntPtr.Zero;

    /// <summary>Restores the focused item if something other than navigation moved it shortly after the drop.</summary>
    internal static void Guard(ItemContainerUI container)
    {
        if (_guardContainer == IntPtr.Zero) return;
        try
        {
            if (!IsAlive(container) || container.Pointer != _guardContainer || Time.unscaledTime > _guardUntil)
            { _guardContainer = IntPtr.Zero; return; }
            var slot = container.currentFocusSlot;
            if (slot.x == _guardSlot.x && slot.y == _guardSlot.y) return;
            var items = container.Container;
            var entry = items is null || items.WasCollected ? null : items.GetEntryAt(_guardSlot);
            if (entry is null || entry.WasCollected) { _guardContainer = IntPtr.Zero; return; }
            Plugin.Log.LogInfo($"[Inventory drop] focus moved to {slot.x},{slot.y}; restored {_guardSlot.x},{_guardSlot.y}");
            container.currentFocusSlot = _guardSlot;
            container.SetFocusItem(entry);
        }
        catch (Exception ex) { _guardContainer = IntPtr.Zero; ReportFailure(ex); }
    }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Inventory drop focus unavailable: {ex.Message}");
    }
}
