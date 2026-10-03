using System;

namespace LeAccess.Accessibility.Audio;

public static class NpcCueSynthesizer
{
    // D2RAccess entity_cue_synth.cpp, NPC tap retained in the 2026-09-22 listening revision.
    public const int SampleRate = 48000;
    public const int Channels = 2;
    public const double DurationSeconds = 0.084;
    private const double AttackSeconds = 0.0035;
    private const double ReleaseSeconds = 0.012;
    private const double Level = 0.32;
    private const double TextureCutoffHz = 1800;
    private const double BodyLevel = 0.75;
    private const double UpperLevel = 0.30;
    private const double TopLevel = 0.14;
    private const double UpperRatio = 1.57;
    private const double TopRatio = 2.31;
    private const double FrequencyHz = 850;
    private const double DecaySeconds = 0.013;
    private const double TextureLevel = 0.38;
    private const uint NoiseMultiplier = 1664525;
    private const uint NoiseIncrement = 1013904223;
    private const double NoiseMidpoint = 32767.5;

    public static float[] Synthesize(float pan, float forward, float gain)
    {
        double safeGain = CuePlacement.Gain(gain);
        var placement = new CuePlacement(pan, forward);
        double pitch = placement.Pitch, left = placement.Left, right = placement.Right;
        double textureBlend = 1 - Math.Exp(-2 * Math.PI * TextureCutoffHz * pitch / SampleRate);
        double texture = 0;
        int frames = (int)(DurationSeconds * SampleRate);
        var samples = new float[frames * Channels];
        for (int frame = 0; frame < frames; frame++)
        {
            double seconds = (double)frame / SampleRate;
            uint noise = unchecked((uint)frame * NoiseMultiplier + NoiseIncrement);
            noise ^= noise >> 16;
            double noiseSample = (noise & ushort.MaxValue) / NoiseMidpoint - 1;
            texture += textureBlend * (noiseSample - texture);
            double phase = 2 * Math.PI * FrequencyHz * pitch * seconds;
            double body = BodyLevel * Math.Sin(phase) + UpperLevel * Math.Sin(UpperRatio * phase)
                + TopLevel * Math.Sin(TopRatio * phase);
            double envelope = Math.Clamp(seconds / AttackSeconds, 0, 1)
                * Math.Clamp((DurationSeconds - seconds - 1.0 / SampleRate) / ReleaseSeconds, 0, 1)
                * Math.Exp(-seconds / DecaySeconds);
            double sample = Level * envelope * (body + TextureLevel * texture) * safeGain;
            samples[frame * Channels] = (float)Math.Clamp(sample * left, -1, 1);
            samples[frame * Channels + 1] = (float)Math.Clamp(sample * right, -1, 1);
        }
        return samples;
    }
}
