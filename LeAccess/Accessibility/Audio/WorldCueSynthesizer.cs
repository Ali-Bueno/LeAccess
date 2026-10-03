using System;
using LeAccess.Accessibility.WorldCues;

namespace LeAccess.Accessibility.Audio;

internal static class WorldCueSynthesizer
{
    internal const int SampleRate = 48000;
    internal const int Channels = 2;
    // Sound-design values: two high chimes, one metallic strike, and a rising two-note prompt.
    private const double CoinsHz = 2100;
    private const double EquipmentHz = 1250;
    private const double InteractableHz = 420;
    private const double Level = 0.24;
    private const double AttackSeconds = 0.004;
    private const double ReleaseSeconds = 0.015;

    internal static double Duration(WorldCueCategory category, NaturalCueKind? kind = null)
    {
        var selected = NaturalKind(category, kind);
        double natural = selected.HasValue ? NaturalCueBank.Duration(selected.Value) : 0;
        return natural > 0 ? natural : SynthesizedDuration(category);
    }

    private static NaturalCueKind? NaturalKind(WorldCueCategory category, NaturalCueKind? kind) => category switch
    {
        WorldCueCategory.Coins => NaturalCueKind.Gold,
        WorldCueCategory.Equipment => NaturalCueKind.Equipment,
        WorldCueCategory.Interactable => kind,
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    private static double SynthesizedDuration(WorldCueCategory category) => category switch
    {
        WorldCueCategory.Coins => 0.16,
        WorldCueCategory.Equipment => 0.14,
        WorldCueCategory.Interactable => 0.24,
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    internal static float[] Synthesize(WorldCueCategory category, float pan, float forward, float gain,
        NaturalCueKind? kind = null)
    {
        var selected = NaturalKind(category, kind);
        if (selected.HasValue && NaturalCueBank.TryBuild(selected.Value, pan, forward, gain, out var natural)) return natural;
        double duration = SynthesizedDuration(category);
        double safeGain = CuePlacement.Gain(gain);
        var placement = new CuePlacement(pan, forward);
        double pitch = placement.Pitch, left = placement.Left, right = placement.Right;
        int frames = (int)Math.Round(duration * SampleRate);
        var samples = new float[frames * Channels];
        for (int frame = 0; frame < frames; frame++)
        {
            double t = (double)frame / SampleRate;
            double part = t, frequency, decay;
            double pulseDuration = duration;
            if (category == WorldCueCategory.Coins)
            {
                pulseDuration = duration / 2;
                part = t % pulseDuration;
                frequency = CoinsHz * (t < pulseDuration ? 1 : 1.25);
                decay = 0.022;
            }
            else if (category == WorldCueCategory.Equipment)
            {
                frequency = EquipmentHz;
                decay = 0.032;
            }
            else
            {
                pulseDuration = duration / 2;
                part = t % pulseDuration;
                frequency = InteractableHz * (t < pulseDuration ? 1 : 1.5);
                decay = 0.085;
            }
            double phase = 2 * Math.PI * frequency * pitch * part;
            double tone = Math.Sin(phase);
            if (category == WorldCueCategory.Equipment)
                tone = (tone + 0.5 * Math.Sin(phase * 2.76) + 0.25 * Math.Sin(phase * 4.07)) / 1.75;
            double envelope = Math.Clamp(part / AttackSeconds, 0, 1)
                * Math.Clamp((pulseDuration - part - 1.0 / SampleRate) / ReleaseSeconds, 0, 1)
                * Math.Exp(-part / decay);
            double sample = Level * safeGain * envelope * tone;
            samples[frame * Channels] = (float)(sample * left);
            samples[frame * Channels + 1] = (float)(sample * right);
        }
        return samples;
    }
}
