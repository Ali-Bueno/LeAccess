using System;

namespace LeAccess.Accessibility.Navigation;

internal sealed class RadarDiagnostics
{
    // One compact heartbeat per five seconds distinguishes a quiet scene from a stalled radar.
    private const double ReportSeconds = 5;
    private readonly int[] _resets = new int[Enum.GetValues<RadarResetReason>().Length];
    private double _nextReport;
    private int _polls, _generated, _submitted;

    public void Observe(RadarEngine engine, bool rebased)
    {
        _polls++;
        if (rebased) _resets[(int)engine.LastResetReason]++;
        foreach (var state in engine.Events)
            if (state != RadarState.Unknown) _generated++;
    }

    public void Submitted() => _submitted++;

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
        string result = $"Reactive radar status: polls={_polls} generated={_generated} submitted={_submitted} queued={pending} resets=[{resets.Trim()}] states={string.Join(",", engine.States)} {geometry} audio={audio()}";
        _polls = _generated = _submitted = 0;
        Array.Clear(_resets, 0, _resets.Length);
        return result;
    }

    public void Reset()
    {
        _polls = _generated = _submitted = 0;
        _nextReport = 0;
        Array.Clear(_resets, 0, _resets.Length);
    }
}
