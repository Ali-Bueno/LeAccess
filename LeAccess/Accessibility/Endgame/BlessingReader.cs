using LeAccess.Accessibility.Forge;
using LeAccess.Accessibility.Items;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LE.UI.PanelSystem;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Endgame;

/// <summary>Reads a blessing option of the Choose Blessing panel: name, lock reason, role and the tooltip the game shows.</summary>
/// <remarks>See reference/endgame-panels.md#choose-blessing.</remarks>
internal static class BlessingReader
{
    // The game's item tooltip opens a moment after focus arrives; past this wait the name is spoken without it.
    private const float TooltipWaitSeconds = 0.6f;
    private static string _waitingKey;
    private static float _waitingSince;

    internal static bool Owns(GameObject element) => IsAlive(element.GetComponentInParent<BlessingOptionContainerUI>());

    internal static bool TryRead(ChooseBlessingPanel panel, GameObject selected, out EndgameStop stop)
    {
        stop = default;
        var option = selected.GetComponentInParent<BlessingOptionContainerUI>();
        var manager = IsAlive(panel) ? panel._blessingRewardPanelManager : null;
        if (!IsAlive(option) || !IsAlive(manager)) return false;
        var data = Held(option);
        string name = ForgeCellReader.NameOf(data);
        bool locked = option.locked;
        string key = $"{option.GetInstanceID()}:{name}:{locked}";
        string tooltip = null;
        if (data is not null && !locked && !ItemTooltipReader.TryReadForItem(data, option.gameObject, out tooltip) &&
            Waiting(key)) return false;
        stop = new EndgameStop(key, EndgameText.Blessing(Role(manager, option), name, locked ? LockReason(option) : null, locked,
            Position(manager, option), Count(manager), tooltip));
        return true;
    }

    private static ItemData Held(BlessingOptionContainerUI option)
    {
        var items = option.Container;
        var entry = items is null || items.WasCollected ? null : items.GetEntryAt(option.currentFocusSlot);
        return entry is null || entry.WasCollected || entry.data is null || entry.data.WasCollected ? null : entry.data;
    }

    private static BlessingRole Role(BlessingRewardPanelManager manager, BlessingOptionContainerUI option) =>
        Same(manager.currentBlessing, option) ? BlessingRole.Current :
        Same(manager.currentBlessingConfirmation, option) ? BlessingRole.New : BlessingRole.Option;

    private static int Position(BlessingRewardPanelManager manager, BlessingOptionContainerUI option)
    {
        var options = manager.blessingOptions;
        for (int i = 0; options is not null && !options.WasCollected && i < options.Count; i++)
            if (Same(options[i], option)) return i + 1;
        return 0;
    }

    private static int Count(BlessingRewardPanelManager manager) =>
        manager.blessingOptions is { WasCollected: false } options ? options.Count : 0;

    // The game's own lock explanation, localized from the key its tooltip carries.
    private static string LockReason(BlessingOptionContainerUI option)
    {
        var tooltip = option.lockTooltip;
        if (!IsAlive(tooltip)) return null;
        string title = Localize(tooltip.titleKey, tooltip.title);
        string body = Localize(tooltip.bodyKey, tooltip.body);
        return ScreenContentTracker.Combine(new[] { title, body });
    }

    private static string Localize(string key, string fallback) =>
        TextUtils.CleanText(string.IsNullOrWhiteSpace(key) ? fallback : Localization.GetText(key, fallback ?? ""));

    private static bool Waiting(string key)
    {
        if (_waitingKey != key) { _waitingKey = key; _waitingSince = Time.unscaledTime; }
        return Time.unscaledTime - _waitingSince < TooltipWaitSeconds;
    }

    private static bool Same(Component a, Component b) => IsAlive(a) && IsAlive(b) && a.Pointer == b.Pointer;
}
