using System;

namespace LeAccess.Accessibility.Vitals;

internal readonly record struct VitalsChange(int HealthPercent, int ManaPercent, bool Interrupt)
{
    internal bool HasContent => HealthPercent >= 0 || ManaPercent >= 0;
}

internal sealed class VitalsAnnouncementState
{
    // D2R src/vitals.h: Steps and CriticalPercent; retain its strict downward boundaries.
    private static readonly int[] Bands = { 75, 50, 40, 30, 20, 10, 5, 4, 3, 2, 1 };
    private const int CriticalPercent = 10;
    private readonly Meter _health = new();
    private readonly Meter _mana = new();

    internal VitalsChange Observe(int healthPercent, int manaPercent)
    {
        int previousHealth = _health.Previous, previousMana = _mana.Previous;
        int health = _health.Observe(healthPercent), mana = _mana.Observe(manaPercent);
        bool interrupt = health >= 0 && health <= CriticalPercent && previousHealth > health ||
            mana >= 0 && mana <= CriticalPercent && previousMana > mana;
        return new VitalsChange(health, mana, interrupt);
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

    private static int Band(int percent)
    {
        foreach (int boundary in Bands)
            if (percent >= boundary) return boundary;
        return 0;
    }

    private sealed class Meter
    {
        private int _band = -1;
        internal int Previous { get; private set; } = -1;

        internal int Observe(int percent)
        {
            if (percent < 0) return -1;
            percent = Math.Min(percent, 100);
            int band = Band(percent);
            bool baseline = _band < 0;
            Previous = percent;
            if (band == _band) return -1;
            _band = band;
            return baseline ? -1 : percent;
        }

        internal void Reset() { _band = -1; Previous = -1; }
    }
}
