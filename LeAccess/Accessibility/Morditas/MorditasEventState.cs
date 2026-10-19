using System;
using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;

namespace LeAccess.Accessibility.Morditas;

/// <summary>RageOfMorditasLogic.State, as the encounter's network object syncs it to every player.</summary>
internal enum MorditasPhase : byte { NotStarted, Hunt, PrepareGauntlet, Gauntlet, ReadyForReward, Finished }

/// <summary>One reading of the encounter; Inside is the local player's circle status, null until the game reports it,
/// and the centre is the circle's centre from the player in steps with a guidance direction key.</summary>
internal readonly record struct MorditasSample(int Encounter, MorditasPhase Phase, bool Success, int HuntKills,
    float TimeLeft, int GauntletKills, bool? Inside = null, bool AllOut = false, int CentreSteps = 0,
    string CentreDirection = null)
{
    // The gauntlet raises one bloodcrafted copy per hunt kill (reference/rage-of-morditas.md#gauntlet).
    internal int Remaining => Math.Max(0, HuntKills - GauntletKills);
}

/// <summary>The game's numbers: charge kill thresholds (the first opens the gauntlet), timer warnings and the circle.</summary>
internal readonly record struct MorditasRules(IReadOnlyList<int> Thresholds, float WarningSeconds, float CriticalSeconds,
    float OutOfZoneSeconds, int RadiusSteps)
{
    internal int Requirement => Thresholds?.Count > 0 ? Thresholds[0] : 0;

    internal int Charges(int kills)
    {
        int charges = 0;
        foreach (int threshold in Thresholds ?? Array.Empty<int>()) if (kills >= threshold) charges++;
        return charges;
    }

    internal int Next(int kills)
    {
        foreach (int threshold in Thresholds ?? Array.Empty<int>()) if (kills < threshold) return threshold;
        return 0;
    }
}

/// <summary>The game's own localized texts for the encounter.</summary>
internal readonly record struct MorditasGameTexts(string Name, string StartedTitle, string StartedDescription,
    string RitualTitle, string RitualDescription, string GauntletObjective, string RewardObjective, string ChargeTitle,
    string FailedTitle, string FailedDescription);

internal delegate string MorditasText(string key, params object[] arguments);

internal readonly record struct MorditasUpdate(string Text, bool Interrupt)
{
    internal bool HasContent => !string.IsNullOrWhiteSpace(Text);
}

// What the player hears about one encounter: each phase once, coalesced counts, the timer's warnings and the circle.
// Nothing counts as said until Acknowledge, so blocked speech returns later with the latest values.
internal sealed class MorditasEventState
{
    // Mod pacing (reference/rage-of-morditas.md#pacing): an area attack's kills land over several frames, so a count
    // speaks once it has rested, or after MaxWait while kills keep coming; then not again before its interval.
    internal const double SettleSeconds = 0.6, MaxWaitSeconds = 2.5;
    internal const double HuntInterval = 2, ChargeInterval = 6, GauntletInterval = 3;
    // Kills add time in small steps; a passed warning re-arms only once time left climbs this far above it.
    internal const float RearmSeconds = 2;
    private const int Warning = 1, Critical = 2;

    private Spoken _spoken, _next;
    private bool _tracking;
    private int _encounter, _seen = int.MinValue;
    private double _changedAt, _pendingSince = double.NaN;

    internal int Encounter => _tracking ? _encounter : 0;

    internal MorditasUpdate Observe(in MorditasSample sample, MorditasRules rules, MorditasGameTexts game,
        MorditasText text, double now)
    {
        if (!_tracking || sample.Encounter != _encounter) Begin(sample);
        _next = _spoken;
        var parts = new List<string>();
        bool interrupt = sample.Phase != _next.Phase ? Enter(sample, rules, game, text, parts, now)
            : sample.Phase == MorditasPhase.Hunt ? Hunt(sample, rules, game, text, parts, now)
            : sample.Phase == MorditasPhase.Gauntlet && Gauntlet(sample, rules, text, parts, now);
        var update = new MorditasUpdate(ScreenContentTracker.Combine(parts), interrupt);
        if (!update.HasContent) _spoken = _next;
        return update;
    }

    internal void Acknowledge()
    {
        if (_next.CountAt != _spoken.CountAt) _pendingSince = double.NaN;
        _spoken = _next;
    }

