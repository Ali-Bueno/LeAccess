using LeAccess.Accessibility.Endgame;
using LeAccess.Accessibility.Items;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using LE.UI.PanelSystem;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Market;

/// <summary>Reads the item slots of the Rage of Morditas reward panel, whose containers speak only their object name.</summary>
/// <remarks>See reference/rage-of-morditas.md#reward-panel-slots-2026-10-21.</remarks>
internal static class MorditasRewardSlots
{
    internal readonly record struct Slot(RageOfMorditasRewardPanelRewardUI Ui, int Position, int Total);

    /// <summary>True for an item container that belongs to the open reward panel, so the inventory reader leaves it alone.</summary>
    internal static bool Owns(ItemContainerUI container)
    {
        if (!IsAlive(container)) return false;
        var slot = container.GetComponentInParent<RageOfMorditasRewardPanelRewardUI>();
        return IsAlive(slot);
    }

    internal static bool TryFind(RageOfMorditasRewardPanel panel, GameObject element, out Slot found)
    {
        found = default;
        var rewards = IsAlive(panel) && IsAlive(element) ? panel.ItemRewards : null;
        if (rewards is null || rewards.WasCollected) return false;
        int position = 0, total = 0;
        RageOfMorditasRewardPanelRewardUI match = null;
        foreach (var reward in rewards)
        {
            if (!IsAlive(reward) || !reward.gameObject.activeInHierarchy) continue;
            total++;
            if (match is null && element.transform.IsChildOf(reward.transform)) { match = reward; position = total; }
        }
        if (match is null) return false;
        found = new Slot(match, position, total);
        return true;
    }

    internal static EndgameStop Read(Slot slot)
    {
        var ui = slot.Ui;
        var container = ui.ItemContainerUI;
        var entry = IsAlive(container) ? Entry(container) : null;
        var data = entry is not null && !entry.WasCollected ? entry.data : null;
        string item = entry is null ? ModText.Get("endgame.slot_empty")
            : ItemIdentityReader.Read(data) ?? ModText.Get("inventory.selection_unavailable");
        var facts = new RewardSlotFacts(ui.IsSelected, SacrificedFor(ui), ui.CanBeUpgradedByRitual);
        string text = EndgameText.RewardSlot(item, facts, slot.Position, slot.Total);
        string key = $"{ui.GetInstanceID()}:{item}:{facts.Target}:{facts.SacrificedFor}:{facts.Upgradable}";
        return new EndgameStop(key, text, () => Details(data, container));
    }

    private static string SacrificedFor(RageOfMorditasRewardPanelRewardUI ui)
    {
        if (ui.IsSacrificedForBlood) return MorditasReader.RitualName(blood: true);
        return ui.IsSacrificedForFrost ? MorditasReader.RitualName(blood: false) : null;
    }

    // The item entry the game keeps in the slot's single-cell container.
    private static ItemContainerEntry Entry(ItemContainerUI container)
    {
        var source = container.Container;
        return source is null || source.WasCollected ? null : source.GetEntryAt(Vector2Int.zero);
    }

    // The item tooltip the game shows beside the focused slot, as the inventory reads it on request.
    private static string Details(ItemData data, ItemContainerUI container) =>
        data is not null && !data.WasCollected && IsAlive(container) &&
        ItemTooltipReader.TryReadForItem(data, container.gameObject, out string text) ? text : null;
}
