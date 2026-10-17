using System;
using LeAccess.Accessibility.Audio;

namespace LeAccess.Accessibility.Npcs;

internal sealed class NpcCueTiming
{
    internal const double PollSeconds = EntityCueCadence.PollSeconds;
    private long _session;
    private double _nextCue;

    internal bool TryTake(long session, double now, float distance, float range, float speed = 1)
    {
        if (!IsDue(session, now, distance, range)) return false;
        if (session != _session) { Reset(); _session = session; }
        if (now < _nextCue) return false;
        _nextCue = now + EntityCueCadence.Interval(distance, range, speed);
        return true;
    }

    internal bool IsDue(long session, double now, float distance, float range) =>
        double.IsFinite(now) && float.IsFinite(distance) && distance >= 0 &&
        float.IsFinite(range) && range > 0 && distance <= range && (session != _session || now >= _nextCue);

    internal void Reset() { _session = 0; _nextCue = 0; }
}
