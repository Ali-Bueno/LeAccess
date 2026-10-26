using LeAccess.Accessibility.Endgame;
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
        var entry = ItemSlotReader.Entry(container);
        var data = ItemSlotReader.Data(entry);
        string item = ItemSlotReader.Identity(entry);
        var facts = new RewardSlotFacts(ui.IsSelected, SacrificedFor(ui), ui.CanBeUpgradedByRitual);
        string text = EndgameText.RewardSlot(item, facts, slot.Position, slot.Total);
        string key = $"{ui.GetInstanceID()}:{item}:{facts.Target}:{facts.SacrificedFor}:{facts.Upgradable}";
        return new EndgameStop(key, text, () => IsAlive(container)
            ? ItemSlotReader.Details(data, container.gameObject, TooltipItemManager.SlotType.MORDITAS) : null);
    }

    private static string SacrificedFor(RageOfMorditasRewardPanelRewardUI ui)
    {
        if (ui.IsSacrificedForBlood) return MorditasReader.RitualName(blood: true);
        return ui.IsSacrificedForFrost ? MorditasReader.RitualName(blood: false) : null;
    }
}
