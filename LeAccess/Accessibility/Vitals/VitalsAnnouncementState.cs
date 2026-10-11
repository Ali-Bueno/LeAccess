using System;

namespace LeAccess.Accessibility.Vitals;

internal readonly record struct VitalsChange(int HealthPercent, int ManaPercent, bool Interrupt)
{
    internal bool HasContent => HealthPercent >= 0 || ManaPercent >= 0;
}

/// <summary>Which meters announce automatically and the smallest change in percent worth a new band announcement.</summary>
internal readonly record struct VitalsPolicy(bool Life, bool Mana, int LifeMinimumChange, int ManaMinimumChange)
{
    internal static VitalsPolicy Default => new(true, true, 0, 0);
}

internal sealed class VitalsAnnouncementState
{
    // D2R src/vitals.h: Steps and CriticalPercent; retain its strict downward boundaries.
    private static readonly int[] Bands = { 75, 50, 40, 30, 20, 10, 5, 4, 3, 2, 1 };
    private const int CriticalPercent = 10;
    // A one-band rise is regeneration between casts or hits; only clearer recoveries speak.
    private const int RecoveryBands = 2;
    private readonly Meter _health = new(routineLow: false);
    // Running low on mana is routine for casters, not danger: no interrupts, few steps below ten.
    private readonly Meter _mana = new(routineLow: true);

    internal VitalsChange Observe(int healthPercent, int manaPercent, bool acknowledge = true, VitalsPolicy? policy = null)
    {
        var rules = policy ?? VitalsPolicy.Default;
        int previousHealth = _health.Previous;
        // Critical life and empty mana are never held back by a minimum change.
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

    // Index into Bands; larger is lower, and Bands.Length means empty.
    private static int Band(int percent)
    {
        for (int i = 0; i < Bands.Length; i++)
            if (percent >= Bands[i]) return i;
        return Bands.Length;
    }

    private sealed class Meter
    {
        private readonly bool _routineLow;
        private int _band = -1;
        internal int Previous { get; private set; } = -1;

        internal Meter(bool routineLow) => _routineLow = routineLow;

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
            // Below ten a routine meter speaks its first step and empty only.
            bool routineLow = _routineLow && !baseline && percent < CriticalPercent &&
                Previous < CriticalPercent && (percent > 0 || Previous == 0);
            quiet |= routineLow;
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
