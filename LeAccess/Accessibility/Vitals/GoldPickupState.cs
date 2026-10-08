namespace LeAccess.Accessibility.Vitals;

/// <summary>Turns gold counter readings into gains to announce: rises only, never at a vendor, after a silent baseline.</summary>
/// <remarks>See reference/vitals-api.md#gold-pickups-2026-10-08.</remarks>
internal sealed class GoldPickupState
{
    private long _tracker;
    private int? _gold;

    /// <summary>Gold gained and not yet spoken; it survives rejected speech and adds up until acknowledged.</summary>
    internal int Pending { get; private set; }

    // tracker identifies the counter: a new character, session or player is a new baseline, never a gain.
    internal int Observe(long tracker, int? gold, bool vendorOpen)
    {
        if (gold is null || tracker != _tracker || _gold is null)
        {
            Reset();
            _tracker = gold is null ? 0 : tracker;
            _gold = gold;
            return 0;
        }
        int previous = _gold.Value;
        _gold = gold;
        // Shops speak their own new total after a trade; spending or dropping gold is not a find.
        if (vendorOpen) Pending = 0;
        else if (gold.Value > previous) Pending += gold.Value - previous;
        return Pending;
    }

    internal void Acknowledge() => Pending = 0;

    internal void Reset()
    {
        _tracker = 0;
        _gold = null;
        Pending = 0;
    }
}
