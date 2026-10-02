using System;

namespace LeAccess.Accessibility.Navigation;

public sealed class RadarCueQueue
{
    private readonly RadarState[] _pending = new RadarState[RadarEngine.DirectionCount];
    private int _next;

    public int PendingCount
    {
        get
        {
            int count = 0;
            foreach (RadarState state in _pending) if (state != RadarState.Unknown) count++;
            return count;
        }
    }

    public void Retain(RadarState[] states)
    {
        Validate(states);
        for (int i = 0; i < _pending.Length; i++)
            if (_pending[i] != states[i]) _pending[i] = RadarState.Unknown;
    }

    public void Enqueue(RadarState[] events)
    {
        Validate(events);
        for (int i = 0; i < _pending.Length; i++)
            if (events[i] != RadarState.Unknown) _pending[i] = events[i];
    }

    public bool TryTake(out RadarDirection direction, out RadarState state)
    {
        if (!TryPeek(out direction, out state)) return false;
        int chosen = (int)direction;
        _pending[chosen] = RadarState.Unknown;
        _next = (chosen + 1) % _pending.Length;
        return true;
    }

    public bool TryPeek(out RadarDirection direction, out RadarState state)
    {
        int chosen = -1;
        for (int offset = 0; offset < _pending.Length; offset++)
        {
            int i = (_next + offset) % _pending.Length;
            if (_pending[i] == RadarState.Unknown) continue;
            if (chosen < 0 || Priority(_pending[i]) > Priority(_pending[chosen])) chosen = i;
        }
        if (chosen < 0)
        {
            direction = default;
            state = RadarState.Unknown;
            return false;
        }
        direction = (RadarDirection)chosen;
        state = _pending[chosen];
        return true;
    }

    public void Clear()
    {
        Array.Clear(_pending, 0, _pending.Length);
        _next = 0;
    }

    private static int Priority(RadarState state) => state == RadarState.Blocked ? 1 : 0;

    private static void Validate(RadarState[] values)
    {
        if (values == null || values.Length != RadarEngine.DirectionCount)
            throw new ArgumentException("Provide four direction states.", nameof(values));
    }
}
