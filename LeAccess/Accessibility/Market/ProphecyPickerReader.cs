using LeAccess.Accessibility.Utils;
using LE.Factions;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Market;

/// <summary>Reads the reward and lens options of the Observatory's pickers: name, effect, favor price, lock and place.</summary>
/// <remarks>See reference/endgame-panels.md#observatory.</remarks>
internal static class ProphecyPickerReader
{
    internal static bool TryRead(GameObject element, out string text)
    {
        text = null;
        if (!IsAlive(element) || IsAlive(element.GetComponentInParent<ProphecySlotEditUI>())) return false;
        var rewardOption = element.GetComponentInParent<ProphecyRewardOptionUI>();
        if (IsAlive(rewardOption)) return Reward(rewardOption, out text);
        var lensOption = element.GetComponentInParent<ProphecyLensOptionUI>();
        return IsAlive(lensOption) && Lens(lensOption, out text);
    }

    private static bool Reward(ProphecyRewardOptionUI option, out string text)
    {
        text = null;
        var reward = IsAlive(option._rewardUI) ? option._rewardUI.Reward : null;
        if (reward is not { WasCollected: false }) return false;
        string name = Clean(ProphecyRewardUI.GetDisplayName(reward));
        if (name == null) return false;
        string lockReason = Clean(reward.GetRewardMessageIfLocked());
        var socket = option.GetComponentInParent<ProphecyRewardSocketUI>();
        Place(socket, out int position, out int count);
        text = MarketText.RewardOption(name, Clean(reward.GetRewardDescription()), reward.FavorPerCharge, lockReason,
            lockReason != null, Current(socket?._activeIndicator), position, count);
        return true;
    }

    private static bool Lens(ProphecyLensOptionUI option, out string text)
    {
        text = null;
        var lens = IsAlive(option._lensUI) ? option._lensUI.Lens : default;
        string name = lens.HasValue ? Clean(ProphecyLensUI.GetDisplayName(lens)) : null;
        var socket = option.GetComponentInParent<ProphecyLensSocketUI>();
        Place(socket, out int position, out int count);
        text = MarketText.LensOption(name, lens.HasValue ? Clean(Localization.GetText(LensEffectKey((int)lens.Value), "")) : null,
            Current(socket?._activeIndicator), position, count);
        return true;
    }

    // The slot's effect label builds its text from the common string table: CircleOfFortune_Lens_<LensType index>_Effect.
    private static string LensEffectKey(int lensType) => $"CircleOfFortune_Lens_{lensType}_Effect";

    private static bool Current(UnityEngine.UI.Image indicator) => IsAlive(indicator) && indicator.isActiveAndEnabled;

    // Sockets sit side by side under the picker; the place is among the visible ones of the same kind.
    private static void Place<T>(T socket, out int position, out int count) where T : Component
    {
        position = count = 0;
        var parent = IsAlive(socket) ? socket.transform.parent : null;
        var all = IsAlive(parent) ? parent.GetComponentsInChildren<T>(false) : null;
        if (all == null || all.WasCollected) return;
        count = all.Length;
        for (int i = 0; i < count; i++)
            if (all[i].Pointer == socket.Pointer) position = i + 1;
    }

    private static string Clean(string text) => string.IsNullOrWhiteSpace(text) ? null : TextUtils.CleanText(text);
}
