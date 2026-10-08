using System;

namespace LeAccess.Accessibility.Inventory;

/// <summary>Keeps item grids unhovered while the controller is active, as the game itself does on switching to it.</summary>
/// <remarks>A hidden mouse over a grid otherwise makes the game's hover refresh refocus the corner cell;
/// see reference/inventory-drop-focus.md#hidden-pointer-hover-2026-10-07.</remarks>
internal static class InventoryPointerHover
{
    private static string _lastFailure;
    private static IntPtr _lastLogged;

    /// <summary>The hover value the game's setter may store.</summary>
    internal static bool Filter(ItemContainerUI container, bool hovered)
    {
        try
        {
            if (!hovered || !EpochInputManager.IsControllerActive) return hovered;
            // One log line per grid: the setter can repeat while the hidden pointer rests on it.
            if (container is not null && !container.WasCollected && container.Pointer != _lastLogged)
            {
                _lastLogged = container.Pointer;
                Plugin.Log.LogInfo($"[Inventory hover] pointer hover ignored in controller mode: {container.identifier}");
            }
            return false;
        }
        catch (Exception ex) { ReportFailure(ex); return hovered; }
    }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Inventory pointer hover unavailable: {ex.Message}");
    }
}
