using System;

namespace LeAccess.Accessibility.Audio;

/// <summary>A dull thud for walking into a wall, lower and shorter than the radar's tonal knock.</summary>
public static class BumpCueSynthesizer
{
    // Sound design, not game data: a falling sub-bass body under a short muffled noise click.
    private const double Duration = 0.090;
    private const double Attack = 0.002;
    private const double Release = 0.012;
    private const double StartFrequency = 150;
    private const double EndFrequency = 70;
    private const double BodyDecay = 0.030;
    private const double ClickDecay = 0.006;
    private const double ClickLevel = 0.5;
    private const double ClickCutoff = 1800;
    // Low frequencies sound quieter at the same peak; the thud matches the knock's loudness at the radar level.
    private const double LevelOverKnock = 1.4;
    private const double PanWidth = 0.9;
    private const uint NoiseSeed = 7;
    private const uint NoiseMultiplier = 1664525;
    private const uint NoiseIncrement = 1013904223;
    private const double NoiseScale = 8388608.0;

    /// <param name="pan">-1 for screen left to 1 for screen right.</param>
    public static float[] Synthesize(float pan, float gain)
    {
        double level = RadarCueSynthesizer.KnockLevel * LevelOverKnock
            * (float.IsFinite(gain) ? Math.Clamp(gain, 0f, 1f) : 0f);
        double side = (float.IsFinite(pan) ? Math.Clamp(pan, -1f, 1f) : 0f) * PanWidth;
        double left = Math.Sqrt((1 - side) / 2), right = Math.Sqrt((1 + side) / 2);
        int frames = (int)(Duration * RadarCueSynthesizer.SampleRate);
        var samples = new float[frames * RadarCueSynthesizer.Channels];
        double smoothing = 1 - Math.Exp(-2 * Math.PI * ClickCutoff / RadarCueSynthesizer.SampleRate);
        double phase = 0, noise = 0;
        uint seed = NoiseSeed;
        for (int frame = 0; frame < frames; frame++)
        {
            double time = (double)frame / RadarCueSynthesizer.SampleRate;
            phase += 2 * Math.PI * StartFrequency * Math.Pow(EndFrequency / StartFrequency, time / Duration)
                / RadarCueSynthesizer.SampleRate;
            seed = unchecked(seed * NoiseMultiplier + NoiseIncrement);
            noise += smoothing * ((seed >> 8) / NoiseScale - 1 - noise);
            double gate = Math.Min(1, time / Attack) * Math.Min(1, (Duration - time) / Release);
            double value = level * gate * (Math.Sin(phase) * Math.Exp(-time / BodyDecay)
                + ClickLevel * noise * Math.Exp(-time / ClickDecay));
            samples[frame * RadarCueSynthesizer.Channels] = (float)Math.Clamp(value * left, -1, 1);
            samples[frame * RadarCueSynthesizer.Channels + 1] = (float)Math.Clamp(value * right, -1, 1);
        }
        return samples;
    }
}
