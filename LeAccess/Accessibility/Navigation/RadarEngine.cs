using System;
using System.Numerics;

namespace LeAccess.Accessibility.Navigation;

public sealed class RadarEngine
{
    public const int DirectionCount = 4;
    // Two polls past a followed wall's line confirm that it ended, as the two-observation stationary rule does.
    private const int WallEndPolls = 2;

    private struct Sensor
    {
        public bool Baseline;
        public RadarState State, Candidate, Deferred, LastCue;
        public Vector2 CandidateOrigin, WallOrigin;
        public double CandidateSince, LastOpening;
        public bool HasLastOpening, WallKnown, TrackingWall;
        public float WallProjection;
        public int BeyondPolls;
    }

    private readonly Sensor[] _sensors = new Sensor[DirectionCount];
    private readonly Vector2[] _axes = new Vector2[DirectionCount];
    private bool _positioned;
    private Vector2 _previous;
    private double _lastTick, _lastMove;
    private long _session;

    public RadarSettings Settings { get; }
    public ulong Revision { get; private set; }
    public RadarResetReason LastResetReason { get; private set; }
    /// <summary>Polls skipped for a non-finite position, time or camera axis.</summary>
    public ulong InvalidTicks { get; private set; }
    /// <summary>Openings dropped because the same remembered wall was only farther away.</summary>
    public ulong SuppressedRetreats { get; private set; }
    public RadarState[] Events { get; } = new RadarState[DirectionCount];
    public RadarState[] States { get; } = new RadarState[DirectionCount];

