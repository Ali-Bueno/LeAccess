using System;
using System.Collections.Generic;

namespace LeAccess.Accessibility.Vitals;

internal readonly record struct VitalsChange(int HealthPercent, int ManaPercent, bool Interrupt)
{
    internal bool HasContent => HealthPercent >= 0 || ManaPercent >= 0;
}

/// <summary>Which meters announce automatically, the smallest change in percent worth a new band announcement,
/// and each meter's steps (null keeps its default).</summary>
internal readonly record struct VitalsPolicy(bool Life, bool Mana, int LifeMinimumChange, int ManaMinimumChange,
    VitalsSteps? LifeSteps = null, VitalsSteps? ManaSteps = null)
{
    internal static VitalsPolicy Default => new(true, true, 0, 0);
}

/// <summary>A meter speaks every Every percent, every 10 below TensBelow, every 5 below FivesBelow and every 1
/// below OnesBelow (0 turns a range off); the finest step that applies wins.</summary>
internal readonly record struct VitalsSteps(int Every, int TensBelow, int FivesBelow, int OnesBelow)
{
    // D2R src/vitals.h Steps: 75, 50, 40, 30, 20, 10, 5, 4, 3, 2, 1.
    internal static VitalsSteps Life => new(25, 50, 10, 5);
    // Running low on mana is routine for casters: below ten only empty speaks.
    internal static VitalsSteps Mana => new(25, 50, 0, 0);

    internal int StepAt(int percent)
    {
        int step = Math.Max(1, Every);
        if (percent < TensBelow) step = Math.Min(step, 10);
        if (percent < FivesBelow) step = Math.Min(step, 5);
        return percent < OnesBelow ? 1 : step;
    }

    /// <summary>Percents a falling meter speaks below, highest first; full is not one, 1 always is, so empty is its own band.</summary>
    internal int[] Bands()
    {
        var bands = new List<int>();
        for (int percent = 99; percent >= 1; percent--)
            if (percent == 1 || percent % StepAt(percent) == 0) bands.Add(percent);
        return bands.ToArray();
    }
}

internal sealed class VitalsAnnouncementState
{
    // D2R src/vitals.h CriticalPercent; bands keep its strict downward boundaries.
    private const int CriticalPercent = 10;
    // A one-band rise is regeneration between casts or hits; only clearer recoveries speak.
    private const int RecoveryBands = 2;
    private readonly Meter _health = new(VitalsSteps.Life);
    // Running low on mana is routine for casters, not danger: it never interrupts.
    private readonly Meter _mana = new(VitalsSteps.Mana);

    internal VitalsChange Observe(int healthPercent, int manaPercent, bool acknowledge = true, VitalsPolicy? policy = null)
    {
        var rules = policy ?? VitalsPolicy.Default;
        int previousHealth = _health.Previous;
        // Critical life and empty mana are never held back by a minimum change.
        _health.Use(rules.LifeSteps ?? VitalsSteps.Life);
        _mana.Use(rules.ManaSteps ?? VitalsSteps.Mana);
        int health = _health.Observe(healthPercent, acknowledge, rules.Life, rules.LifeMinimumChange, CriticalPercent);
        int mana = _mana.Observe(manaPercent, acknowledge, rules.Mana, rules.ManaMinimumChange, 0);
        bool interrupt = health >= 0 && health <= CriticalPercent && previousHealth > health;
        return new VitalsChange(health, mana, interrupt);
    }

    internal void Acknowledge(int healthPercent, int manaPercent)
    {
        _health.Acknowledge(healthPercent);
        _mana.Acknowledge(manaPercent);
    }

    internal void Reset() { _health.Reset(); _mana.Reset(); }

    internal static int Percent(double current, double maximum)
    {
        if (!double.IsFinite(current) || !double.IsFinite(maximum) || maximum <= 0) return -1;
        if (current <= 0) return 0;
        if (current >= maximum) return 100;
        double percent = current <= double.MaxValue / 100 ? current * 100 / maximum : current / maximum * 100;
        return (int)Math.Floor(percent);
    }

    private sealed class Meter
    {
        private VitalsSteps _steps;
        private int[] _bands;
        private int _band = -1;
        internal int Previous { get; private set; } = -1;

        internal Meter(VitalsSteps steps) => Use(steps);

        // New steps keep the last accepted reading, placed in the new bands.
        internal void Use(VitalsSteps steps)
        {
            if (_bands != null && steps == _steps) return;
            _steps = steps;
            _bands = steps.Bands();
            if (_band >= 0) _band = Band(Previous);
        }

        // Index into the bands; larger is lower, and the band count means empty.
        private int Band(int percent)
        {
            for (int i = 0; i < _bands.Length; i++)
                if (percent >= _bands[i]) return i;
            return _bands.Length;
        }

        internal int Observe(int percent, bool acknowledge, bool enabled, int minimumChange, int alwaysSpeakAtOrBelow)
        {
            if (percent < 0) return -1;
            percent = Math.Min(percent, 100);
            // A silenced meter still follows the value, so enabling it later does not replay old crossings.
            if (!enabled) { Acknowledge(percent); return -1; }
            int band = Band(percent);
            bool baseline = _band < 0;
            bool unchanged = band == _band;
            // A quiet small rise keeps the spoken band, so falling back into it stays silent.
            bool quiet = unchanged || band < _band && _band - band < RecoveryBands;
            // Held back until the value has moved this far from the last accepted reading.
            quiet |= minimumChange > 0 && !baseline && percent > alwaysSpeakAtOrBelow && Math.Abs(percent - Previous) < minimumChange;
            if (baseline || unchanged || !quiet && acknowledge) Acknowledge(percent);
            // Changed bands remain relative to accepted output after rejection.
            // The first live sample is always a silent baseline.
            return baseline || quiet ? -1 : percent;
        }

        internal void Acknowledge(int percent)
        {
            if (percent < 0) return;
            Previous = Math.Min(percent, 100);
            _band = Band(Previous);
        }

        internal void Reset() { _band = -1; Previous = -1; }
    }
}
