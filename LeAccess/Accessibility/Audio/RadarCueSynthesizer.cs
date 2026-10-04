using System;
using LeAccess.Accessibility.Navigation;

namespace LeAccess.Accessibility.Audio;

public static class RadarCueSynthesizer
{
    public const int SampleRate = 48000;
    public const int Channels = 2;
    public const double CueGapSeconds = 0.05;

    // Sound design ported from D2RAccess radar_synth.cpp; see reference/navigation-radar.md.
    private const double WallDuration = 0.120;
    private const double BreathDuration = 0.180;
    private const double Attack = 0.003;
    private const double Release = 0.015;
    private const double Level = 0.17;
    private const double WallFrequency = 260;
    private const double WallHarmonic = 1.5;
    private const double HarmonicLevel = 0.2;
    private const double WallDecay = 0.025;
    private const double DownPitch = 0.86;
    private const double PanWidth = 0.9;
    private const double BreathPeak = 0.7;
    private const double FilterQ = 3;
    private const double UpStartFrequency = 900;
    private const double UpEndFrequency = 2400;
    private const double DownStartFrequency = 600;
    private const double DownEndFrequency = 220;
    private const double SideFrequency = 1200;
    // D2R unexplored mark (radar.h, radar_synth.cpp): two pips after a short gap.
    private const double MarkFrequency = 900;
    private const double MarkGap = 0.030;
    private const double PipDuration = 0.035;
    private const double PipGap = 0.030;
    private const double MarkDecay = 0.03;
    private const double MarkHarmonic = 2.0;
    private const uint NoiseSeed = 1;
    private const uint NoiseMultiplier = 1664525;
    private const uint NoiseIncrement = 1013904223;
    private const double NoiseScale = 8388608.0;

    // An unexplored opening keeps its breath and ends in the mark, lowered for screen-down like the cue.
    public static float[] Synthesize(RadarState state, RadarDirection direction, float gain, bool unexplored = false)
    {
        if (state != RadarState.Open && state != RadarState.Blocked)
            return Array.Empty<float>();

        double level = Level * (float.IsFinite(gain) ? Math.Clamp(gain, 0f, 1f) : 0f);
        double pan = direction == RadarDirection.Left ? -PanWidth
            : direction == RadarDirection.Right ? PanWidth : 0;
        double left = Math.Sqrt((1 - pan) / 2);
        double right = Math.Sqrt((1 + pan) / 2);
        double[] mono = state == RadarState.Open ? Breath(direction, level * BreathPeak)
            : Wall(direction, level);
        if (unexplored && state == RadarState.Open) mono = WithMark(mono, direction, level);
        var samples = new float[mono.Length * Channels];
        for (int frame = 0; frame < mono.Length; frame++)
        {
            samples[frame * Channels] = (float)Math.Clamp(mono[frame] * left, -1, 1);
            samples[frame * Channels + 1] = (float)Math.Clamp(mono[frame] * right, -1, 1);
        }
        return samples;
    }

    private static double[] Wall(RadarDirection direction, double level)
    {
        var samples = new double[(int)(WallDuration * SampleRate)];
        double frequency = WallFrequency * (direction == RadarDirection.Down ? DownPitch : 1);
        for (int frame = 0; frame < samples.Length; frame++)
        {
            double time = (double)frame / SampleRate;
            double phase = 2 * Math.PI * frequency * time;
            double fade = Math.Min(1, (WallDuration - time) / Release);
            double envelope = Math.Min(1, time / Attack) * fade * Math.Exp(-time / WallDecay);
            samples[frame] = level * envelope
                * (Math.Sin(phase) + HarmonicLevel * Math.Sin(phase * WallHarmonic));
        }
        return samples;
    }

    private static double[] WithMark(double[] cue, RadarDirection direction, double level)
    {
        int gap = (int)(MarkGap * SampleRate), pip = (int)(PipDuration * SampleRate), between = (int)(PipGap * SampleRate);
        var samples = new double[cue.Length + gap + 2 * pip + between];
        Array.Copy(cue, samples, cue.Length);
        double frequency = MarkFrequency * (direction == RadarDirection.Down ? DownPitch : 1);
        for (int frame = 0; frame < 2 * pip + between; frame++)
        {
            int pipFrame = frame % (pip + between);
            if (pipFrame >= pip) continue;
            double time = (double)pipFrame / SampleRate;
            double phase = 2 * Math.PI * frequency * time;
            double fade = Math.Min(1, (PipDuration - time) / Release);
            double envelope = Math.Min(1, time / Attack) * fade * Math.Exp(-time / MarkDecay);
            samples[cue.Length + gap + frame] = level * envelope * (Math.Sin(phase) + HarmonicLevel * Math.Sin(phase * MarkHarmonic));
        }
        return samples;
    }

    private static double[] Breath(RadarDirection direction, double peak)
    {
        double startFrequency = direction == RadarDirection.Up ? UpStartFrequency
            : direction == RadarDirection.Down ? DownStartFrequency : SideFrequency;
        double endFrequency = direction == RadarDirection.Up ? UpEndFrequency
            : direction == RadarDirection.Down ? DownEndFrequency : SideFrequency;
        var samples = new double[(int)(BreathDuration * SampleRate)];
        uint seed = NoiseSeed;
        double low = 0, band = 0, loudest = 0;
        for (int frame = 0; frame < samples.Length; frame++)
        {
            double time = (double)frame / SampleRate;
            double centreFrequency = startFrequency
                * Math.Pow(endFrequency / startFrequency, time / BreathDuration);
            double tuning = 2 * Math.Sin(Math.PI * centreFrequency / SampleRate);
            seed = unchecked(seed * NoiseMultiplier + NoiseIncrement);
            double high = (seed >> 8) / NoiseScale - 1 - low - band / FilterQ;
            band += tuning * high;
            low += tuning * band;
            double swell = Math.Sin(Math.PI * time / BreathDuration);
            samples[frame] = swell * swell * Math.Min(1, (BreathDuration - time) / Release) * band;
            loudest = Math.Max(loudest, Math.Abs(samples[frame]));
        }
        if (loudest > 0)
            for (int frame = 0; frame < samples.Length; frame++)
                samples[frame] *= peak / loudest;
        return samples;
    }
}
