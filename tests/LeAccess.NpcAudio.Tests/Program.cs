using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Npcs;

internal static class Program
{
    private static int _checks;

    private static int Main()
    {
        try
        {
            FormatAndEnvelope();
            StereoPlacement();
            RearPitch();
            InvalidInputsAndGain();
            CueCadence();
            CadenceInvalidInputs();
            Console.WriteLine($"NPC audio tests passed: {_checks} checks; no audio device opened.");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static void FormatAndEnvelope()
    {
        float[] samples = NpcCueSynthesizer.Synthesize(0, 1, 1);
        Check(samples.Length == 8064, "The D2R NPC tap contains exactly 84 ms of 48 kHz stereo samples.");
        Check(samples.All(sample => float.IsFinite(sample) && Math.Abs(sample) <= 1),
            "Full-volume synthesis stays finite and unclipped.");
        Check(samples.Any(sample => sample != 0), "An enabled tap contains an audible signal.");
        Check(samples.SequenceEqual(NpcCueSynthesizer.Synthesize(0, 1, 1)),
            "Noise texture and timbre are reproducible.");
        Check(samples[0] == 0 && samples[1] == 0 && samples.TakeLast(2).All(sample => Math.Abs(sample) < 0.000001),
            "The attack and release reach silence at both ends.");
        Check(Energy(samples, 0, 0.005, 0.020) > Energy(samples, 0, 0.060, 0.080) * 100,
            "The tap is quickly damped rather than behaving like a continuous tone.");
    }

    private static void StereoPlacement()
    {
        float[] left = NpcCueSynthesizer.Synthesize(-1, 0.5f, 1);
        float[] right = NpcCueSynthesizer.Synthesize(1, 0.5f, 1);
        float[] center = NpcCueSynthesizer.Synthesize(0, 0.5f, 1);
        Check(Energy(left, 0) > 0 && Energy(left, 1) == 0, "Full-left placement reaches only the left channel.");
        Check(Energy(right, 1) > 0 && Energy(right, 0) == 0, "Full-right placement reaches only the right channel.");
        Check(Enumerable.Range(0, left.Length / 2).All(frame =>
            left[frame * 2] == right[frame * 2 + 1] && left[frame * 2 + 1] == right[frame * 2]),
            "Left and right cues mirror exactly.");
        Check(Enumerable.Range(0, center.Length / 2).All(frame => center[frame * 2] == center[frame * 2 + 1]),
            "Centered cues have identical stereo channels.");
        double totalCenter = Energy(center, 0) + Energy(center, 1);
        Check(Math.Abs(totalCenter / Energy(left, 0) - 1) < 0.000001,
            "Equal-power panning preserves signal energy across horizontal positions.");
        Check(left.SequenceEqual(NpcCueSynthesizer.Synthesize(-2, 0.5f, 1)) &&
            right.SequenceEqual(NpcCueSynthesizer.Synthesize(2, 0.5f, 1)), "Pan outside the range clamps to the nearest side.");
    }

    private static void RearPitch()
    {
        float[] front = NpcCueSynthesizer.Synthesize(0, 1, 1);
        float[] rear = NpcCueSynthesizer.Synthesize(0, -1, 1);
        Check(!front.SequenceEqual(rear), "Front and rear placement produce different pitch content.");
        Check(SpectralEnergy(front, 850) > SpectralEnergy(front, 680) * 3,
            "The front tap retains D2R's 850 Hz main resonance.");
        Check(SpectralEnergy(rear, 680) > SpectralEnergy(rear, 850) * 3,
            "The rear tap lowers its main resonance to 80 percent, 680 Hz.");
        Check(front.SequenceEqual(NpcCueSynthesizer.Synthesize(0, 0, 1)),
            "Only the rear hemisphere lowers pitch.");
        Check(rear.SequenceEqual(NpcCueSynthesizer.Synthesize(0, -2, 1)), "Rearward projection is bounded.");
    }

    private static void InvalidInputsAndGain()
    {
        float[] full = NpcCueSynthesizer.Synthesize(0, 1, 1);
        float[] half = NpcCueSynthesizer.Synthesize(0, 1, 0.5f);
        Check(half.Zip(full).All(pair => Math.Abs(pair.First * 2 - pair.Second) < 0.000001),
            "Gain changes amplitude without changing timbre or duration.");
        Check(NpcCueSynthesizer.Synthesize(0, 1, CuePlacement.MaxGain + 1).SequenceEqual(NpcCueSynthesizer.Synthesize(0, 1, CuePlacement.MaxGain)), "Excessive gain clamps to the boost limit.");
        foreach (float gain in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            Check(NpcCueSynthesizer.Synthesize(0, 1, gain).All(sample => sample == 0),
                "Disabled or invalid gain produces silence.");
        foreach (float axis in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Check(full.SequenceEqual(NpcCueSynthesizer.Synthesize(axis, 1, 1)), "Invalid pan falls back to center.");
            Check(full.SequenceEqual(NpcCueSynthesizer.Synthesize(0, axis, 1)), "Invalid direction falls back to front.");
        }
    }

    private static void CueCadence()
    {
        var near = new NpcCueTiming();
        Check(near.TryTake(1, 0, 0, 10), "The first eligible NPC cue plays immediately.");
        Check(!near.TryTake(1, 0.1, 0, 10) && near.TryTake(1, 0.2, 0, 10),
            "At 100 ms polls, the near NPC cadence produces its next cue after 200 ms.");
        Check(!near.TryTake(1, 0.3, 0, 10) && near.TryTake(1, 0.4, 0, 10),
            "Repeated near cues remain separated by the proximity interval.");

        var far = new NpcCueTiming();
        Check(far.TryTake(1, 0, 10, 10), "An NPC at the range boundary is eligible.");
        Check(Enumerable.Range(1, 4).All(poll => !far.TryTake(1, poll * 0.1, 10, 10)) &&
            far.TryTake(1, 0.5, 10, 10),
            "At 100 ms polls, the far NPC cadence produces its next cue after 500 ms.");

        var switched = new NpcCueTiming();
        Check(switched.TryTake(1, 0, 10, 10), "The original far NPC establishes its cadence.");
        Check(!switched.TryTake(1, 0.1, 0, 10) && !switched.TryTake(1, 0.2, 4, 10) &&
            !switched.TryTake(1, 0.3, 0, 10) && !switched.TryTake(1, 0.4, 3, 10),
            "Changing the nearest NPC and its distance cannot reset the pending cue or cause a burst.");
        Check(switched.TryTake(1, 0.5, 0, 10) && !switched.TryTake(1, 0.6, 0, 10) &&
            switched.TryTake(1, 0.7, 0, 10),
            "After the previous deadline, the new nearby NPC uses the faster cadence.");
        Check(switched.TryTake(2, 0.05, 0, 10),
            "A new scene session clears the previous deadline even if its clock starts earlier.");
        Check(!switched.TryTake(2, 0.06, 0, 10), "The new scene still enforces the pulse interval.");
        switched.Reset();
        Check(switched.TryTake(2, 0.06, 0, 10),
            "Explicit suspension reset allows an immediate fresh cue on resume.");
        Check(new NpcCueTiming().TryTake(0, 0, 0, 10), "The default session is valid at clock zero.");
    }

    private static void CadenceInvalidInputs()
    {
        var timing = new NpcCueTiming();
        foreach (double now in new[] { -1, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Check(!timing.TryTake(1, now, 0, 10), "Invalid or negative times cannot schedule a cue.");
        foreach (float distance in new[] { -1f, 11f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            Check(!timing.TryTake(1, 0, distance, 10), "Invalid or out-of-range distances cannot schedule a cue.");
        foreach (float range in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            Check(!timing.TryTake(1, 0, 0, range), "Invalid ranges cannot schedule a cue.");
        Check(timing.TryTake(1, 0, 0, 10), "Rejected initial observations do not delay the first valid cue.");
        Check(!timing.TryTake(1, 0.1, 11, 10) && !timing.TryTake(1, 0.1, 0, 10),
            "An out-of-range observation does not erase an existing pulse deadline.");
        Check(!timing.TryTake(1, double.NaN, 0, 10) && timing.TryTake(1, 0.2, 0, 10),
            "A rejected clock value does not corrupt an existing deadline.");
    }

    private static double Energy(float[] samples, int channel, double startSeconds = 0,
        double endSeconds = NpcCueSynthesizer.DurationSeconds)
    {
        int start = (int)(startSeconds * NpcCueSynthesizer.SampleRate);
        int end = Math.Min(samples.Length / 2, (int)(endSeconds * NpcCueSynthesizer.SampleRate));
        double sum = 0;
        for (int frame = start; frame < end; frame++)
            sum += (double)samples[frame * 2 + channel] * samples[frame * 2 + channel];
        return sum;
    }

    private static double SpectralEnergy(float[] samples, double frequency)
    {
        double real = 0, imaginary = 0;
        for (int frame = 0; frame < samples.Length / 2; frame++)
        {
            double phase = 2 * Math.PI * frequency * frame / NpcCueSynthesizer.SampleRate;
            real += samples[frame * 2] * Math.Cos(phase);
            imaginary += samples[frame * 2] * Math.Sin(phase);
        }
        return real * real + imaginary * imaginary;
    }

    private static void Check(bool condition, string description)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException(description);
    }
}
