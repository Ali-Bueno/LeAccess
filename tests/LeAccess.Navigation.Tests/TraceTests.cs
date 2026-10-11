using System.Numerics;
using LeAccess.Accessibility.Navigation;
using static Program;

internal static class TraceTests
{
    internal static void Run()
    {
        var trace = new RadarTrace();
        for (int poll = 0; poll < RadarTrace.Polls + 2; poll++)
            trace.Record(Readings(poll), poll == 0 ? null : new[] { Readings(poll + 100), Readings(poll + 200) },
                poll < 3 ? 0.01f : 1, 0.1f);
        string line = trace.Live((int)RadarDirection.Left, RadarState.Open, 10.25, new Vector2(1.5f, -2), 0.01f);
        Check(line.StartsWith("Reactive radar event: t=10.25 dir=Left state=Open pos=1.50,-2.00 edge=0.01 depth="),
            "An event line names time, direction, state, position and edge.");
        Check(line.Contains($"depth={Series(2)}") && line.Contains($" origin={Series(102)} ") && line.EndsWith($" lanes={Series(202)}"),
            "Only the newest polls are kept, oldest first, for the live reading and each shadow.");
        Check(line.Contains(" cont=" + new string('1', RadarTrace.Polls) + " "), "Continuation flags follow each poll.");
        Check(!line.Contains("flip="), "A first cue is no flip.");

        string flip = trace.Live((int)RadarDirection.Left, RadarState.Blocked, 11.0, Vector2.Zero, 1);
        Check(flip.Contains(" flip=0.75 "), "A reversal within the window is a flip with its interval.");
        Check(!trace.Live((int)RadarDirection.Left, RadarState.Blocked, 11.2, Vector2.Zero, 1).Contains("flip="),
            "The same state again is no flip.");
        Check(!trace.Live((int)RadarDirection.Left, RadarState.Open, 11.2 + RadarTrace.FlipSeconds + 0.1, Vector2.Zero, 1)
            .Contains("flip="), "A reversal after the window is no flip.");
        Check(!trace.Live((int)RadarDirection.Right, RadarState.Blocked, 13.0, Vector2.Zero, 1).Contains("flip="),
            "Directions keep their own memory.");
        Check(trace.Shadow(RadarShadowVariant.Origin, (int)RadarDirection.Up, RadarState.Open, 13.1)
            == "Reactive radar shadow origin: t=13.10 dir=Up state=Open", "A shadow line is short and names its variant.");
        Check(trace.Shadow(RadarShadowVariant.Origin, (int)RadarDirection.Up, RadarState.Blocked, 13.5).EndsWith(" flip=0.40"),
            "Shadow flips are counted too.");
        Check(!trace.Shadow(RadarShadowVariant.Lanes, (int)RadarDirection.Up, RadarState.Blocked, 13.6).Contains("flip="),
            "Each variant keeps its own memory.");

        string summary = trace.Summary();
        Check(summary == "trace: live_events=5 live_flips=1 origin_events=2 origin_flips=1 lanes_events=1 lanes_flips=0 " +
            $"pressed_polls=3/{RadarTrace.Polls + 2}", "The summary counts events, flips and polls pressed against an edge.");
        Check(trace.Summary() == "trace: live_events=0 live_flips=0 origin_events=0 origin_flips=0 lanes_events=0 lanes_flips=0 " +
            "pressed_polls=0/0", "Each summary starts a new window.");

        trace.ClearReadings();
        trace.Record(Readings(7), null, float.NaN, 0.1f);
        string after = trace.Live((int)RadarDirection.Up, RadarState.Open, 14, Vector2.Zero, float.NaN);
        Check(after.EndsWith(" edge=- depth=7.00 cont=1 origin=- lanes=-"),
            "After a reset only new polls show; unknown values print as -.");
    }

    private static RadarReading[] Readings(int poll) =>
        Enumerable.Range(0, RadarEngine.DirectionCount).Select(_ => new RadarReading(poll, true)).ToArray();

    private static string Series(int first) =>
        string.Join(",", Enumerable.Range(first, RadarTrace.Polls).Select(value => $"{value}.00"));
}
