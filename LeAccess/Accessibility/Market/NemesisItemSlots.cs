using LeAccess.Accessibility.Endgame;
using LE.UI.PanelSystem;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Market;

/// <summary>Reads the item slots of the Nemesis panel, whose holders speak only their object name.</summary>
/// <remarks>See reference/endgame-panels.md#nemesis-item-slots-2026-10-26.</remarks>
internal static class NemesisItemSlots
{
    internal readonly record struct Slot(GameObject Root, ItemContainerUI Container, NemesisItemContainer Source, int Position, int Total);

    /// <summary>True for an item container shown in a Nemesis item holder, so the inventory reader leaves it alone.</summary>
    internal static bool Owns(ItemContainerUI container)
    {
        if (!IsAlive(container)) return false;
        var panel = container.GetComponentInParent<NemesisPanel>();
        return IsAlive(panel) && TryFind(panel, container.gameObject, out _);
    }

    internal static bool TryFind(NemesisPanel panel, GameObject element, out Slot found)
    {
        found = default;
        var manager = IsAlive(panel) && IsAlive(element) ? panel._manager : null;
        var holders = IsAlive(manager) ? manager.itemHolders : null;
        if (holders is null || holders.WasCollected || IsSwapButton(manager, element)) return false;
        int position = 0, total = 0;
        NemesisPanelManager.NemesisItemHolderUI match = null;
        GameObject root = null;
        foreach (var holder in holders)
        {
            var shown = Shown(holder);
            if (shown is null) continue;
            total++;
            if (match is null && element.transform.IsChildOf(shown.transform)) { match = holder; root = shown; position = total; }
        }
        if (match is null) return false;
        var container = element.GetComponentInParent<ItemContainerUI>();
        if (!IsAlive(container) || !container.transform.IsChildOf(root.transform)) container = root.GetComponentInChildren<ItemContainerUI>();
        found = new Slot(root, IsAlive(container) ? container : null, Source(manager, match.containerID), position, total);
        return true;
    }

    internal static EndgameStop Read(Slot slot)
    {
        // The container the holder shows; the panel's own nemesis containers if the holder has none.
        var entry = ItemSlotReader.Entry(slot.Container) ??
            (slot.Source is { WasCollected: false } source ? source.GetEntryAt(Vector2Int.zero) : null);
        var data = ItemSlotReader.Data(entry);
        string item = ItemSlotReader.Identity(entry);
        string text = EndgameText.RewardSlot(item, default, slot.Position, slot.Total);
        var root = slot.Root;
        return new EndgameStop($"{root.GetInstanceID()}:{item}", text, () => ItemSlotReader.Details(data, root));
    }

    // Each holder shows one of its layouts: a plain item, a unique that can be swapped, or the one swapped in.
    private static GameObject Shown(NemesisPanelManager.NemesisItemHolderUI holder)
    {
        if (holder is null || holder.WasCollected) return null;
        foreach (var root in new[] { holder.nonSwappableHolder, holder.swappableHolder, holder.swappedItemHolder })
            if (IsAlive(root) && root.activeInHierarchy) return root;
        return null;
    }

    // Swap buttons sit beside the swappable unique and keep their own caption.
    private static bool IsSwapButton(NemesisPanelManager manager, GameObject element)
    {
        var buttons = manager.swapButtons;
        if (buttons is null || buttons.WasCollected) return false;
        foreach (var button in buttons)
            if (IsAlive(button) && element.transform.IsChildOf(button.transform)) return true;
        return false;
    }

    private static NemesisItemContainer Source(NemesisPanelManager manager, ContainerID id)
    {
        var data = manager.containers;
        var containers = data is null || data.WasCollected ? null : data.Containers;
        if (containers is null || containers.WasCollected) return null;
        foreach (var container in containers)
            if (container is not null && !container.WasCollected && container.GetContainerID() == id) return container;
        return null;
    }
}
