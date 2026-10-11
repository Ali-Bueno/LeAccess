using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Navigation;

internal static class WallToneTests
{
    private static int _checks;
    private const int Rate = WallToneSynth.SampleRate;

    internal static int Run()
    {
        Curve();
        Tones();
        Glide();
        Levels();
        Preview();
        return _checks;
    }

    private static void Curve()
    {
        Check(WallToneSynth.Gain(0, 12) == 1, "A touching wall is full volume.");
        Check(Math.Abs(WallToneSynth.Gain(6, 12) - 0.25f) < 1e-6, "Half the range is a quarter: Grimdark's squared curve.");
        Check(WallToneSynth.Gain(9, 12) < WallToneSynth.Gain(3, 12) / 8, "Far walls are much softer than near ones.");
        Check(WallToneSynth.Gain(12, 12) == 0 && WallToneSynth.Gain(20, 12) == 0, "At or beyond the range the tone is silent.");
        Check(WallToneSynth.Gain(-1, 12) == 1, "A negative depth counts as touching.");
        Check(WallToneSynth.Gain(float.NaN, 12) == 0 && WallToneSynth.Gain(3, 0) == 0 && WallToneSynth.Gain(3, float.NaN) == 0,
            "Unknown distances or ranges are silent.");
    }

    private static void Tones()
    {
        float[] ahead = Steady(RadarDirection.Up), behind = Steady(RadarDirection.Down);
        float[] left = Steady(RadarDirection.Left), right = Steady(RadarDirection.Right);
        Check(ahead.All(float.IsFinite) && ahead.Max(Math.Abs) <= WallToneSynth.ContactLevel * 1.5,
            "A touching wall stays near the soft contact level.");
        Check(Math.Abs(Energy(ahead, 0) - Energy(ahead, 1)) < Energy(ahead, 0) * 0.01, "The tone ahead is centred.");
        Check(Energy(left, 0) > Energy(left, 1) * 100 && Energy(right, 1) > Energy(right, 0) * 100,
            "Side tones sit in their own ear.");
        Check(Crossings(ahead) > Crossings(left) && Crossings(left) > Crossings(behind),
            "Ahead is the highest tone, behind the lowest, the sides between.");
        Check(Energy(behind, 0) > Energy(ahead, 0), "The low tone behind is lifted so it is heard as clearly.");
    }

    private static void Glide()
    {
        var synth = new WallToneSynth(() => 1);
        synth.SetTarget(RadarDirection.Up, 1);
        float[] start = new float[Rate / 100 * 2];
        synth.Read(start, 0, start.Length);
        Check(Math.Abs(start[0]) < 1e-4 && Math.Abs(start[2]) < 1e-3, "A tone fades in instead of clicking.");
        float[] steady = new float[Rate / 5 * 2];
        synth.Read(steady, 0, steady.Length);
        synth.Silence();
        float[] fade = new float[Rate / 5 * 2];
        synth.Read(fade, 0, fade.Length);
        float largestStep = 0;
        for (int i = 2; i < fade.Length; i += 2) largestStep = Math.Max(largestStep, Math.Abs(fade[i] - fade[i - 2]));
        Check(largestStep < WallToneSynth.ContactLevel * 0.5f, "Silencing glides out without a jump.");
        Check(fade.Skip(fade.Length - 200).All(sample => Math.Abs(sample) < 1e-4), "After the glide the tone is silent.");
        synth.SetTarget(RadarDirection.Up, float.NaN);
        float[] bad = new float[400];
        synth.Read(bad, 0, bad.Length);
        Check(bad.All(sample => float.IsFinite(sample) && Math.Abs(sample) < 1e-4), "A bad target is silence.");
    }

    private static void Levels()
    {
        float full = Steady(RadarDirection.Up, 1).Max(Math.Abs);
        float half = Steady(RadarDirection.Up, 0.5f).Max(Math.Abs);
        Check(Math.Abs(half - full * 0.5f) < full * 0.02f, "The user level (master times sonar) scales the tones.");
        Check(Steady(RadarDirection.Up, 0).All(sample => sample == 0), "A zero level silences the tones.");
        Check(Steady(RadarDirection.Up, float.NaN).All(sample => sample == 0), "A bad level is silence.");
        float near = Steady(RadarDirection.Up, 1, WallToneSynth.Gain(1, 12)).Max(Math.Abs);
        float far = Steady(RadarDirection.Up, 1, WallToneSynth.Gain(8, 12)).Max(Math.Abs);
        Check(near > far * 4, "A near wall is clearly louder than a far one.");
    }

    private static void Preview()
    {
        float[] clip = WallToneSynth.Preview(RadarDirection.Right, 1, 1);
        Check(clip.Length == Rate * 2, "The preview lasts the asked time in stereo.");
        Check(Energy(clip, 1) > Energy(clip, 0) * 100, "The side preview plays in its ear.");
        Check(Math.Abs(clip[^1]) < WallToneSynth.ContactLevel * 0.05f, "The preview fades out at its end.");
        Check(WallToneSynth.Preview(RadarDirection.Up, 0, 1).Length == 0, "A silent level has no preview.");
    }

    // Half a second at a steady gain, after the fade-in.
    private static float[] Steady(RadarDirection direction, float level = 1, float gain = 1)
    {
        var synth = new WallToneSynth(() => level);
        synth.SetTarget(direction, gain);
        float[] warm = new float[Rate / 5 * 2];
        synth.Read(warm, 0, warm.Length);
        float[] samples = new float[Rate / 2 * 2];
        synth.Read(samples, 0, samples.Length);
        return samples;
    }

    private static double Energy(float[] samples, int channel)
    {
        double sum = 0;
        for (int i = channel; i < samples.Length; i += 2) sum += samples[i] * samples[i];
        return sum;
    }

    private static int Crossings(float[] samples)
    {
        int channel = Energy(samples, 0) >= Energy(samples, 1) ? 0 : 1, count = 0;
        for (int i = channel + 2; i < samples.Length; i += 2)
            if (samples[i] >= 0 != samples[i - 2] >= 0) count++;
        return count;
    }

    private static void Check(bool condition, string description)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException(description);
    }
}