    internal void Reset()
    {
        _tracking = false;
        _encounter = 0;
        _spoken = _next = default;
        ResetCount();
    }

    /// <summary>The on-demand line: the event's name and where it stands.</summary>
    internal static string Status(in MorditasSample sample, MorditasRules rules, MorditasGameTexts game, MorditasText text)
    {
        var parts = new List<string> { game.Name };
        switch (sample.Phase)
        {
            case MorditasPhase.Hunt:
                parts.Add(KillsText(sample.HuntKills, rules, text));
                if (sample.TimeLeft > 0) parts.Add(TimeText(sample.TimeLeft, text));
                break;
            case MorditasPhase.PrepareGauntlet: parts.Add(game.RitualTitle); break;
            case MorditasPhase.Gauntlet:
                parts.Add(EnemiesText(sample.Remaining, text));
                if (sample.Inside == false || sample.AllOut) { parts.Add(text("morditas.outside")); parts.Add(CentreText(sample, text)); }
                break;
            case MorditasPhase.ReadyForReward: parts.Add(game.RewardObjective); break;
            default: return null;
        }
        return ScreenContentTracker.Combine(parts);
    }

    // A first reading mid-encounter is announced; one before the start or after the end is only a baseline.
    private void Begin(in MorditasSample sample)
    {
        _tracking = true;
        _encounter = sample.Encounter;
        _spoken = new Spoken { Phase = sample.Phase is MorditasPhase.NotStarted or MorditasPhase.Finished
            ? sample.Phase : MorditasPhase.NotStarted };
        ResetCount();
    }

    private bool Enter(in MorditasSample sample, MorditasRules rules, MorditasGameTexts game, MorditasText text,
        List<string> parts, double now)
    {
        _next = new Spoken { Phase = sample.Phase, CountAt = now };
        ResetCount();
        switch (sample.Phase)
        {
            case MorditasPhase.Hunt:
                parts.Add(game.StartedTitle);
                parts.Add(game.StartedDescription);
                if (rules.Requirement > 0) parts.Add(text("morditas.hunt_goal", rules.Requirement));
                if (sample.TimeLeft > 0) parts.Add(TimeText(sample.TimeLeft, text));
                if (sample.HuntKills > 0) parts.Add(KillsText(sample.HuntKills, rules, text));
                _next.Kills = sample.HuntKills;
                _next.Charges = rules.Charges(sample.HuntKills);
                _next.Requirement = rules.Requirement > 0 && sample.HuntKills >= rules.Requirement;
                // The time just said covers any warning already passed.
                if (sample.TimeLeft <= rules.WarningSeconds) _next.Warned |= Warning;
                if (sample.TimeLeft <= rules.CriticalSeconds) _next.Warned |= Critical;
                return false;
            case MorditasPhase.PrepareGauntlet:
                parts.Add(game.RitualTitle);
                parts.Add(game.RitualDescription);
                return false;
            case MorditasPhase.Gauntlet:
                parts.Add(game.GauntletObjective);
                if (rules.RadiusSteps > 0) parts.Add(text("morditas.stay_within", StepsText(rules.RadiusSteps, text)));
                parts.Add(EnemiesText(sample.Remaining, text));
                _next.Remaining = sample.Remaining;
                return false;
            case MorditasPhase.ReadyForReward:
                parts.Add(game.RewardObjective);
                return false;
            case MorditasPhase.Finished when sample.Success:
                parts.Add(text("morditas.complete", game.Name));
                return false;
            case MorditasPhase.Finished:
                parts.Add(game.FailedTitle);
                parts.Add(game.FailedDescription);
                return true;
            default: return false;
        }
    }

    private bool Hunt(in MorditasSample sample, MorditasRules rules, MorditasGameTexts game, MorditasText text,
        List<string> parts, double now)
    {
        int kills = sample.HuntKills;
        bool met = rules.Requirement > 0 && kills >= rules.Requirement;
        if (met && !_next.Requirement)
        {
            parts.Add(text("morditas.requirement_met", kills));
            _next.Requirement = true;
        }
        int charges = rules.Charges(kills);
        if (charges > _next.Charges)
        {
            parts.Add(game.ChargeTitle);
            int next = rules.Next(kills);
            if (next > 0) parts.Add(text("morditas.next_charge", next));
            _next.Charges = charges;
        }
        if (parts.Count > 0) Counted(kills, now);
        else if (CountDue(kills, _next.Kills, met ? ChargeInterval : HuntInterval, now))
        {
            parts.Add(KillsText(kills, rules, text));
            Counted(kills, now);
        }
        return Timer(sample.TimeLeft, rules, text, parts) && !met;
    }

