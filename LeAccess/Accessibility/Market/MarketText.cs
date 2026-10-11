using System.Collections.Generic;
using System.Linq;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Market;

internal readonly record struct ProphecySlotInfo(string Region, bool Locked, string LockText, string Target, string Reward,
    string RewardDescription, string Lens, int Charges, int Percent, int Favor, bool Ready);

internal readonly record struct LadderMember(string Name, string Class, string Level, bool Dead);

internal readonly record struct ListingSummary(string Name, string Type, string Level, string Gold, string Favor,
    int Position, int Count, string State = null, string Action = null);

internal enum ListingState { Active, Sold, Redeemed, Cancelled }

/// <summary>Mod-authored wording of the Observatory, Ladder and Bazaar readers; game text is always passed in.</summary>
/// <remarks>See reference/endgame-panels.md and reference/faction-vendor-panels.md.</remarks>
internal static class MarketText
{
    /// <summary>One prophecy slot: the region, what it needs, what it gives and how charged it is.</summary>
    internal static string ProphecySlot(ProphecySlotInfo slot)
    {
        if (slot.Locked) return Combine(slot.Region, ModText.Get("market.locked"), slot.LockText);
        return Combine(slot.Region, Labeled("market.prophecy_target", slot.Target),
            string.IsNullOrWhiteSpace(slot.Reward) ? ModText.Get("market.prophecy_reward_none") : ModText.Format("market.prophecy_reward", slot.Reward),
            slot.RewardDescription,
            string.IsNullOrWhiteSpace(slot.Lens) ? ModText.Get("market.prophecy_lens_none") : slot.Lens,
            ModText.Format("market.prophecy_charges", slot.Charges),
            slot.Percent > 0 ? ModText.Format("market.prophecy_progress", slot.Percent) : null,
            slot.Favor > 0 ? ModText.Format("market.prophecy_favor", slot.Favor) : null,
            slot.Ready ? ModText.Get("market.prophecy_ready") : null);
    }

    /// <summary>The lens in a slot: its name and the effect text the game shows, without saying the name twice.</summary>
    internal static string Lens(string name, string effect)
    {
        if (string.IsNullOrWhiteSpace(effect)) return Labeled("market.prophecy_lens", name);
        bool named = string.IsNullOrWhiteSpace(name) || effect.Contains(name, System.StringComparison.OrdinalIgnoreCase);
        return Labeled("market.prophecy_lens", named ? effect : Combine(name, effect));
    }

    internal static string RegionStep(bool next, string region) =>
        string.IsNullOrWhiteSpace(region) ? null : ModText.Format(next ? "market.prophecy_next" : "market.prophecy_previous", region);

    /// <summary>A reward offered by the picker: name, what it does, its favor price and whether it is out of reach.</summary>
    internal static string RewardOption(string name, string description, int favor, string lockReason, bool locked, bool current,
        int position, int count) =>
        Combine(name, current ? ModText.Get("market.current") : null, locked ? ModText.Get("market.locked") : description,
            locked ? lockReason : null, favor > 0 && !locked ? ModText.Format("market.prophecy_favor", favor) : null, Place(position, count));

    /// <summary>A lens offered by the picker: name, what it does (the game's effect text), whether it is equipped, place.</summary>
    internal static string LensOption(string name, string effect, bool current, int position, int count) =>
        Combine(name ?? ModText.Get("market.prophecy_lens_none"), current ? ModText.Get("market.current") : null, effect, Place(position, count));

    /// <summary>A ladder row: rank first, then each character with its class and level, then the score.</summary>
    internal static string LadderRow(string rank, string score, IReadOnlyList<LadderMember> members, bool own, int position, int count)
    {
        var people = members.Select(Member).Where(text => !string.IsNullOrWhiteSpace(text)).ToArray();
        return Combine(Labeled("market.ladder_rank", rank), own ? ModText.Get("market.ladder_own") : null,
            string.Join(". ", people), Labeled("market.ladder_score", score), Place(position, count));
    }

    private static string Member(LadderMember member) =>
        Combine(member.Name, member.Class, LevelText(member.Level), member.Dead ? ModText.Get("market.ladder_dead") : null);

    internal static string LadderEmpty() => ModText.Get("market.ladder_empty");

    /// <summary>A bazaar listing: what it is and what it costs; the affixes follow when details are asked for.</summary>
    internal static string Listing(ListingSummary listing, string details) =>
        Combine(listing.Name, listing.Type, listing.Level, Price(listing.Gold, listing.Favor), listing.State, listing.Action, details,
            Place(listing.Position, listing.Count));

