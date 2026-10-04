using System.Numerics;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Navigation;
using static Program;

internal static class MarkTests
{
    private static readonly Vector2[] Axes = { new(0, 1), new(-1, 0), new(1, 0), new(0, -1) };

    internal static void Run()
    {
        Quadrants();
        Freshness();
        Sound();
    }

    private static void Quadrants()
    {
        var player = new Vector2(10, 10);
        RadarUnexploredMarks.Publish(new[] { new Vector2(12, 25) }, 40, 0);
        Check(RadarUnexploredMarks.Marked(RadarDirection.Up, player, Axes, 0.5), "An area ahead marks the upward opening.");
        Check(!RadarUnexploredMarks.Marked(RadarDirection.Right, player, Axes, 0.5), "A mostly-ahead area does not mark the side.");
        Check(!RadarUnexploredMarks.Marked(RadarDirection.Down, player, Axes, 0.5), "Nothing marks the opposite direction.");
        RadarUnexploredMarks.Publish(new[] { new Vector2(-5, 13), new Vector2(10, -20) }, 40, 1);
        Check(RadarUnexploredMarks.Marked(RadarDirection.Left, player, Axes, 1), "The dominant screen axis picks the left quadrant.");
        Check(RadarUnexploredMarks.Marked(RadarDirection.Down, player, Axes, 1), "A second area marks its own quadrant.");
        Check(!RadarUnexploredMarks.Marked(RadarDirection.Up, player, Axes, 1), "Areas elsewhere leave up unmarked.");
        RadarUnexploredMarks.Publish(new[] { new Vector2(10, 60) }, 40, 2);
        Check(!RadarUnexploredMarks.Marked(RadarDirection.Up, player, Axes, 2), "An area beyond the range does not mark.");
        // A turned camera turns the quadrants with it.
        var turned = new[] { new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1), new Vector2(-1, 0) };
        RadarUnexploredMarks.Publish(new[] { new Vector2(30, 10) }, 40, 3);
        Check(RadarUnexploredMarks.Marked(RadarDirection.Up, player, turned, 3), "Quadrants follow the camera axes.");
        Check(!RadarUnexploredMarks.Marked(RadarDirection.Up, player, null, 3), "Missing axes never mark.");
        RadarUnexploredMarks.Publish(new[] { new Vector2(float.NaN, 1) }, 40, 3);
        Check(RadarUnexploredMarks.Count == 0, "Non-finite areas are ignored.");
        RadarUnexploredMarks.Clear();
    }

    private static void Freshness()
    {
        var player = Vector2.Zero;
        RadarUnexploredMarks.Publish(new[] { new Vector2(0, 10) }, 40, 10);
        Check(RadarUnexploredMarks.Marked(RadarDirection.Up, player, Axes, 10 + RadarUnexploredMarks.StaleSeconds),
            "Areas stay trusted for a few scanner refreshes.");
        Check(!RadarUnexploredMarks.Marked(RadarDirection.Up, player, Axes, 10.01 + RadarUnexploredMarks.StaleSeconds),
            "Stale areas no longer mark, e.g. after leaving gameplay.");
        Check(!RadarUnexploredMarks.Marked(RadarDirection.Up, player, Axes, 9), "Time reversal does not trust old areas.");
        RadarUnexploredMarks.Publish(new[] { new Vector2(0, 10) }, 0, 20);
        Check(!RadarUnexploredMarks.Marked(RadarDirection.Up, player, Axes, 20), "A zero range marks nothing.");
        RadarUnexploredMarks.Publish(new[] { new Vector2(0, 10) }, 40, 30);
        RadarUnexploredMarks.Clear();
        Check(!RadarUnexploredMarks.Marked(RadarDirection.Up, player, Axes, 30), "Clearing removes every mark.");
    }

    private static void Sound()
    {
        int frame = RadarCueSynthesizer.Channels;
        int tail = (int)(0.030 * RadarCueSynthesizer.SampleRate) + 2 * (int)(0.035 * RadarCueSynthesizer.SampleRate)
            + (int)(0.030 * RadarCueSynthesizer.SampleRate);
        foreach (RadarDirection direction in Enum.GetValues<RadarDirection>())
        {
            float[] plain = RadarCueSynthesizer.Synthesize(RadarState.Open, direction, 1);
            float[] marked = RadarCueSynthesizer.Synthesize(RadarState.Open, direction, 1, unexplored: true);
            Check(marked.Length == plain.Length + tail * frame, "The mark adds a gap and two pips after the breath.");
            Check(marked.Take(plain.Length).SequenceEqual(plain), "The breath itself is unchanged by the mark.");
            Check(marked.Skip(plain.Length).Take((int)(0.030 * RadarCueSynthesizer.SampleRate) * frame).All(sample => sample == 0),
                "A short silence separates the breath from the pips.");
            Check(marked.Skip(plain.Length).Any(sample => sample != 0), "The pips are audible.");
            Check(marked.All(sample => float.IsFinite(sample) && Math.Abs(sample) <= 1), "The mark is finite bounded audio.");
            Check(RadarCueSynthesizer.Synthesize(RadarState.Blocked, direction, 1, unexplored: true)
                .SequenceEqual(RadarCueSynthesizer.Synthesize(RadarState.Blocked, direction, 1)), "Walls never carry the mark.");
        }
        float[] left = RadarCueSynthesizer.Synthesize(RadarState.Open, RadarDirection.Left, 1, unexplored: true);
        int start = left.Length - tail * frame;
        double leftEnergy = 0, rightEnergy = 0;
        for (int index = start; index < left.Length; index += frame)
        {
            leftEnergy += left[index] * left[index];
            rightEnergy += left[index + 1] * left[index + 1];
        }
        Check(leftEnergy > rightEnergy * 10, "The mark keeps the cue's stereo side.");
        Check(RadarCueSynthesizer.Synthesize(RadarState.Open, RadarDirection.Up, 0, unexplored: true).All(sample => sample == 0),
            "Zero volume silences the mark too.");
    }
}
