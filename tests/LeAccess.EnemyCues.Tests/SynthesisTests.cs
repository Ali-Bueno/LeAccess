using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Combat;
using LeAccess.Accessibility.Navigation;

internal static partial class Program
{
    // D2R entity_cue_audio_tests.cpp bounds: peak 1500..14000 of a 32767 PCM full scale.
    private const double MinimumPeak = 1500.0 / 32767;
    private const double MaximumPeak = 14000.0 / 32767;
    private static readonly EnemyCueKind[] Kinds = { EnemyCueKind.Boss, EnemyCueKind.Ordinary };

    private static void Synthesis()
    {
        foreach (var kind in Kinds)
        {
            float[] samples = EnemyCueSynthesizer.Synthesize(kind, 0, 1, 1);
            int frames = samples.Length / EnemyCueSynthesizer.Channels;
            Check(samples.Length == 8640, $"The {kind} cue holds exactly 90 ms of 48 kHz stereo.");
            Check(samples.All(float.IsFinite), $"The {kind} cue contains no NaN or infinity.");
            double peak = samples.Max(sample => Math.Abs(sample));
            Check(peak > MinimumPeak && peak < MaximumPeak, $"The {kind} cue is audible with headroom (peak {peak:F3}).");
            Check(samples[0] == 0 && samples[1] == 0 && samples.TakeLast(2).All(sample => Math.Abs(sample) < 1e-6),
                $"The {kind} cue starts and ends at silence.");
            double middle = Rms(samples, 0, frames / 3, frames * 2 / 3);
            Check(middle > 2 * Rms(samples, 0, 0, frames / 3) && middle > 2 * Rms(samples, 0, frames * 2 / 3, frames),
                $"The {kind} cue swells and fades instead of striking sharply.");
            Check(samples.SequenceEqual(EnemyCueSynthesizer.Synthesize(kind, 0, 1, 1)), $"The {kind} cue is reproducible.");
            float[] rear = EnemyCueSynthesizer.Synthesize(kind, 0, -1, 1);
            Check(Crossings(rear, frames / 3, frames * 2 / 3) < Crossings(samples, frames / 3, frames * 2 / 3),
                $"A {kind} cue behind is lower than one ahead.");
            Check(samples.SequenceEqual(EnemyCueSynthesizer.Synthesize(kind, 0, 0, 1)), "Only the rear hemisphere lowers pitch.");
        }
        Timbres();
        DistinctFromOtherCues();
        Placement();
        GainAndInvalidInputs();
    }

    private static void Timbres()
    {
        float[] growl = EnemyCueSynthesizer.Synthesize(EnemyCueKind.Ordinary, 0, 1, 1);
        float[] swoop = EnemyCueSynthesizer.Synthesize(EnemyCueKind.Boss, 0, 1, 1);
        int frames = swoop.Length / 2;
        Check(!growl.SequenceEqual(swoop), "The boss swoop and the growl have signatures of their own.");
        Check(Crossings(swoop, frames / 6, frames / 3) > Crossings(swoop, frames * 2 / 3, frames * 5 / 6),
            "The boss swoop sweeps down.");
        Check(Crossings(growl, frames / 6, frames / 3) > Crossings(growl, frames * 2 / 3, frames * 5 / 6),
            "The growl falls from 175 to 125 Hz.");
        // A 36 Hz flutter puts loudness dips about 28 ms apart; the middle window spans more than one period.
        var envelope = Enumerable.Range(0, 12).Select(part => Rms(growl, 0, frames / 4 + part * frames / 24,
            frames / 4 + (part + 1) * frames / 24)).ToArray();
        Check(envelope.Max() > 1.5 * envelope.Min(), "The growl's loudness flutters.");
    }

    private static void DistinctFromOtherCues()
    {
        float[] knock = RadarCueSynthesizer.Synthesize(RadarState.Blocked, RadarDirection.Up, 1);
        float[] tap = NpcCueSynthesizer.Synthesize(0, 1, 1);
        double knockRate = Rate(knock, 0, knock.Length / 2 / 3);
        double tapRate = Rate(tap, 0, tap.Length / 2 / 3);
        foreach (var kind in Kinds)
        {
            float[] enemy = EnemyCueSynthesizer.Synthesize(kind, 0, 1, 1);
            int frames = enemy.Length / 2;
            double enemyRate = Rate(enemy, frames / 3, frames * 2 / 3);
            foreach (RadarDirection direction in Enum.GetValues<RadarDirection>())
            {
                float[] breath = RadarCueSynthesizer.Synthesize(RadarState.Open, direction, 1);
                int breathFrames = breath.Length / 2;
                Check(enemyRate < 0.5 * Rate(breath, breathFrames / 3, breathFrames * 2 / 3),
                    $"The {kind} cue sits well below the radar's {direction} breath.");
            }
            Check(enemyRate < 0.25 * tapRate, $"The {kind} cue sits far below the NPC wooden tap.");
            if (kind == EnemyCueKind.Ordinary)
                Check(enemyRate < knockRate, "The growl is lower than the radar wall knock.");
        }
    }

