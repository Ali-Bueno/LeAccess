namespace LeAccess.Accessibility.Npcs;

internal sealed class NpcNameAnnouncementState
{
    // Brief losses are range-edge flicker; the same NPC stays silent inside this gap.
    internal const double RepeatGapSeconds = 5;
    private readonly double _repeatGap;
    private long _session;
    private int _target;
    private string _candidate;
    private string _spoken;
    private double _lostAt = double.NaN;

    internal NpcNameAnnouncementState(double repeatGapSeconds = RepeatGapSeconds) => _repeatGap = repeatGapSeconds;

    internal bool HasPendingContent => !string.IsNullOrWhiteSpace(_candidate) && _candidate != _spoken;

    internal bool Observe(long session, int target, string name, bool canSpeak, double now, out string announcement)
    {
        announcement = null;
        if (session != _session || target != _target)
        {
            Reset();
            _session = session;
            _target = target;
        }
        if (!double.IsNaN(_lostAt) && now - _lostAt >= _repeatGap) _spoken = null;
        _lostAt = double.NaN;
        string current = name?.Trim();
        bool stable = current == _candidate;
        _candidate = current;
        if (!stable || !canSpeak || !HasPendingContent) return false;
        _spoken = current;
        announcement = current;
        return true;
    }

    // Target left range: keep the spoken memory for the repeat gap.
    internal void Lost(double now)
    {
        _candidate = null;
        if (double.IsNaN(_lostAt)) _lostAt = now;
    }

    internal void Reset() { _session = 0; _target = 0; _candidate = null; _spoken = null; _lostAt = double.NaN; }
}
