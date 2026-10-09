using System;

namespace LeAccess.Accessibility.Navigation;

internal sealed class RadarDiagnostics
{
    // One compact heartbeat per five seconds distinguishes a quiet scene from a stalled radar.
    private const double ReportSeconds = 5;
    private readonly int[] _resets = new int[Enum.GetValues<RadarResetReason>().Length];
    private double _nextReport, _maxWait;
    private double? _waitingSince;
    private int _polls, _generated, _submitted;
    private ulong _invalidReported, _retreatsReported;

    public void Observe(RadarEngine engine, bool rebased)
    {
        _polls++;
        if (rebased) _resets[(int)engine.LastResetReason]++;
        foreach (var state in engine.Events)
            if (state != RadarState.Unknown) _generated++;
    }

    // Time from a cue becoming pending to its playback, including waits for the radar's own output.
    public void Pending(double now, bool pending)
    {
        if (!pending) _waitingSince = null;
        else _waitingSince ??= now;
    }

    public void Submitted(double now, bool stillPending)
    {
        _submitted++;
        if (_waitingSince is { } since && now >= since) _maxWait = Math.Max(_maxWait, now - since);
        _waitingSince = stillPending ? now : null;
    }

    public string Report(double now, RadarWorldSnapshot snapshot, RadarEngine engine, int pending, Func<string> audio)
    {
        if (now < _nextReport) return null;
        _nextReport = now + ReportSeconds;
        string resets = "";
        for (int i = 0; i < _resets.Length; i++)
            if (_resets[i] != 0) resets += $"{(RadarResetReason)i}:{_resets[i]} ";
        var settings = snapshot.Settings;
        var depths = snapshot.Readings;
        string geometry = FormattableString.Invariant($"position={snapshot.Position.X:F2},{snapshot.Position.Y:F2} body_radius={snapshot.BodyRadius:F3} agent_radius={snapshot.AgentRadius:F3} agent_type={snapshot.AgentTypeId} mask={snapshot.AreaMask} close={settings.CloseDepth:F2} open={settings.OpenDepth:F2} reach={settings.ReachDepth:F2} depths_ULRD={depths[0].Depth:F2},{depths[1].Depth:F2},{depths[2].Depth:F2},{depths[3].Depth:F2}");
        string result = $"Reactive radar status: polls={_polls} generated={_generated} submitted={_submitted} queued={pending} max_wait_ms={_maxWait * 1000:F0} resets=[{resets.Trim()}] invalid={Since(engine.InvalidTicks, _invalidReported)} retreats={Since(engine.SuppressedRetreats, _retreatsReported)} states={string.Join(",", engine.States)} {geometry} audio={audio()}";
        _invalidReported = engine.InvalidTicks;
        _retreatsReported = engine.SuppressedRetreats;
        _polls = _generated = _submitted = 0;
        _maxWait = 0;
        Array.Clear(_resets, 0, _resets.Length);
        return result;
    }

    // A replaced engine restarts its counters.
    private static ulong Since(ulong total, ulong reported) => total >= reported ? total - reported : total;

    public void Reset()
    {
        _polls = _generated = _submitted = 0;
        _nextReport = _maxWait = 0;
        _waitingSince = null;
        _invalidReported = _retreatsReported = 0;
        Array.Clear(_resets, 0, _resets.Length);
    }
}
