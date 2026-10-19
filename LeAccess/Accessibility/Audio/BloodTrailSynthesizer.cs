using System;

namespace LeAccess.Accessibility.Audio;

// The Morditas blood-trail drip: a short tone gliding up like a falling drop, unlike the beacon's level blip, the NPC
// tap or the enemy growls. Mod-made, no asset (reference/rage-of-morditas.md#the-drip).
internal static class BloodTrailSynthesizer
{
    internal const int SampleRate = RadarCueSynthesizer.SampleRate;
    internal const int Channels = RadarCueSynthesizer.Channels;
    internal const double DurationSeconds = 0.16;
    private const double StartHz = 320, EndHz = 960, GlideSeconds = 0.05;
    private const double Attack = 0.003, Release = 0.02, Decay = 0.045;
    private const double Level = 0.2;

    // pan: -1 left .. +1 right; ahead: +1 in front .. -1 behind, which lowers the pitch like the other world cues.
    internal static float[] Synthesize(float pan, float ahead, float gain)
    {
        var placement = new CuePlacement(pan, ahead);
        double level = Level * CuePlacement.Gain(gain);
        int frames = (int)(DurationSeconds * SampleRate);
        var samples = new float[frames * Channels];
        double phase = 0;
        for (int frame = 0; frame < frames; frame++)
        {
            double time = (double)frame / SampleRate;
            double hz = StartHz * Math.Pow(EndHz / StartHz, Math.Min(1, time / GlideSeconds)) * placement.Pitch;
            phase += 2 * Math.PI * hz / SampleRate;
            double envelope = Math.Min(1, time / Attack) * Math.Min(1, (DurationSeconds - time) / Release) *
                Math.Exp(-time / Decay);
            double tone = level * envelope * Math.Sin(phase);
            samples[frame * Channels] = (float)Math.Clamp(tone * placement.Left, -1, 1);
            samples[frame * Channels + 1] = (float)Math.Clamp(tone * placement.Right, -1, 1);
        }
        return samples;
    }
}
