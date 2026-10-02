using LeAccess.Accessibility.Items;
using LeAccess.Accessibility.Announcements;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Inventory;

internal static class InventoryPromptReader
{
    // These actions now belong to each item announcement, not the separate menu hint queue.
    internal static bool IsItemPrompt(MultiButtonsActionPrompt prompt) =>
        IsAlive(prompt) && IsAlive(prompt.GetComponentInParent<ItemTooltipPrompts>());

    internal static bool CanRead(MultiButtonsActionPrompt prompt)
    {
        if (!IsAlive(prompt)) return false;
        var owner = prompt.GetComponentInParent<ItemTooltipPrompts>();
        if (owner is null) return true;
        if (!IsAlive(owner) || !owner.isActiveAndEnabled ||
            !ItemTooltipReader.TryGetVisible(out var manager, out var tooltip)) return false;

        // Legacy prefabs enable every prompt before the tooltip's SetPrompts stage.
        var current = tooltip._itemTooltipPrompts;
        if (!IsAlive(current) || current.Pointer != owner.Pointer) return false;
        var selected = NavigationManager.currentlySelected;
        var container = selected is not null && !selected.WasCollected ? selected.TryCast<ItemContainerUI>() : null;
        if (!IsAlive(container) || !container.isActiveAndEnabled || !UiVisibility.IsVisible(container.gameObject)) return false;
        var entry = container.currentlySelectedItem;
        return entry is not null && !entry.WasCollected &&
            ItemTooltipReader.MatchesItemOwner(manager.activeParameters, entry.data, container.gameObject);
    }
}
