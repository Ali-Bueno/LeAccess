using System.Globalization;
using System.Numerics;
using System.Text;

namespace LeAccess.Accessibility.Navigation;

/// <summary>Recent readings for each radar event's log line, and flips of the live and shadow readings
/// (reference/navigation-radar.md#radar-trace-2026-10-11).</summary>
public sealed class RadarTrace
{
    /// <summary>Polls kept per direction: 0.4 s at the radar's poll rate, longer than an edge's two-poll confirmation.</summary>
    public const int Polls = 8;
    /// <summary>The previous cue in a direction reversed within this time counts as a flip (2026-10-10 log review window).</summary>
    public const double FlipSeconds = 1.5;

    private readonly float[,] _live = new float[RadarEngine.DirectionCount, Polls];
    private readonly float[,] _shadow = new float[RadarEngine.DirectionCount, Polls];
    private readonly bool[,] _continues = new bool[RadarEngine.DirectionCount, Polls];
    private readonly Sequence _liveCues = new(), _shadowCues = new();
    private int _count, _next, _polls, _pressed;

    private sealed class Sequence
    {
        public readonly RadarState[] States = new RadarState[RadarEngine.DirectionCount];
        public readonly double[] Times = new double[RadarEngine.DirectionCount];
        public int Events, Flips;

        // Seconds since the opposite cue in this direction when it is a flip, otherwise null.
        public double? Add(int direction, RadarState state, double now)
        {
            Events++;
            RadarState previous = States[direction];
            double since = now - Times[direction];
            States[direction] = state;
            Times[direction] = now;
            if (previous == RadarState.Unknown || previous == state || since < 0 || since > FlipSeconds) return null;
            Flips++;
            return since;
        }

        public void Reset()
        {
            System.Array.Clear(States, 0, States.Length);
            Events = Flips = 0;
        }
    }

    /// <summary>One poll; a missing shadow is NaN, and the edge within <paramref name="touching"/> counts as pressed.</summary>
    public void Record(RadarReading[] live, RadarReading[] shadow, float edge, float touching)
    {
        for (int i = 0; i < RadarEngine.DirectionCount; i++)
        {
            _live[i, _next] = live[i].Available ? live[i].Depth : float.NaN;
            _continues[i, _next] = live[i].Continues;
            _shadow[i, _next] = shadow != null && shadow[i].Available ? shadow[i].Depth : float.NaN;
        }
        _next = (_next + 1) % Polls;
        if (_count < Polls) _count++;
        _polls++;
        if (edge < touching) _pressed++;
    }

    /// <summary>Forgets readings from before a reset; the flip memory survives, as the player hears it.</summary>
    public void ClearReadings() => _count = _next = 0;

    public string Live(int direction, RadarState state, double now, Vector2 position, float edge)
    {
        var line = new StringBuilder("Reactive radar event: ");
        Head(line, direction, state, now, _liveCues.Add(direction, state, now));
        line.Append(" pos=").Append(F(position.X)).Append(',').Append(F(position.Y)).Append(" edge=").Append(F(edge));
        line.Append(" depth=");
        Series(line, _live, direction);
        line.Append(" cont=");
        for (int poll = 0; poll < _count; poll++) line.Append(_continues[direction, Index(poll)] ? '1' : '0');
        line.Append(" shadow=");
        Series(line, _shadow, direction);
        return line.ToString();
    }

    public string Shadow(int direction, RadarState state, double now)
    {
        var line = new StringBuilder("Reactive radar shadow: ");
        Head(line, direction, state, now, _shadowCues.Add(direction, state, now));
        return line.ToString();
    }

    /// <summary>Counts since the previous summary, appended to the five-second status.</summary>
    public string Summary()
    {
        string summary = $"trace: live_events={_liveCues.Events} live_flips={_liveCues.Flips} " +
            $"shadow_events={_shadowCues.Events} shadow_flips={_shadowCues.Flips} pressed_polls={_pressed}/{_polls}";
        _liveCues.Events = _liveCues.Flips = _shadowCues.Events = _shadowCues.Flips = _polls = _pressed = 0;
        return summary;
    }

    public void Reset()
    {
        ClearReadings();
        _liveCues.Reset();
        _shadowCues.Reset();
        _polls = _pressed = 0;
    }

    private static void Head(StringBuilder line, int direction, RadarState state, double now, double? flip)
    {
        line.Append("t=").Append(now.ToString("0.00", CultureInfo.InvariantCulture))
            .Append(" dir=").Append((RadarDirection)direction).Append(" state=").Append(state);
        if (flip is { } seconds) line.Append(" flip=").Append(seconds.ToString("0.00", CultureInfo.InvariantCulture));
    }

    // Oldest to newest.
    private void Series(StringBuilder line, float[,] values, int direction)
    {
        for (int poll = 0; poll < _count; poll++)
        {
            if (poll > 0) line.Append(',');
            line.Append(F(values[direction, Index(poll)]));
        }
    }

    private int Index(int poll) => (_next - _count + poll + Polls) % Polls;

    private static string F(float value) =>
        float.IsFinite(value) ? value.ToString("0.00", CultureInfo.InvariantCulture) : "-";
}
