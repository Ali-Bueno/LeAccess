using System;

namespace LeAccess.Accessibility.Audio;

// The Morditas circle pulse: a short bell that drops a fifth and loses its overtones behind, so front and back differ
// in pitch and brightness, unlike the drip's glide or the beacon's level blip (reference/rage-of-morditas.md#circle-pulse).
internal static class MorditasCircleSynthesizer
{
    internal const int SampleRate = RadarCueSynthesizer.SampleRate;
    internal const int Channels = RadarCueSynthesizer.Channels;
    internal const double DurationSeconds = 0.14;
    // A perfect fifth below: wider than the other world cues' major third so the rear is unmistakable.
    internal const double RearPitch = 2.0 / 3;
    // Fully rear-sounding from 60 degrees either side of straight behind.
    internal const float RearFullAhead = 0.5f;
    private const double BaseHz = 440;
    // Bell partials (inharmonic ratios) and their levels in front; behind only the fundamental remains.
    private static readonly double[] Partials = { 1, 2.76, 5.4 };
    private static readonly double[] PartialLevels = { 1, 0.5, 0.25 };
    private const double Attack = 0.003, Release = 0.02, Decay = 0.06;
    // Above the drip's 0.2: the user asked for the circle a little louder.
    private const double Level = 0.3;
    // Level at the circle's edge and beyond, so the way back stays audible.
    internal const float EdgeGain = 0.5f;

    /// <summary>Full level at the centre, falling linearly to EdgeGain at the circle's radius.</summary>
    internal static float Gain(float distance, float radius)
    {
        if (!float.IsFinite(distance) || !(radius > 0)) return 1;
        return 1 + (EdgeGain - 1) * Math.Clamp(distance / radius, 0, 1);
    }

    /// <summary>Share of the rear sound, 0 in front to 1 behind.</summary>
    internal static double Rear(float ahead) =>
        float.IsFinite(ahead) ? Math.Clamp(-ahead / RearFullAhead, 0, 1) : 0;

    // pan: -1 left .. +1 right; ahead: +1 in front .. -1 behind.
    internal static float[] Synthesize(float pan, float ahead, float gain)
    {
        var placement = new CuePlacement(pan, ahead);
        double rear = Rear(ahead);
        double pitch = 1 + (RearPitch - 1) * rear, brightness = 1 - rear;
        double level = Level * CuePlacement.Gain(gain), norm = 0;
        for (int partial = 0; partial < Partials.Length; partial++)
            norm += PartialLevels[partial] * (partial == 0 ? 1 : brightness);
        int frames = (int)(DurationSeconds * SampleRate);
        var samples = new float[frames * Channels];
        for (int frame = 0; frame < frames; frame++)
        {
            double time = (double)frame / SampleRate;
            double envelope = Math.Min(1, time / Attack) * Math.Min(1, (DurationSeconds - time) / Release) *
                Math.Exp(-time / Decay);
            double sum = 0;
            for (int partial = 0; partial < Partials.Length; partial++)
                sum += PartialLevels[partial] * (partial == 0 ? 1 : brightness) *
                    Math.Sin(2 * Math.PI * BaseHz * pitch * Partials[partial] * time);
            double tone = level * envelope * sum / norm;
            samples[frame * Channels] = (float)Math.Clamp(tone * placement.Left, -1, 1);
            samples[frame * Channels + 1] = (float)Math.Clamp(tone * placement.Right, -1, 1);
        }
        return samples;
    }
}
