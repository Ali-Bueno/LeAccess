using System.Numerics;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Navigation;
using static Program;

internal static class BumpTests
{
    private const float Step = 0.3f;
    private static readonly Vector2 North = new(0, 1), East = new(1, 0);

    internal static void Run()
    {
        WalkingThenStopping();
        StalledWithoutWall();
        OneBumpPerContact();
        CornerTurn();
        ReleaseAndGaps();
        Sound();
    }

    // Polls at the radar's rate from time `start`, moving `speed` units per second along `push`.
    private static int Run(WallBumpDetector detector, ref Vector2 position, Vector2 push, float speed, bool contact,
        double start, double seconds, out double end)
    {
        int bumps = 0;
        double now = start;
        for (; now < start + seconds; now += RadarSettings.PollSeconds)
        {
            position += push * speed * (float)RadarSettings.PollSeconds;
            if (detector.Tick(position, push, contact, now)) bumps++;
        }
        end = now;
        return bumps;
    }

    private static void WalkingThenStopping()
    {
        var detector = new WallBumpDetector(Step);
        var position = Vector2.Zero;
        Check(Run(detector, ref position, North, 5, false, 0, 1, out double now) == 0, "Walking freely never bumps.");
        Check(Run(detector, ref position, North, 5, true, now, 0.5, out now) == 0,
            "Walking up to a wall that is still giving way does not bump.");
        int bumps = Run(detector, ref position, North, 0, true, now, 1, out _);
        Check(bumps == 1, "Stopping against the wall bumps once.");
        Check(detector.Pressing, "The player stays pressed against the wall.");

        detector = new WallBumpDetector(Step);
        position = Vector2.Zero;
        Run(detector, ref position, North, 0, true, 0, RadarSettings.StoppedSeconds - RadarSettings.PollSeconds, out now);
        Check(!detector.Stalled, "Shorter than the stopped time is not a stall yet.");
        Check(Run(detector, ref position, North, 0, true, now, 2 * RadarSettings.PollSeconds, out _) == 1,
            "The stopped time later the push is a bump.");
        // Sliding along a wall at walking pace keeps moving: the footsteps are real.
        detector = new WallBumpDetector(Step);
        position = Vector2.Zero;
        Check(Run(detector, ref position, Vector2.Normalize(new Vector2(1, 1)), 3, true, 0, 2, out _) == 0,
            "Sliding along a wall does not bump.");
        Check(!detector.Pressing, "Sliding is not pressing.");
    }

    private static void StalledWithoutWall()
    {
        var detector = new WallBumpDetector(Step);
        var position = Vector2.Zero;
        Check(Run(detector, ref position, North, 0, false, 0, 1, out _) == 0, "Blocked by an enemy or a stun, not a wall: silent.");
        Check(detector.Stalled && !detector.Pressing, "The stall is still visible for diagnostics.");
    }

    private static void OneBumpPerContact()
    {
        var detector = new WallBumpDetector(Step);
        var position = Vector2.Zero;
        Check(Run(detector, ref position, North, 0, true, 0, 3, out double now) == 1, "Holding against the wall stays one bump.");
        Check(Run(detector, ref position, -North, 5, false, now, 0.3, out now) == 0, "Backing off is silent.");
        Check(Run(detector, ref position, North, 5, false, now, 0.25, out now) == 0, "Walking back toward it is silent.");
        Check(Run(detector, ref position, North, 0, true, now, 1, out _) == 1, "Hitting the wall again after moving bumps again.");
    }

    private static void CornerTurn()
    {
        var detector = new WallBumpDetector(Step);
        var position = Vector2.Zero;
        Check(Run(detector, ref position, North, 0, true, 0, 1, out double now) == 1, "First side of a corner bumps.");
        var slightly = Vector2.Normalize(new Vector2(0.2f, 1));
        Check(Run(detector, ref position, slightly, 0, true, now, 1, out now) == 0, "A slight stick wobble is the same wall.");
        Check(Run(detector, ref position, East, 0, true, now, 0.1, out now) == 1, "Turning into the corner's other side bumps.");
        Check(Run(detector, ref position, North, 0, true, now, 0.1, out now) == 0,
            "Turning straight back within the opening interval is held back.");
        Check(Run(detector, ref position, North, 0, true, now, 1, out _) == 1, "After the interval the other side bumps again.");
    }