    public RadarEngine(RadarSettings settings)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        if (!Positive(settings.OpenDepth) || !Positive(settings.CloseDepth)
            || settings.CloseDepth >= settings.OpenDepth || !Positive(settings.ReachDepth)
            || settings.ReachDepth < settings.OpenDepth || !Positive(settings.ConfirmDistance)
            || !Positive(settings.BoundaryRange) || !Positive(settings.TeleportDistance))
            throw new ArgumentException("Radar distances must be finite and form valid thresholds.", nameof(settings));
    }

    public void Reset(RadarResetReason reason = RadarResetReason.Explicit)
    {
        Revision++;
        LastResetReason = reason;
        Array.Clear(_sensors, 0, _sensors.Length);
        Array.Clear(Events, 0, Events.Length);
        Array.Clear(States, 0, States.Length);
        _positioned = false;
    }

    public RadarState[] Tick(Vector2 position, Vector2[] axes, RadarReading[] readings,
        double now, long session)
    {
        if (axes == null || axes.Length != DirectionCount)
            throw new ArgumentException("Provide four screen direction axes.", nameof(axes));
        if (readings == null || readings.Length != DirectionCount)
            throw new ArgumentException("Provide four direction readings.", nameof(readings));
        Array.Clear(Events, 0, Events.Length);
        if (!Finite(position) || !double.IsFinite(now) || !ValidAxes(axes))
        {
            // A transient bad frame cancels queued audio but keeps remembered walls; a long one ends as a poll gap.
            InvalidTicks++;
            Array.Fill(States, RadarState.Unknown);
            return Events;
        }

        RadarResetReason resetReason = !_positioned ? RadarResetReason.Initial
            : session != _session ? RadarResetReason.SessionChanged
            : now < _lastTick ? RadarResetReason.TimeReversed
            : now - _lastTick > RadarSettings.MaxGapSeconds ? RadarResetReason.PollGap
            : Vector2.Distance(position, _previous) >= Settings.TeleportDistance ? RadarResetReason.Teleport
            : AxesChanged(axes) ? RadarResetReason.CameraTurn : RadarResetReason.None;
        if (resetReason != RadarResetReason.None)
        {
            Reset(resetReason);
            _positioned = true;
            _session = session;
            _lastMove = now;
            for (int i = 0; i < DirectionCount; i++) _axes[i] = Vector2.Normalize(axes[i]);
        }
        else if (position != _previous) _lastMove = now;
        _previous = position;
        _lastTick = now;

        for (int i = 0; i < DirectionCount; i++)
        {
            ref Sensor sensor = ref _sensors[i];
            RadarReading reading = readings[i];
            if (!reading.Available || !float.IsFinite(reading.Depth) || reading.Depth < 0)
            {
                sensor.Candidate = sensor.Deferred = RadarState.Unknown;
                // Unknown cancels queued audio, while the private committed state survives.
                States[i] = RadarState.Unknown;
                continue;
            }
            RadarState observed = Classify(reading.Depth, sensor.State);
            Observe(ref sensor, observed, reading.Depth, position, _axes[i], now);
            // Follow the wall while it holds the direction closed, so walking along it and then away is no opening.
            if (sensor.State == RadarState.Blocked && observed == RadarState.Blocked && reading.Depth < Settings.ReachDepth)
                RememberWall(ref sensor, reading.Depth, position, _axes[i]);
            else if (sensor.State == RadarState.Open && sensor.TrackingWall)
                TrackWall(ref sensor, reading.Depth, position, _axes[i]);
            States[i] = sensor.State;
            Events[i] = Deliver(ref sensor, reading.Depth, position, _axes[i], now);
        }
        return Events;
    }

    private RadarState Classify(float depth, RadarState committed)
    {
        if (depth <= Settings.CloseDepth) return RadarState.Blocked;
        if (depth < Settings.OpenDepth && committed == RadarState.Blocked) return committed;
        return RadarState.Open;
    }

    private void Observe(ref Sensor sensor, RadarState observed, float depth,
        Vector2 position, Vector2 axis, double now)
    {
        if (!sensor.Baseline)
        {
            sensor.Baseline = true;
            sensor.State = observed;
            if (observed == RadarState.Blocked) RememberWall(ref sensor, depth, position, axis);
        }
        else if (observed == sensor.State) sensor.Candidate = RadarState.Unknown;
        else if (sensor.Candidate != observed)
        {
            sensor.Candidate = observed;
            sensor.CandidateOrigin = position;
            sensor.CandidateSince = now;
        }
        else if (Vector2.Distance(position, sensor.CandidateOrigin) >= Settings.ConfirmDistance
            || (now - _lastMove >= RadarSettings.StoppedSeconds
                && now - sensor.CandidateSince >= RadarSettings.StationaryConfirmSeconds))
        {
            sensor.State = sensor.Deferred = observed;
            sensor.Candidate = RadarState.Unknown;
            sensor.TrackingWall = false;
            if (observed == RadarState.Blocked)
                RememberWall(ref sensor, depth, position, axis);
        }
    }

    // Half the hysteresis band: how far past a remembered wall's line a probe must reach to be another place.
    private float RetreatMargin => (Settings.OpenDepth - Settings.CloseDepth) / 2;

    private bool BeyondWall(in Sensor sensor, float depth, Vector2 position, Vector2 axis, out bool nearby)
    {
        Vector2 offset = position - sensor.WallOrigin;
        nearby = Math.Abs(-offset.X * axis.Y + offset.Y * axis.X) <= Settings.BoundaryRange;
        return Vector2.Dot(position, axis) + depth >= sensor.WallProjection + RetreatMargin;
    }

    // After a silent retreat the same wall is followed, so its real end still sounds open.
    private void TrackWall(ref Sensor sensor, float depth, Vector2 position, Vector2 axis)
    {
        bool beyond = BeyondWall(sensor, depth, position, axis, out bool nearby);
        if (!nearby) { sensor.TrackingWall = false; return; }
        if (!beyond)
        {
            sensor.BeyondPolls = 0;
            if (depth < Settings.ReachDepth) RememberWall(ref sensor, depth, position, axis);
            return;
        }
        if (++sensor.BeyondPolls < WallEndPolls) return;
        sensor.TrackingWall = false;
        sensor.Deferred = RadarState.Open;
    }

    private static void RememberWall(ref Sensor sensor, float depth, Vector2 position, Vector2 axis)
    {
        sensor.WallKnown = true;
        sensor.WallOrigin = position;
        sensor.WallProjection = Vector2.Dot(position, axis) + depth;
    }

    private RadarState Deliver(ref Sensor sensor, float depth, Vector2 position,
        Vector2 axis, double now)
    {
        if (sensor.Deferred == RadarState.Unknown) return RadarState.Unknown;
        if (sensor.Deferred == RadarState.Open)
        {
            if (sensor.WallKnown && !BeyondWall(sensor, depth, position, axis, out bool nearby) && nearby)
            {
                // Drop retreats: holding them makes later wall-following sound like an opening.
                sensor.Deferred = RadarState.Unknown;
                sensor.TrackingWall = true;
                sensor.BeyondPolls = 0;
                SuppressedRetreats++;
                return RadarState.Unknown;
            }
            if (sensor.LastCue == sensor.Deferred)
            {
                sensor.Deferred = RadarState.Unknown;
                return RadarState.Unknown;
            }
            if (sensor.HasLastOpening && now - sensor.LastOpening < RadarSettings.OpeningIntervalSeconds)
                return RadarState.Unknown;
            sensor.HasLastOpening = true;
            sensor.LastOpening = now;
        }
        RadarState result = sensor.Deferred;
        sensor.LastCue = result;
        sensor.Deferred = RadarState.Unknown;
        return result;
    }

    private bool AxesChanged(Vector2[] axes)
    {
        if (!_positioned) return false;
        for (int i = 0; i < DirectionCount; i++)
            // Camera drift smaller than one confirmation step within the wall neighbourhood must not erase candidates.
            if (Vector2.Distance(_axes[i], Vector2.Normalize(axes[i])) * Settings.BoundaryRange
                >= Settings.ConfirmDistance) return true;
        return false;
    }

    private static bool ValidAxes(Vector2[] axes)
    {
        foreach (Vector2 axis in axes)
            if (!Finite(axis) || !Positive(axis.LengthSquared())) return false;
        return true;
    }

    private static bool Positive(float value) => float.IsFinite(value) && value > 0;
    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
