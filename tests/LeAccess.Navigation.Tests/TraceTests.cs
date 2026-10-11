using System.Numerics;
using LeAccess.Accessibility.Navigation;
using static Program;

internal static class TraceTests
{
    internal static void Run()
    {
        var trace = new RadarTrace();
        for (int poll = 0; poll < RadarTrace.Polls + 2; poll++)
            trace.Record(Readings(poll), poll < 3 ? 0.01f : 1, 0.1f);
        string line = trace.Event((int)RadarDirection.Left, RadarState.Open, 10.25, new Vector2(1.5f, -2), 0.01f);
        Check(line.StartsWith("Reactive radar event: t=10.25 dir=Left state=Open pos=1.50,-2.00 edge=0.01 depth="),
            "An event line names time, direction, state, position and edge.");
        Check(line.Contains($"depth={Series(2)} "), "Only the newest polls are kept, oldest first.");
        Check(line.EndsWith(" cont=" + new string('1', RadarTrace.Polls)), "Continuation flags follow each poll.");
        Check(!line.Contains("flip="), "A first cue is no flip.");

        string flip = trace.Event((int)RadarDirection.Left, RadarState.Blocked, 11.0, Vector2.Zero, 1);
        Check(flip.Contains(" flip=0.75 "), "A reversal within the window is a flip with its interval.");
        Check(!trace.Event((int)RadarDirection.Left, RadarState.Blocked, 11.2, Vector2.Zero, 1).Contains("flip="),
            "The same state again is no flip.");
        Check(!trace.Event((int)RadarDirection.Left, RadarState.Open, 11.2 + RadarTrace.FlipSeconds + 0.1, Vector2.Zero, 1)
            .Contains("flip="), "A reversal after the window is no flip.");
        Check(!trace.Event((int)RadarDirection.Right, RadarState.Blocked, 13.0, Vector2.Zero, 1).Contains("flip="),
            "Directions keep their own memory.");

        Check(trace.Summary() == $"trace: events=5 flips=1 pressed_polls=3/{RadarTrace.Polls + 2}",
            "The summary counts events, flips and polls pressed against an edge.");
        Check(trace.Summary() == "trace: events=0 flips=0 pressed_polls=0/0", "Each summary starts a new window.");

        trace.ClearReadings();
        trace.Record(Readings(7), float.NaN, 0.1f);
        string after = trace.Event((int)RadarDirection.Up, RadarState.Open, 14, Vector2.Zero, float.NaN);
        Check(after.EndsWith(" edge=- depth=7.00 cont=1"), "After a reset only new polls show; unknown values print as -.");
        trace.Reset();
        Check(!trace.Event((int)RadarDirection.Up, RadarState.Blocked, 14.1, Vector2.Zero, 1).Contains("flip="),
            "A full reset forgets the previous cues.");
    }

    private static RadarReading[] Readings(int poll) =>
        Enumerable.Range(0, RadarEngine.DirectionCount).Select(_ => new RadarReading(poll, true)).ToArray();

    private static string Series(int first) =>
        string.Join(",", Enumerable.Range(first, RadarTrace.Polls).Select(value => $"{value}.00"));
}