    private static void ReleaseAndGaps()
    {
        var detector = new WallBumpDetector(Step);
        var position = Vector2.Zero;
        Check(Run(detector, ref position, North, 0, true, 0, 1, out double now) == 1, "Pressing bumps.");
        Check(!detector.Tick(position, Vector2.Zero, true, now), "Releasing the stick is silent.");
        Check(!detector.Pressing, "Released is not pressing.");
        Check(Run(detector, ref position, North, 0, true, now + 1, 1, out now) == 1, "A new push into the same wall bumps.");
        Check(!detector.Tick(position, North, true, now + 1), "A poll gap (menu, loading) restarts quietly.");
        Check(!detector.Pressing, "After a gap the stall has to be seen again.");
        Check(!detector.Tick(new Vector2(float.NaN, 0), North, true, now + 1.05), "A non-finite position is ignored.");
        Check(!detector.Tick(position, new Vector2(float.PositiveInfinity, 0), true, now + 1.1), "A non-finite push is ignored.");
        bool threw = false;
        try { _ = new WallBumpDetector(0); } catch (ArgumentOutOfRangeException) { threw = true; }
        Check(threw, "A zero move distance is rejected.");
    }

    private static void Sound()
    {
        float[] centre = BumpCueSynthesizer.Synthesize(0, 1);
        Check(centre.Length == RadarCueSynthesizer.SampleRate * RadarCueSynthesizer.Channels * 90 / 1000,
            "The bump lasts 90 ms.");
        Check(centre.All(sample => float.IsFinite(sample) && Math.Abs(sample) <= 1), "The bump is finite and bounded.");
        Check(centre.Any(sample => sample != 0), "The bump is audible.");
        Check(centre[0] == 0 && centre[1] == 0, "The bump starts without a click.");
        Check(centre.SequenceEqual(BumpCueSynthesizer.Synthesize(0, 1)), "The bump is deterministic.");
        float[] left = BumpCueSynthesizer.Synthesize(-1, 1), right = BumpCueSynthesizer.Synthesize(1, 1);
        Check(Energy(left, 0) > Energy(left, 1) * 10, "A push to the left sounds on the left.");
        Check(Energy(right, 1) > Energy(right, 0) * 10, "A push to the right sounds on the right.");
        Check(BumpCueSynthesizer.Synthesize(0, 0).All(sample => sample == 0), "Zero volume is silent.");
        Check(BumpCueSynthesizer.Synthesize(float.NaN, float.NaN).All(float.IsFinite), "Bad inputs stay finite.");
        float[] knock = RadarCueSynthesizer.Synthesize(RadarState.Blocked, RadarDirection.Up, 1);
        Check(Crossings(centre, 0.02, 0.06) * 2 < Crossings(knock, 0.02, 0.06), "Past its click the bump is far lower than the radar knock.");
    }

    private static double Energy(float[] samples, int channel)
    {
        double sum = 0;
        for (int index = channel; index < samples.Length; index += RadarCueSynthesizer.Channels)
            sum += samples[index] * samples[index];
        return sum;
    }

    private static int Crossings(float[] samples, double from, double to)
    {
        int count = 0, first = (int)(from * RadarCueSynthesizer.SampleRate), last = (int)(to * RadarCueSynthesizer.SampleRate);
        for (int index = (first + 1) * RadarCueSynthesizer.Channels; index < last * RadarCueSynthesizer.Channels;
             index += RadarCueSynthesizer.Channels)
            if (samples[index] >= 0 != samples[index - RadarCueSynthesizer.Channels] >= 0) count++;
        return count;
    }
}
