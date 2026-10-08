using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Panels;

/// <summary>Wording the faction, vendor and item-service readers add to the game's own text.</summary>
/// <remarks>See reference/faction-vendor-panels.md.</remarks>
internal static class PanelText
{
    internal static string Tab(string name, bool selected, int position, int count) =>
        Combine(name, ModText.Get(selected ? "panels.tab_selected" : "panels.tab"), Position(position, count));

    internal static string Refresh(string label, bool available) =>
        available ? label : ModText.Format("control.disabled", label);

    internal static string YouHave(string amounts) =>
        string.IsNullOrWhiteSpace(amounts) ? null : ModText.Format("panels.you_have", amounts);

    internal static string Standing(int rank, int favor) => ModText.Format("panels.standing", rank, favor);

    internal static string Banner(string summary) => Combine(summary, ModText.Get("panels.faction_banner"));

    internal static string RankReward(int rank, int count, string title, string description, bool unlocked, bool current,
        string unlockHint) =>
        Combine(ModText.Format("panels.rank_of", rank, count), current ? ModText.Get("panels.rank_current") : null, title,
            description, ModText.Get(unlocked ? "panels.reward_unlocked" : "panels.reward_locked"),
            unlocked ? null : unlockHint);

    internal static string Evolution(string content, bool chosen, int position, int count) =>
        Combine(content, chosen ? ModText.Get("panels.chosen") : null, Position(position, count));

    internal static string Rule(int position, int count, string description, bool enabled) =>
        Combine(ModText.Format("panels.rule", position, count), description, enabled ? null : ModText.Get("panels.rule_off"));

    internal static string RuleSwitch(string description, bool enabled) =>
        Combine(description, ModText.Get(enabled ? "panels.rule_on" : "panels.rule_off"));

    internal static string KeySlot(int position, int count) => ModText.Format("panels.key_slot", position, count);

    internal static string Cost(IEnumerable<string> parts) => ModText.Format("panels.cost", Combine(parts));

    internal static string MerchantTokens(string amount) => ModText.Format("panels.merchant_tokens", amount);

    internal static string Position(int position, int count) =>
        count > 1 && position > 0 ? ModText.Format("list.position", position, count) : null;

    private static string Combine(params string[] parts) => ScreenContentTracker.Combine(parts);

    private static string Combine(IEnumerable<string> parts) => ScreenContentTracker.Combine(parts);
}
