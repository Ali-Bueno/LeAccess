using System;
using System.Numerics;

namespace LeAccess.Accessibility.Navigation;

public sealed class RadarEngine
{
    public const int DirectionCount = 4;

    private struct Sensor
    {
        public bool Baseline;
        public RadarState State, Candidate, Deferred, LastCue;
        public Vector2 CandidateOrigin, TrackOrigin;
        public double CandidateSince, LastOpening;
        public bool HasLastOpening, HasPrevious, EdgeSeen, TrackingWall;
        public float PreviousReach;
        public int EdgePolls;
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
    /// <summary>Openings dropped because the wall only receded (backing away, a diagonal wall), with no edge passed.</summary>
    public ulong SuppressedRetreats { get; private set; }
    public RadarState[] Events { get; } = new RadarState[DirectionCount];
    public RadarState[] States { get; } = new RadarState[DirectionCount];

    public RadarEngine(RadarSettings settings)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        if (!Positive(settings.OpenDepth) || !Positive(settings.CloseDepth)
            || settings.CloseDepth >= settings.OpenDepth || !Positive(settings.PassageDepth)
            || settings.PassageDepth < settings.OpenDepth || !Positive(settings.ReachDepth)
            || settings.ReachDepth < settings.PassageDepth || !Positive(settings.EdgeJump)
            || !Positive(settings.ConfirmDistance)
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
            // A transient bad frame cancels queued audio but keeps the walls; a long one ends as a poll gap.
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
                sensor.HasPrevious = false;
                // Unknown cancels queued audio, while the private committed state survives.
                States[i] = RadarState.Unknown;
                continue;
            }
            int step = Step(ref sensor, reading.Depth, position, _axes[i]);
            if (sensor.State == RadarState.Blocked)
            {
                // The latest discontinuity decides: past an edge the probe looks through a gap, back at a wall it does not.
                if (step > 0) sensor.EdgeSeen = true;
                else if (step < 0) sensor.EdgeSeen = false;
            }
            else if (sensor.State == RadarState.Open && sensor.TrackingWall) TrackWall(ref sensor, step, position);
            RadarState observed = Classify(reading, sensor.State);
            Observe(ref sensor, observed, now, position);
            States[i] = sensor.State;
            Events[i] = Deliver(ref sensor, now);
        }
        return Events;
    }

    // +1 when the probe tip jumped away by an edge's depth since the last poll, -1 when it jumped closer, 0 otherwise.
    private int Step(ref Sensor sensor, float depth, Vector2 position, Vector2 axis)
    {
        float reach = Vector2.Dot(position, axis) + depth;
        float change = sensor.HasPrevious ? reach - sensor.PreviousReach : 0;
        sensor.PreviousReach = reach;
        sensor.HasPrevious = true;
        return change >= Settings.EdgeJump ? 1 : change <= -Settings.EdgeJump ? -1 : 0;
    }

    // A short opening needs the space to go on beyond it; a passage-deep one does not.
    private RadarState Classify(RadarReading reading, RadarState committed)
    {
        if (reading.Depth <= Settings.CloseDepth) return RadarState.Blocked;
        bool opening = reading.Depth >= Settings.PassageDepth || reading.Depth >= Settings.OpenDepth && reading.Continues;
        if (!opening && committed == RadarState.Blocked) return committed;
        return RadarState.Open;
    }

    private void Observe(ref Sensor sensor, RadarState observed, double now, Vector2 position)
    {
        if (!sensor.Baseline)
        {
            sensor.Baseline = true;
            sensor.State = observed;
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
            if (observed == RadarState.Blocked) sensor.EdgeSeen = false;
        }
    }

    // Two polls after an edge confirm that the followed wall ended, as the two-observation stationary rule does.
    private const int WallEndPolls = 2;

    // After a silent retreat the same wall is followed, so its real end still sounds open.
    private void TrackWall(ref Sensor sensor, int step, Vector2 position)
    {
        if (Vector2.Distance(position, sensor.TrackOrigin) > Settings.BoundaryRange) { sensor.TrackingWall = false; return; }
        if (step < 0) { sensor.EdgePolls = 0; return; }
        if (step > 0) sensor.EdgePolls = 1;
        else if (sensor.EdgePolls > 0) sensor.EdgePolls++;
        if (sensor.EdgePolls < WallEndPolls) return;
        sensor.TrackingWall = false;
        sensor.EdgeSeen = true;
        sensor.Deferred = RadarState.Open;
    }

    private RadarState Deliver(ref Sensor sensor, double now)
    {
        if (sensor.Deferred == RadarState.Unknown) return RadarState.Unknown;
        if (sensor.Deferred == RadarState.Open)
        {
            if (!sensor.EdgeSeen)
            {
                // Drop retreats: the wall only receded, so there is no gap; follow it for its real end.
                sensor.Deferred = RadarState.Unknown;
                sensor.TrackingWall = true;
                sensor.TrackOrigin = _previous;
                sensor.EdgePolls = 0;
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
            sensor.EdgeSeen = false;
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
