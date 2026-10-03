using System.Numerics;
using LeAccess.Accessibility.Navigation.Guidance;

internal static partial class Program
{
    private static PassiveGuidanceObservation Sample(float distance = 12) => new(1, 10, "Tracked objective", true,
        Vector3.Zero, new(0, 0, distance), new(0, 0, distance), Vector3.UnitZ, Vector3.UnitX, distance, 1, true);

    private static void StateTests()
    {
        var state = new PassiveGuidanceState();
        var sample = Sample();
        Check(state.Observe(sample, 0), "verified route accepted");
        Check(!state.PulseDue(0.5) && !state.TryTakeSpeech(0.5, out _), "new target settles before speech or sound");
        Check(state.Observe(sample, 1), "same target remains available");
        Check(state.TryTakeSpeech(1, out var speech) && speech.Kind == PassiveGuidanceSpeechKind.Target &&
            speech.Name == sample.Name && speech.Direction.EndsWith(".up"), "destination identified with next route segment");
        Check(state.PulseDue(1), "stationary target still sounds");
        state.PulsePlayed(1);
        Check(!state.PulseDue(1.1) && state.PulseDue(1 + state.PulseInterval), "pulses use the distance cadence");
        Check(state.Pan == 0 && state.Forward == 1, "forward path centered");
        for (int now = 2; now < 30; now++)
        {
            state.Observe(sample, now);
            Check(!state.TryTakeSpeech(now, out _), "unchanged stationary target never repeats speech");
        }
        state.Suspend();
        Check(!state.PulseDue(50) && !state.TryTakeSpeech(50, out _), "suspension cancels outputs");
        state.Observe(sample, 50);
        Check(!state.PulseDue(50), "resuming revalidates before sound");
        state.Observe(sample, 51);
        Check(state.PulseDue(51) && !state.TryTakeSpeech(51, out _), "menu return resumes sound without repeated target speech");

        var right = sample with { Next = new(10, 0, 0) };
        state.Observe(right, 60);
        Check(!state.TryTakeSpeech(60, out _), "major direction change must settle");
        state.Observe(right, 61);
        Check(state.TryTakeSpeech(61, out speech) && speech.Kind == PassiveGuidanceSpeechKind.Turn &&
            speech.Direction.EndsWith(".right"), "stable major turn spoken once");
        Check(state.Pan == 1 && state.Forward == 0, "right turn panned right");
        state.Observe(sample, 61.5);
        Check(!state.TryTakeSpeech(62.5, out _), "turns respect global narration gap");
        state.Observe(right, 67);
        Check(!state.TryTakeSpeech(68, out _), "obsolete pending turn disappears if route returns");
        state.Observe(sample with { Next = new(10, 0, 10) }, 70);
        Check(!state.TryTakeSpeech(72, out _), "small diagonal change remains sound-only");

        var near = sample with { Distance = 0.8f, Next = Vector3.Zero };
        state.Observe(near, 80);
        Check(state.NearMarker && !state.PulseDue(80), "near marker pauses repeated sound");
        Check(state.TryTakeSpeech(80, out speech) && speech.Kind == PassiveGuidanceSpeechKind.NearMarker,
            "near speech represents marker proximity only");
        state.Observe(near, 90);
        Check(!state.TryTakeSpeech(90, out _), "near speech never repeats while stationary");
        state.Observe(sample with { Distance = 1.8f }, 91);
        Check(state.NearMarker && !state.PulseDue(91), "arrival hysteresis rejects edge chatter");
        state.Observe(sample with { Distance = 2.1f }, 92);
        Check(!state.NearMarker && state.PulseDue(92), "leaving marker restores orientation");
        state.Observe(near, 100);
        Check(state.TryTakeSpeech(100, out speech) && speech.Kind == PassiveGuidanceSpeechKind.NearMarker,
            "reentering marker can be announced");

        state.Observe(sample with { TargetId = 11, Name = "New target" }, 101);
        Check(!state.TryTakeSpeech(102, out _), "changing target retains narration pacing");
        state.Observe(sample with { TargetId = 12, Name = "Current target" }, 102);
        Check(state.TryTakeSpeech(106, out speech) && speech.Name == "Current target", "pending target always uses current observation");
        state.Observe(sample with { Session = 2 }, 107);
        Check(state.TryTakeSpeech(108, out speech) && speech.Kind == PassiveGuidanceSpeechKind.Target, "new scene or player resets target identity");
        state.Observe(sample with { Session = 2, Name = "Changed objective" }, 109);
        Check(state.TryTakeSpeech(115, out speech) && speech.Name == "Changed objective", "same marker with changed task named again");
        state.Observe(sample, 0);
        Check(state.TryTakeSpeech(1, out _), "clock rollback resets timers");

        foreach (var invalid in new[]
        {
            sample with { Complete = false }, sample with { Name = " " }, sample with { BodyWidth = 0 },
            sample with { Distance = float.NaN }, sample with { Distance = -1 }, sample with { BodyWidth = float.PositiveInfinity },
            sample with { Player = new(float.NaN, 0, 0) }, sample with { Destination = new(0, float.PositiveInfinity, 0) },
            sample with { Next = new(float.NaN, 0, 0) }, sample with { Forward = Vector3.Zero }, sample with { Right = Vector3.Zero }
        })
        {
            Check(!state.Observe(invalid, 200), "unverified or invalid route rejected");
            Check(!state.PulseDue(200) && !state.TryTakeSpeech(200, out _), "invalid observations cancel outputs");
        }
        Check(!state.Observe(sample, double.NaN), "nonfinite clock rejected");

        double previousInterval = 0;
        for (float distance = 1.1f; distance <= 50; distance += 0.5f)
        {
            state.Observe(Sample(distance), distance);
            Check(state.PulseInterval >= previousInterval, "cadence accelerates monotonically on approach");
            Check(state.PulseInterval >= PassiveGuidanceState.NearPulseSeconds && state.PulseInterval <= PassiveGuidanceState.FarPulseSeconds,
                "pulse cadence bounded");
            previousInterval = state.PulseInterval;
        }
        state.Observe(sample with { Next = new(0, 0, -5) }, 500);
        Check(state.Pan == 0 && state.Forward == -1, "rear path retains separate forward-back audio information");
        state.Observe(sample with { Forward = Vector3.UnitX, Right = -Vector3.UnitZ }, 501);
        Check(state.Pan == -1, "panning follows camera screen axes");
    }
}
