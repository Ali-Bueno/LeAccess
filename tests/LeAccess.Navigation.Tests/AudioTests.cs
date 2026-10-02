using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Navigation;
using static Program;

internal static class AudioTests
{
    internal static void Run()
    {
        foreach (RadarState state in new[] { RadarState.Open, RadarState.Blocked })
        {
            foreach (RadarDirection direction in Enum.GetValues<RadarDirection>())
            {
                float[] cue = RadarCueSynthesizer.Synthesize(state, direction, 1);
                int durationMilliseconds = state == RadarState.Open ? 180 : 120;
                Check(cue.Length == RadarCueSynthesizer.SampleRate * RadarCueSynthesizer.Channels
                    * durationMilliseconds / 1000, "Cue has the exact documented duration.");
                Check(cue.All(sample => float.IsFinite(sample) && Math.Abs(sample) <= 1),
                    "Synthesized cue contains only finite bounded audio.");
                Check(cue.Any(sample => sample != 0), "Audible cue contains signal.");
                Check(cue.SequenceEqual(RadarCueSynthesizer.Synthesize(state, direction, 1)),
                    "Synthesis produces deterministic cues.");
                Check(cue[0] == 0 && cue[1] == 0, "Cue attack starts without an initial discontinuity.");
            }
            Stereo(state);
            Gains(state);
        }
        float[] up = RadarCueSynthesizer.Synthesize(RadarState.Open, RadarDirection.Up, 1);
        float[] down = RadarCueSynthesizer.Synthesize(RadarState.Open, RadarDirection.Down, 1);
        Check(!up.SequenceEqual(down), "Upward and downward opening breaths are distinguishable.");
        Check(Crossings(up, 0.10, 0.16) > Crossings(up, 0.02, 0.08), "Upward breath rises in frequency.");
        Check(Crossings(down, 0.10, 0.16) < Crossings(down, 0.02, 0.08), "Downward breath falls in frequency.");
        Check(RadarCueSynthesizer.Synthesize(RadarState.Unknown, RadarDirection.Up, 1).Length == 0,
            "Unknown geometry has no sound.");
    }

    private static void Stereo(RadarState state)
    {
        float[] left = RadarCueSynthesizer.Synthesize(state, RadarDirection.Left, 1);
        float[] right = RadarCueSynthesizer.Synthesize(state, RadarDirection.Right, 1);
        Check(Energy(left, 0) > Energy(left, 1) * 10, "Left cue is clearly panned left.");
        Check(Energy(right, 1) > Energy(right, 0) * 10, "Right cue is clearly panned right.");
        bool mirrored = true;
        for (int frame = 0; frame < left.Length; frame += 2)
            mirrored &= left[frame] == right[frame + 1] && left[frame + 1] == right[frame];
        Check(mirrored, "Left and right cues preserve exact mirrored stereo placement.");
        foreach (RadarDirection direction in new[] { RadarDirection.Up, RadarDirection.Down })
        {
            float[] cue = RadarCueSynthesizer.Synthesize(state, direction, 1);
            bool centered = true;
            for (int frame = 0; frame < cue.Length; frame += 2) centered &= cue[frame] == cue[frame + 1];
            Check(centered, "Up and down cues are centered.");
        }
    }

    private static void Gains(RadarState state)
    {
        float[] full = RadarCueSynthesizer.Synthesize(state, RadarDirection.Up, 1);
        Check(full.SequenceEqual(RadarCueSynthesizer.Synthesize(state, RadarDirection.Up, 2)),
            "Gain above full scale is clamped.");
        foreach (float gain in new[] { 0, -1, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            float[] muted = RadarCueSynthesizer.Synthesize(state, RadarDirection.Up, gain);
            Check(muted.Length == full.Length && muted.All(sample => sample == 0),
                "Zero, negative and non-finite gains safely produce silence.");
        }
        float[] half = RadarCueSynthesizer.Synthesize(state, RadarDirection.Up, 0.5f);
        Check(half.Zip(full).All(pair => Math.Abs(pair.First * 2 - pair.Second) < 0.000001),
            "Volume scales amplitude without changing cue shape.");
    }

    private static double Energy(float[] samples, int channel)
    {
        double energy = 0;
        for (int i = channel; i < samples.Length; i += 2) energy += samples[i] * samples[i];
        return energy;
    }

    private static int Crossings(float[] samples, double startSeconds, double endSeconds)
    {
        int start = (int)(startSeconds * RadarCueSynthesizer.SampleRate) * RadarCueSynthesizer.Channels;
        int end = (int)(endSeconds * RadarCueSynthesizer.SampleRate) * RadarCueSynthesizer.Channels;
        int crossings = 0;
        for (int i = start + 2; i < end; i += 2)
            if ((samples[i] >= 0) != (samples[i - 2] >= 0)) crossings++;
        return crossings;
    }
}