    // True for the critical warning, which cuts other speech.
    private bool Timer(float left, MorditasRules rules, MorditasText text, List<string> parts)
    {
        if (!(left > 0)) return false;
        if (left > rules.WarningSeconds + RearmSeconds) _next.Warned &= ~Warning;
        if (left > rules.CriticalSeconds + RearmSeconds) _next.Warned &= ~Critical;
        int due = left <= rules.CriticalSeconds ? Critical : left <= rules.WarningSeconds ? Warning : 0;
        if (due == 0 || (_next.Warned & due) != 0) return false;
        parts.Add(TimeText(left, text));
        _next.Warned |= due | Warning;
        return due == Critical;
    }

    private bool Gauntlet(in MorditasSample sample, MorditasRules rules, MorditasText text, List<string> parts, double now)
    {
        bool urgent = false, outside = sample.Inside == false || sample.AllOut;
        if (outside && _next.Inside != false)
        {
            parts.Add(text("morditas.outside"));
            parts.Add(CentreText(sample, text));
            if (sample.AllOut) parts.Add(AbandonText(rules, text));
            _next.Inside = false;
            _next.AllOut = sample.AllOut;
            urgent = true;
        }
        else if (!outside && _next.Inside == false && sample.Inside == true)
        {
            parts.Add(text("morditas.inside"));
            _next.Inside = true;
            _next.AllOut = false;
        }
        else if (outside && sample.AllOut && !_next.AllOut)
        {
            parts.Add(AbandonText(rules, text));
            _next.AllOut = urgent = true;
        }
        else if (!sample.AllOut) _next.AllOut = false;
        int remaining = sample.Remaining;
        if (remaining > 0 && CountDue(remaining, _next.Remaining, GauntletInterval, now))
        {
            parts.Add(EnemiesText(remaining, text));
            _next.Remaining = remaining;
            _next.CountAt = now;
        }
        return urgent;
    }

    private bool CountDue(int value, int spoken, double interval, double now)
    {
        if (value != _seen) { _seen = value; _changedAt = now; }
        if (value == spoken) { _pendingSince = double.NaN; return false; }
        if (double.IsNaN(_pendingSince)) _pendingSince = now;
        return (now - _changedAt >= SettleSeconds || now - _pendingSince >= MaxWaitSeconds) &&
            now - _next.CountAt >= interval;
    }

    private void Counted(int kills, double now)
    {
        _next.Kills = kills;
        _next.CountAt = now;
    }

    private void ResetCount()
    {
        _seen = int.MinValue;
        _pendingSince = double.NaN;
    }

    // Before the gauntlet is earned, kills count toward it; then toward the next charge.
    private static string KillsText(int kills, MorditasRules rules, MorditasText text)
    {
        int goal = rules.Requirement > 0 && kills < rules.Requirement ? rules.Requirement : rules.Next(kills);
        return goal > 0 ? text("morditas.kills_of", kills, goal) : text("morditas.kills", kills);
    }

    private static string TimeText(float left, MorditasText text)
    {
        int seconds = (int)Math.Ceiling(left);
        return seconds == 1 ? text("morditas.time_left_one") : text("morditas.time_left", seconds);
    }

    private static string EnemiesText(int remaining, MorditasText text) =>
        remaining == 1 ? text("morditas.enemies_left_one") : text("morditas.enemies_left", remaining);

    private static string AbandonText(MorditasRules rules, MorditasText text) => rules.OutOfZoneSeconds > 0
        ? text("morditas.abandon", (int)Math.Ceiling(rules.OutOfZoneSeconds)) : null;

    private static string CentreText(in MorditasSample sample, MorditasText text) =>
        sample.CentreSteps > 0 && sample.CentreDirection != null
            ? text("morditas.centre", StepsText(sample.CentreSteps, text), text(sample.CentreDirection)) : null;

    private static string StepsText(int steps, MorditasText text) =>
        steps == 1 ? text("exploration.steps_one") : text("exploration.steps_many", steps);

    private struct Spoken
    {
        internal MorditasPhase Phase;
        internal int Kills, Charges, Remaining, Warned;
        internal bool Requirement, AllOut;
        internal bool? Inside;
        internal double CountAt;
    }
}
