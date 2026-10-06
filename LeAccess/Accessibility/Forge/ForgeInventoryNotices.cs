using System;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using LE.UI.PanelSystem;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Forge;

/// <summary>What the inventory beside the forge says about it: the forge and its item when it opens (focus stays on
/// the inventory; L2 reaches the grid), and what Square did to the selected item.</summary>
/// <remarks>See reference/crafting-reading.md#forge-from-the-inventory-2026-10-05.</remarks>
internal static class ForgeInventoryNotices
{
    private static ForgePanel _panel;
    private static bool _open;
    private static ItemContainerUI _dropContainer;
    private static UnityEngine.Vector2Int _dropSlot;
    private static IntPtr _dropItem;
    private static string _dropName;
    private static ForgeContents _before;
    private static string _lastFailure;

    private readonly record struct ForgeContents(IntPtr Main, IntPtr Modifier, IntPtr Support);

    /// <summary>Every frame, before the inventory reads, so the introduction leads its first brief.</summary>
    internal static void Observe()
    {
        try
        {
            var forge = OpenForge();
            bool open = forge is not null;
            if (open == _open) return;
            _open = open;
            InventoryAccessibility.Lead(open ?ScreenContentTracker.Combine(new[] {
                ModText.Get("forge.opened"), ForgeCellReader.ItemSummary(forge), ModText.Get("forge.grid_hint") }) : null);
        }
        catch (Exception ex) { _open = false; ReportFailure(ex); }
    }

    /// <summary>Before a controller drop or quick move in an inventory container while the forge is open.</summary>
    internal static void BeforeDrop(ItemContainerUI container)
    {
        _dropItem = IntPtr.Zero;
        try
        {
            var forge = OpenForge();
            if (forge is null || !IsAlive(container) || IsForgeSlot(container)) return;
            var items = container.Container;
            var entry = items is null || items.WasCollected ? null : items.GetEntryAt(container.currentFocusSlot);
            var data = entry is null || entry.WasCollected ? null : entry.data;
            if (data is null || data.WasCollected) return;
            _dropContainer = container;
            _dropSlot = container.currentFocusSlot;
            _dropItem = data.Pointer;
            _dropName = ForgeCellReader.NameOf(data);
            _before = Contents(forge);
        }
        catch (Exception ex) { _dropItem = IntPtr.Zero; ReportFailure(ex); }
    }

    /// <summary>After it: a forge slot that changed received the item (Square); an unchanged forge with the item
    /// still in place and nothing on the cursor means the game refused the move without a word.</summary>
    internal static void AfterDrop(ItemContainerUI container)
    {
        if (_dropItem == IntPtr.Zero) return;
        var item = _dropItem;
        _dropItem = IntPtr.Zero;
        try
        {
            var forge = OpenForge();
            if (forge is null || !IsAlive(container) || !IsAlive(_dropContainer) || container.Pointer != _dropContainer.Pointer) return;
            var after = Contents(forge);
            if (after != _before)
            {
                string arrived = ForgeCellReader.NameOf(Arrived(forge, after)) ?? _dropName;
                if (arrived == null) return;
                InventoryAccessibility.Lead(ModText.Format("forge.moved_in", arrived));
                Plugin.Log.LogInfo($"[Forge] moved in: {arrived}");
                return;
            }
            var items = container.Container;
            var entry = items is null || items.WasCollected ? null : items.GetEntryAt(_dropSlot);
            var cursor = ItemContainersManager.Instance is { } manager && IsAlive(manager) ? manager.cursor : null;
            if (entry is null || entry.WasCollected || entry.data is null || entry.data.WasCollected || entry.data.Pointer != item ||
                cursor is null || cursor.WasCollected || cursor.HasContent()) return;
            string held = ForgeCellReader.NameOf(ForgeCellReader.Content(forge.main));
            string refusal = held == null ? ModText.Get("forge.move_refused") : ModText.Format("forge.move_refused_holding", held);
            ScreenReader.Speak(refusal, true);
            Plugin.Log.LogInfo($"[Forge] not moved: {_dropName}; {refusal}");
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void Reset()
    {
        _panel = null;
        _open = false;
        _dropItem = IntPtr.Zero;
        _dropContainer = null;
    }

    // Opening the forge always opens the inventory beside it, which keeps the panel lookup off ordinary frames.
    private static CraftingSlotManager OpenForge()
    {
        var ui = UIBase.instance;
        var panels = IsAlive(ui) && ui.IsInventoryOpen() ? ui.PanelSystem : null;
        _panel = IsAlive(panels) ? panels.GetPanelIfOpen<ForgePanel>() : null;
        var forge = IsAlive(_panel) ? _panel.CraftingSlotManager : null;
        return IsAlive(forge) ? forge : null;
    }

    private static bool IsForgeSlot(ItemContainerUI container) => IsAlive(_panel) &&
        (Same(container, _panel._main) || Same(container, _panel._modifier) || Same(container, _panel._support));

    private static bool Same(ItemContainerUI a, ItemContainerUI b) => IsAlive(b) && a.Pointer == b.Pointer;

    private static ForgeContents Contents(CraftingSlotManager forge) =>
        new(PointerOf(forge.main), PointerOf(forge.modifier), PointerOf(forge.support));

    private static IntPtr PointerOf(OneItemContainer slot) => ForgeCellReader.Content(slot) is { } data ? data.Pointer : IntPtr.Zero;

    private static ItemData Arrived(CraftingSlotManager forge, ForgeContents after) =>
        after.Main != _before.Main ? ForgeCellReader.Content(forge.main)
        : after.Modifier != _before.Modifier ? ForgeCellReader.Content(forge.modifier)
        : ForgeCellReader.Content(forge.support);

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Forge inventory notice unavailable: {ex}");
    }
}
