using LeAccess.Accessibility.Endgame;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LE.Factions;
using LE.UI.PanelSystem;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Market;

/// <summary>Reads the Observatory's prophecy slot: region, goal, reward, lens, charges and favor price.</summary>
/// <remarks>See reference/endgame-panels.md#observatory.</remarks>
internal static class ObservatoryReader
{
    internal static bool TryRead(ObservatoryPanel panel, GameObject selected, out EndgameStop stop)
    {
        stop = default;
        var ui = panel._observatoryUI;
        var edit = IsAlive(ui) ? ui._prophecySlotEditUI : null;
        if (!IsAlive(edit) || edit._slot is not { WasCollected: false } slot) return false;
        var region = ui.CurrentRegion;
        string regionName = Clean(region.GetDisplayName());
        if (IsAlive(selected) && Step(ui, selected, region) is { } step)
        {
            stop = new EndgameStop($"step:{step.Next}:{region}", MarketText.RegionStep(step.Next, Clean(step.Region.GetDisplayName())));
            return stop.Text != null;
        }
        var reward = slot.Reward;
        bool hasReward = reward is { WasCollected: false };
        bool locked = IsAlive(edit._lockedOverlay) && edit._lockedOverlay.gameObject.activeInHierarchy;
        var info = new ProphecySlotInfo(regionName, locked, locked ? Visible(edit._lockedOverlay._text) : null,
            Visible(IsAlive(edit._target) ? edit._target._targetDescription : null),
            hasReward ? Clean(ProphecyRewardUI.GetDisplayName(reward)) : null, hasReward ? Clean(reward.GetRewardDescription()) : null,
            LensText(edit, slot), slot.CompletedCharges, Percent(slot), hasReward ? reward.FavorPerCharge : 0, Ready(edit));
        stop = new EndgameStop($"{region}:{(hasReward ? reward.ID : -1)}:{LensKey(slot)}:{slot.CompletedCharges}:{locked}",
            MarketText.ProphecySlot(info));
        return true;
    }

    /// <summary>The slot's own text, which the stop reads, stays out of the window text.</summary>
    internal static bool IsSlotText(ObservatoryPanel panel, Component label)
    {
        var ui = IsAlive(panel) ? panel._observatoryUI : null;
        var edit = IsAlive(ui) ? ui._prophecySlotEditUI : null;
        return IsAlive(edit) && label.transform.IsChildOf(edit.transform);
    }

    /// <summary>Region buttons have only an icon; they are read as steps to the neighbouring region.</summary>
    internal static bool Owns(ObservatoryPanel panel, GameObject element)
    {
        var ui = IsAlive(panel) ? panel._observatoryUI : null;
        return IsAlive(ui) && Step(ui, element, ui.CurrentRegion) != null;
    }

    private static (bool Next, ProphecyRegion Region)? Step(ObservatoryUI ui, GameObject element, ProphecyRegion current)
    {
        if (Inside(ui._previousRegionButton, element)) return (false, current.Decrement());
        if (Inside(ui._nextRegionButton, element)) return (true, current.Increment());
        return null;
    }

    private static string LensText(ProphecySlotEditUI edit, ProphecySlot slot)
    {
        var lens = slot.Lens;
        if (!lens.HasValue) return null;
        string effect = Visible(IsAlive(edit._lensEffect) ? edit._lensEffect._lensEffectText : null);
        return MarketText.Lens(Clean(ProphecyLensUI.GetDisplayName(lens)), effect);
    }

    private static string LensKey(ProphecySlot slot) => slot.Lens.HasValue ? ((int)slot.Lens.Value).ToString() : "-";

    // The game's progress bar takes a fraction; the spoken value is the same fraction in percent.
    private static int Percent(ProphecySlot slot) => (int)System.Math.Round(Mathf.Clamp01(slot.GetChargePercentage()) * 100f);

    private static bool Ready(ProphecySlotEditUI edit) =>
        IsAlive(edit._reward) && IsAlive(edit._reward._readyToFulfillIndicator) &&
        edit._reward._readyToFulfillIndicator.activeInHierarchy;

    private static bool Inside(Component root, GameObject element) =>
        IsAlive(root) && IsAlive(element) && element.transform.IsChildOf(root.transform);

    private static string Visible(TMPro.TMP_Text label) => TextUtils.IsVisibleLabel(label) ? Clean(label.text) : null;

    private static string Clean(string text) => string.IsNullOrWhiteSpace(text) ? null : TextUtils.CleanText(text);
}
