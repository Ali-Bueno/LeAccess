using LeAccess.Accessibility.Endgame;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LE.UI.PanelSystem;
using UnityEngine;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Market;

/// <summary>Names the Rage of Morditas reward buttons, which have no caption, from the game's own strings and tooltip.</summary>
/// <remarks>See reference/endgame-panels.md#rage-of-morditas-reward.</remarks>
internal static class MorditasReader
{
    // Keys of the game's UI string table that its tooltip and rewards window use for these three actions.
    private const string BloodKey = "RageOfMorditas_BloodyRitual_Name";
    private const string FrostKey = "RageOfMorditas_FrozenRitual_Name";
    private const string ClaimKey = "Reward_Claim";
    private const float TooltipWaitSeconds = 0.6f;
    private static string _waitingKey;
    private static float _waitingSince;

    internal static bool Owns(RageOfMorditasRewardPanel panel, GameObject element) => Which(panel, element, out _, out _);

    internal static bool TryRead(RageOfMorditasRewardPanel panel, GameObject element, out EndgameStop stop)
    {
        stop = default;
        if (!Which(panel, element, out var button, out string key)) return false;
        string caption = TextUtils.CleanText(Localization.GetText(key, ""));
        if (string.IsNullOrWhiteSpace(caption)) return false;
        string tooltip = key == ClaimKey ? null : TooltipText();
        string stopKey = $"{element.GetInstanceID()}:{button.interactable}";
        if (key != ClaimKey && tooltip == null && Waiting(stopKey)) return false;
        string body = tooltip != null && tooltip.Contains(caption) ? tooltip : ScreenContentTracker.Combine(new[] { caption, tooltip });
        stop = new EndgameStop(stopKey, button.interactable ? body : LeAccess.I18n.ModText.Format("control.disabled", body));
        return true;
    }

    private static bool Which(RageOfMorditasRewardPanel panel, GameObject element, out Button button, out string key)
    {
        button = null;
        key = null;
        if (!IsAlive(panel) || !IsAlive(element)) return false;
        if (Inside(panel.RitualOfBloodButton, element)) { button = panel.RitualOfBloodButton; key = BloodKey; }
        else if (Inside(panel.RitualOfIceButton, element)) { button = panel.RitualOfIceButton; key = FrostKey; }
        else if (Inside(panel.ClaimButton, element)) { button = panel.ClaimButton; key = ClaimKey; }
        return button != null;
    }

    // The ritual tooltip opens on focus: description, what the marked item becomes and the Bloodrage cost.
    private static string TooltipText()
    {
        var ui = UIBase.instance;
        var system = IsAlive(ui) ? ui.TooltipSystem : null;
        var tooltip = IsAlive(system) ? system._mortidasTooltip : null;
        if (!IsAlive(tooltip) || !UiVisibility.IsVisible(tooltip.gameObject)) return null;
        return ScreenContentReader.Read(tooltip.gameObject, tooltip._name, tooltip._description);
    }

    private static bool Waiting(string key)
    {
        if (_waitingKey != key) { _waitingKey = key; _waitingSince = Time.unscaledTime; }
        return Time.unscaledTime - _waitingSince < TooltipWaitSeconds;
    }

    private static bool Inside(Component root, GameObject element) =>
        IsAlive(root) && element.transform.IsChildOf(root.transform);
}
