using System;
using LeAccess.Accessibility.Combat;

namespace LeAccess.Accessibility.Audio;

// Enemy presence cues ported from D2RAccess entity_cue_synth.cpp; see reference/enemy-cues.md.
internal static class EnemyCueSynthesizer
{
    internal const int SampleRate = 48000;
    internal const int Channels = 2;
    // D2R GrowlMs and WhooshMs: both fit inside the 100 ms start gap.
    internal const double DurationSeconds = 0.090;
    // 2026-10-02 growl: a buzzy low tone falling 175 to 125 Hz whose loudness flutters at 36 Hz.
    private const double GrowlStartHz = 175;
    private const double GrowlEndHz = 125;
    private const double GrowlLevel = 0.36;
    private const double GrowlFlutterHz = 36;
    private const double GrowlFlutterDepth = 0.55;
    private const int GrowlHarmonics = 5;
    // 2026-09-22 important-enemy swoop: the deepest voiced falling resonance, with only a little air.
    private const double SwoopStartHz = 480;
    private const double SwoopEndHz = 110;
    private const double SwoopShimmerRatio = 1.61;
    private const double SwoopLevel = 0.29;
    private const double SwoopMetalDepth = 0.65;
    private const double SwoopBody = 0.88;
    private const double SwoopAir = 0.12;
    private const double SwoopAirCutoffHz = 1500;
    private const uint NoiseSeed = 0x6D2B79F5; // D2R's fixed nonzero xorshift seed keeps the air reproducible.

    internal static float[] Synthesize(EnemyCueKind kind, float pan, float forward, float gain)
    {
        double safeGain = CuePlacement.Gain(gain);
        var placement = new CuePlacement(pan, forward);
        double airBlend = 1 - Math.Exp(-2 * Math.PI * SwoopAirCutoffHz * placement.Pitch / SampleRate);
        int frames = (int)Math.Round(DurationSeconds * SampleRate);
        var samples = new float[frames * Channels];
        double phase = 0, air = 0;
        uint noise = NoiseSeed;
        for (int frame = 0; frame < frames; frame++)
        {
            double progress = (double)frame / (frames - 1);
            double swell = Math.Sin(Math.PI * progress);
            double sample;
            if (kind == EnemyCueKind.Boss)
            {
                noise ^= noise << 13;
                noise ^= noise >> 17;
                noise ^= noise << 5;
                air += airBlend * ((double)noise / uint.MaxValue * 2 - 1 - air);
                double remaining = 1 - progress;
                phase += 2 * Math.PI * (SwoopEndHz + (SwoopStartHz - SwoopEndHz) * remaining * remaining)
                    * placement.Pitch / SampleRate;
                double body = Math.Sin(phase + SwoopMetalDepth * Math.Sin(phase * SwoopShimmerRatio));
                sample = SwoopLevel * swell * swell * (SwoopBody * body + SwoopAir * air);
            }
            else
            {
                phase += 2 * Math.PI * (GrowlStartHz + (GrowlEndHz - GrowlStartHz) * progress)
                    * placement.Pitch / SampleRate;
                double body = 0, norm = 0;
                for (int harmonic = 1; harmonic <= GrowlHarmonics; harmonic++)
                {
                    body += Math.Sin(harmonic * phase) / harmonic;
                    norm += 1.0 / harmonic;
                }
                double seconds = (double)frame / SampleRate;
                double flutter = 1 - GrowlFlutterDepth * (0.5 + 0.5 * Math.Sin(2 * Math.PI * GrowlFlutterHz * seconds));
                sample = GrowlLevel * swell * swell * flutter * body / norm;
            }
            sample *= safeGain;
            samples[frame * Channels] = (float)Math.Clamp(sample * placement.Left, -1, 1);
            samples[frame * Channels + 1] = (float)Math.Clamp(sample * placement.Right, -1, 1);
        }
        return samples;
    }
}
