using System;

namespace LeAccess.Accessibility.Npcs;

internal sealed class NpcCueTiming
{
    // D2R entity_cues.h: NPC pulses accelerate with proximity, polled every 100 ms.
    internal const double PollSeconds = 0.1;
    internal const double NearIntervalSeconds = 0.15;
    internal const double FarIntervalSeconds = 0.45;
    private long _session;
    private double _nextCue;

    internal bool TryTake(long session, double now, float distance, float range)
    {
        if (!IsDue(session, now, distance, range)) return false;
        if (session != _session) { Reset(); _session = session; }
        if (now < _nextCue) return false;
        _nextCue = now + NearIntervalSeconds + (FarIntervalSeconds - NearIntervalSeconds) * distance / range;
        return true;
    }

    internal bool IsDue(long session, double now, float distance, float range) =>
        double.IsFinite(now) && float.IsFinite(distance) && distance >= 0 &&
        float.IsFinite(range) && range > 0 && distance <= range && (session != _session || now >= _nextCue);

    internal void Reset() { _session = 0; _nextCue = 0; }
}
