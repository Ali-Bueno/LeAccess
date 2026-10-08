using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;

namespace LeAccess.Accessibility.Endgame;

internal enum VoteState { Pending, Accepted, Declined }

internal readonly record struct VoteEntry(string Name, VoteState State);

internal enum BlessingRole { Option, Current, New }

internal enum IslandState { Available, Completed, Unreachable }

/// <summary>Mod-authored wording and state diffing for the endgame panels; game text is always passed in.</summary>
/// <remarks>See reference/endgame-panels.md.</remarks>
internal static class EndgameText
{
    /// <summary>Everyone's vote, in the panel's order.</summary>
    internal static string Roster(IReadOnlyList<VoteEntry> votes)
    {
        var parts = new List<string>();
        foreach (var vote in votes)
            if (!string.IsNullOrWhiteSpace(vote.Name)) parts.Add(ModText.Format("endgame.vote_roster", vote.Name, State(vote.State)));
        return ScreenContentTracker.Combine(parts.ToArray());
    }

    /// <summary>Only the players whose vote moved since <paramref name="previous"/>; null when nothing changed.</summary>
    internal static string VoteChanges(IReadOnlyList<VoteEntry> previous, IReadOnlyList<VoteEntry> current)
    {
        var before = new Dictionary<string, VoteState>();
        foreach (var vote in previous) before[vote.Name ?? ""] = vote.State;
        var parts = new List<string>();
        foreach (var vote in current)
        {
            if (string.IsNullOrWhiteSpace(vote.Name)) continue;
            bool known = before.TryGetValue(vote.Name, out var old);
            if (known && old == vote.State || !known && vote.State == VoteState.Pending) continue;
            parts.Add(ModText.Format(vote.State switch
            {
                VoteState.Accepted => "endgame.vote_accepted",
                VoteState.Declined => "endgame.vote_declined",
                _ => "endgame.vote_pending_again"
            }, vote.Name));
        }
        return parts.Count == 0 ? null : ScreenContentTracker.Combine(parts.ToArray());
    }

    /// <summary>A choosable option: the game's label, whether it is the picked one, and its place among the options.</summary>
    internal static string Option(string label, bool chosen, bool enabled, int position, int total)
    {
        string text = enabled ? label : ModText.Format("control.disabled", label);
        return ScreenContentTracker.Combine(new[]
        {
            text, chosen ? ModText.Get("endgame.chosen") : null,
            total > 1 && position > 0 ? ModText.Format("list.position", position, total) : null
        });
    }

    /// <summary>A button with the game's caption; unavailable ones say so.</summary>
    internal static string Button(string caption, bool enabled) =>
        enabled ? caption : ModText.Format("control.disabled", caption);

    /// <summary>An echo of the Monolith web: the game's tooltip text first, then what the tooltip does not say.</summary>
    internal static string Island(string tooltip, IslandState state, bool rewardWaiting) =>
        ScreenContentTracker.Combine(new[]
        {
            tooltip,
            state switch
            {
                IslandState.Completed => ModText.Get("endgame.island_completed"),
                IslandState.Unreachable => ModText.Get("endgame.island_unreachable"),
                _ => null
            },
            rewardWaiting ? ModText.Get("endgame.island_reward") : null
        });

    /// <summary>A blessing slot: who it is (kept, new or an offer), the game's tooltip or name, lock reason and place.</summary>
    internal static string Blessing(BlessingRole role, string name, string lockReason, bool locked, int position, int total, string tooltip) =>
        ScreenContentTracker.Combine(new[]
        {
            role switch
            {
                BlessingRole.Current => ModText.Get("endgame.blessing_current"),
                BlessingRole.New => ModText.Get("endgame.blessing_new"),
                _ => null
            },
            locked ? ModText.Get("endgame.locked") : tooltip ?? name ?? ModText.Get("endgame.slot_empty"),
            lockReason,
            role == BlessingRole.Option && total > 1 && position > 0 ? ModText.Format("list.position", position, total) : null
        });

    /// <summary>A dungeon or arena tier tab, which has no text of its own.</summary>
    internal static string Tier(int tier, bool selected, bool locked, int position, int total) =>
        ScreenContentTracker.Combine(new[]
        {
            ModText.Format("endgame.tier", tier),
            selected ? ModText.Get("endgame.selected") : null,
            locked ? ModText.Get("endgame.locked") : null,
            total > 1 ? ModText.Format("list.position", position, total) : null
        });

    private static string State(VoteState state) => ModText.Get(state switch
    {
        VoteState.Accepted => "endgame.state_accepted",
        VoteState.Declined => "endgame.state_declined",
        _ => "endgame.state_pending"
    });
}