    private static void Placement()
    {
        foreach (var kind in Kinds)
        {
            float[] left = EnemyCueSynthesizer.Synthesize(kind, -1, 0.5f, 1);
            float[] right = EnemyCueSynthesizer.Synthesize(kind, 1, 0.5f, 1);
            float[] center = EnemyCueSynthesizer.Synthesize(kind, 0, 0.5f, 1);
            Check(Rms(left, 0) > 0 && Rms(left, 1) == 0 && Rms(right, 1) > 0 && Rms(right, 0) == 0,
                $"Full-side {kind} placement reaches only that ear.");
            float[] partLeft = EnemyCueSynthesizer.Synthesize(kind, -0.75f, 1, 1);
            float[] partRight = EnemyCueSynthesizer.Synthesize(kind, 0.75f, 1, 1);
            Check(Rms(partLeft, 0) > 2 * Rms(partLeft, 1), $"A {kind} cue to the left favors the left ear.");
            Check(Enumerable.Range(0, partLeft.Length / 2).All(frame =>
                partLeft[frame * 2] == partRight[frame * 2 + 1] && partLeft[frame * 2 + 1] == partRight[frame * 2]),
                $"Left and right {kind} cues mirror exactly.");
            Check(Enumerable.Range(0, center.Length / 2).All(frame => center[frame * 2] == center[frame * 2 + 1]),
                $"A centered {kind} cue is equal in both ears.");
            double power = Math.Pow(Rms(center, 0), 2) + Math.Pow(Rms(center, 1), 2);
            Check(Math.Abs(power / Math.Pow(Rms(left, 0), 2) - 1) < 1e-4, $"{kind} panning keeps equal power.");
        }
    }

    private static void GainAndInvalidInputs()
    {
        foreach (var kind in Kinds)
        {
            float[] full = EnemyCueSynthesizer.Synthesize(kind, 0.3f, 0.2f, 1);
            float[] half = EnemyCueSynthesizer.Synthesize(kind, 0.3f, 0.2f, 0.5f);
            Check(half.Zip(full).All(pair => Math.Abs(pair.First * 2 - pair.Second) < 1e-6),
                $"Volume scales the {kind} cue without changing its timbre.");
            Check(full.SequenceEqual(EnemyCueSynthesizer.Synthesize(kind, 0.3f, 0.2f, 3)), "Excessive gain clamps to full.");
            foreach (float gain in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
                Check(EnemyCueSynthesizer.Synthesize(kind, 0, 1, gain).All(sample => sample == 0),
                    $"Gain {gain} silences the {kind} cue.");
            float[] front = EnemyCueSynthesizer.Synthesize(kind, 0, 1, 1);
            Check(EnemyCueSynthesizer.Synthesize(kind, float.NaN, float.PositiveInfinity, 1).SequenceEqual(front),
                "Non-finite placement falls back to front center.");
            Check(EnemyCueSynthesizer.Synthesize(kind, 5, -5, 1).SequenceEqual(EnemyCueSynthesizer.Synthesize(kind, 1, -1, 1)),
                "Placement beyond the unit range is clamped.");
        }
    }

    private static double Rms(float[] samples, int channel, int beginFrame = 0, int endFrame = -1)
    {
        if (endFrame < 0) endFrame = samples.Length / 2;
        double sum = 0;
        for (int frame = beginFrame; frame < endFrame; frame++) sum += samples[frame * 2 + channel] * samples[frame * 2 + channel];
        return Math.Sqrt(sum / Math.Max(1, endFrame - beginFrame));
    }

    private static int Crossings(float[] samples, int beginFrame, int endFrame)
    {
        int crossings = 0, prior = 0;
        for (int frame = beginFrame; frame < endFrame; frame++)
        {
            int sign = Math.Sign(samples[frame * 2]);
            if (sign != 0 && prior != 0 && sign != prior) crossings++;
            if (sign != 0) prior = sign;
        }
        return crossings;
    }

    private static double Rate(float[] samples, int beginFrame, int endFrame) =>
        (double)Crossings(samples, beginFrame, endFrame) / (endFrame - beginFrame);

    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Close(double expected, double actual, string message) =>
        Check(Math.Abs(expected - actual) < 1e-6, $"{message} Expected {expected}, got {actual}.");
}