    /// <summary>What a listing of the player's own stall is doing.</summary>
    internal static string ListingStatus(ListingState state) => ModText.Get(state switch
    {
        ListingState.Sold => "market.listing_sold",
        ListingState.Redeemed => "market.listing_redeemed",
        ListingState.Cancelled => "market.listing_cancelled",
        _ => "market.listing_active"
    });

    internal static string ListingAction(string caption) => Labeled("market.listing_action", caption);

    /// <summary>The stall view the player is in, with its listing count or the empty notice.</summary>
    internal static string StallView(bool sold, int total, int page, int pages) => Combine(
        ModText.Get(sold ? "market.stall_sold" : "market.stall_listed"),
        total <= 0 ? ModText.Get("market.stall_empty") : ModText.Format("market.results", total, page, pages));

    internal static string SoldWaiting(string count) => Labeled("market.sold_waiting", count);

    /// <summary>A filter or form field: its title, the typed value (or that it is empty) and the unit it is typed in.</summary>
    internal static string InputField(string title, string value, string suffix = null, bool gold = false)
    {
        title ??= ModText.Get("control.text_field");
        if (string.IsNullOrWhiteSpace(value)) return ModText.Format("control.empty_field", title);
        string shown = gold ? ModText.Format("market.price_gold", value) : string.IsNullOrWhiteSpace(suffix) ? value : $"{value} {suffix}";
        return DropdownReader.Control(title, shown);
    }

    /// <summary>One of a minimum / maximum pair of filter fields.</summary>
    internal static string RangeField(string title, bool max, string value) =>
        InputField(ModText.Format(max ? "market.range_max" : "market.range_min", title ?? ModText.Get("control.text_field")), value);

    internal static string Checkbox(string title, bool on) => DropdownReader.Control(title, ModText.Get(on ? "state.on" : "state.off"));

    /// <summary>The item chosen for listing: its name and details, with the price the game suggests.</summary>
    internal static string ListItem(string name, string details, string suggested) =>
        Combine(Labeled("market.list_item", name), details, Labeled("market.list_suggested", suggested));

    internal static string ListEmpty(string gameText) => string.IsNullOrWhiteSpace(gameText) ? ModText.Get("market.list_empty") : gameText;

    /// <summary>The confirm button of the list-an-item form with its favor cost and the fee summaries the game shows.</summary>
    internal static string ListButton(string caption, string favorCost, string tax, string lowering) => Combine(
        string.IsNullOrWhiteSpace(caption) ? ModText.Get("market.list_action") : caption, Labeled("market.list_favor_cost", favorCost), tax, lowering);

    internal static string SourceButton(bool stash, string caption) => string.IsNullOrWhiteSpace(caption)
        ? ModText.Get(stash ? "market.list_stash" : "market.list_inventory") : caption;

    private static string Price(string gold, string favor) => Combine(
        string.IsNullOrWhiteSpace(gold) ? null : ModText.Format("market.price_gold", gold),
        string.IsNullOrWhiteSpace(favor) ? null : ModText.Format("market.price_favor", favor));

    internal static string Potential(string key, string value) => string.IsNullOrWhiteSpace(value) ? null : ModText.Format(key, value);

    internal static string Corrupted() => ModText.Get("market.corrupted");

    internal static string Page(int page, int pages, bool current) =>
        Combine(pages > 1 ? ModText.Format("market.page_of", page, pages) : ModText.Format("market.page", page),
            current ? ModText.Get("market.current") : null);

    internal static string PageStep(bool next) => ModText.Get(next ? "market.page_next" : "market.page_previous");

    /// <summary>What the bazaar shows after a search: how many listings, or why none.</summary>
    internal static string Results(int total, int page, int pages) => total <= 0 ? ModText.Get("market.no_results") :
        ModText.Format("market.results", total, page, pages);

    private static string LevelText(string level) =>
        !string.IsNullOrWhiteSpace(level) && level.All(char.IsDigit) ? ModText.Format("market.ladder_level", level) : level;

    private static string Labeled(string key, string value) => string.IsNullOrWhiteSpace(value) ? null : ModText.Format(key, value);

    private static string Place(int position, int count) =>
        ListPosition.Text(position, count);

    private static string Combine(params string[] parts) => ScreenContentTracker.Combine(parts);
}
