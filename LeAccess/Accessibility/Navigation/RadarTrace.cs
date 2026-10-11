using System.Globalization;
using System.Numerics;
using System.Text;

namespace LeAccess.Accessibility.Navigation;

/// <summary>Recent readings for each radar event's log line, and quick reversals of the cues
/// (reference/navigation-radar.md#radar-trace-2026-10-11).</summary>
public sealed class RadarTrace
{
    /// <summary>Polls kept per direction: 0.4 s at the radar's poll rate, longer than an edge's two-poll confirmation.</summary>
    public const int Polls = 8;
    /// <summary>The previous cue in a direction reversed within this time counts as a flip (2026-10-10 log review window).</summary>
    public const double FlipSeconds = 1.5;

    private readonly float[,] _depths = new float[RadarEngine.DirectionCount, Polls];
    private readonly bool[,] _continues = new bool[RadarEngine.DirectionCount, Polls];
    private readonly RadarState[] _lastStates = new RadarState[RadarEngine.DirectionCount];
    private readonly double[] _lastTimes = new double[RadarEngine.DirectionCount];
    private int _count, _next, _polls, _pressed, _events, _flips;

    /// <summary>One poll; the edge within <paramref name="touching"/> counts as pressed against it.</summary>
    public void Record(RadarReading[] readings, float edge, float touching)
    {
        for (int i = 0; i < RadarEngine.DirectionCount; i++)
        {
            _depths[i, _next] = readings[i].Available ? readings[i].Depth : float.NaN;
            _continues[i, _next] = readings[i].Continues;
        }
        _next = (_next + 1) % Polls;
        if (_count < Polls) _count++;
        _polls++;
        if (edge < touching) _pressed++;
    }

    /// <summary>Forgets readings from before a reset; the flip memory survives, as the player hears it.</summary>
    public void ClearReadings() => _count = _next = 0;

    public string Event(int direction, RadarState state, double now, Vector2 position, float edge)
    {
        var line = new StringBuilder("Reactive radar event: t=").Append(now.ToString("0.00", CultureInfo.InvariantCulture))
            .Append(" dir=").Append((RadarDirection)direction).Append(" state=").Append(state);
        if (Flip(direction, state, now) is { } seconds)
            line.Append(" flip=").Append(seconds.ToString("0.00", CultureInfo.InvariantCulture));
        line.Append(" pos=").Append(F(position.X)).Append(',').Append(F(position.Y)).Append(" edge=").Append(F(edge));
        line.Append(" depth=");
        for (int poll = 0; poll < _count; poll++)
        {
            if (poll > 0) line.Append(',');
            line.Append(F(_depths[direction, Index(poll)]));
        }
        line.Append(" cont=");
        for (int poll = 0; poll < _count; poll++) line.Append(_continues[direction, Index(poll)] ? '1' : '0');
        return line.ToString();
    }

    /// <summary>Counts since the previous summary, appended to the five-second status.</summary>
    public string Summary()
    {
        string summary = $"trace: events={_events} flips={_flips} pressed_polls={_pressed}/{_polls}";
        _events = _flips = _polls = _pressed = 0;
        return summary;
    }

    public void Reset()
    {
        ClearReadings();
        System.Array.Clear(_lastStates, 0, _lastStates.Length);
        _events = _flips = _polls = _pressed = 0;
    }

    // Seconds since the opposite cue in this direction when it is a flip, otherwise null.
    private double? Flip(int direction, RadarState state, double now)
    {
        _events++;
        RadarState previous = _lastStates[direction];
        double since = now - _lastTimes[direction];
        _lastStates[direction] = state;
        _lastTimes[direction] = now;
        if (previous == RadarState.Unknown || previous == state || since < 0 || since > FlipSeconds) return null;
        _flips++;
        return since;
    }

    // Oldest to newest.
    private int Index(int poll) => (_next - _count + poll + Polls) % Polls;

    private static string F(float value) =>
        float.IsFinite(value) ? value.ToString("0.00", CultureInfo.InvariantCulture) : "-";
}
