using System;
using LeAccess.Accessibility.Navigation;
using NAudio.Wave;

namespace LeAccess.Accessibility.Audio;

/// <summary>Four continuous wall tones, one per screen direction, louder as a wall comes closer
/// (Grimdark's design, softer; reference/navigation-radar.md#wall-tones-2026-10-11).</summary>
public sealed class WallToneSynth : ISampleProvider
{
    public const int SampleRate = 48000;
    private const int Channels = 2;
    // Grimdark's loops by zero crossings (ahicks92/grimdark assets/audio/walltones/2): ahead, sides, behind; RadarDirection order.
    internal static readonly double[] Frequencies = { 1265, 856, 856, 432 };
    private static readonly double[] Pans = { 0, -1, 1, 0 };
    // Grimdark lifts the low behind tone 3 dB by ear (src/screens/in_game.cpp).
    private static readonly double[] Trims = { 1, 1, 1, Math.Pow(10, 3 / 20.0) };
    /// <summary>Peak at a touching wall: about 6 dB under Grimdark's loops (-20 dBFS RMS), as the user asked for softer tones.</summary>
    public const double ContactLevel = 0.07;
    // A little brightness, like Grimdark's authored loops, keeps soft tones audible; the sum is normalised to the peak.
    private const double SecondHarmonic = 0.25, ThirdHarmonic = 0.1;
    private const double HarmonicPeak = 1 + SecondHarmonic + ThirdHarmonic;
    // Half the radar poll: a step between polls glides instead of clicking.
    private const double GlideSeconds = 0.025;
    private static readonly double Glide = 1 - Math.Exp(-1 / (GlideSeconds * SampleRate));

    private readonly double[] _phases = new double[RadarEngine.DirectionCount];
    private readonly double[] _gains = new double[RadarEngine.DirectionCount];
    private readonly float[] _targets = new float[RadarEngine.DirectionCount];
    private readonly double[] _left = new double[RadarEngine.DirectionCount], _right = new double[RadarEngine.DirectionCount];
    private readonly Func<float> _level;

    public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels);

    /// <param name="level">User level read once per buffer: master times the sonar volume in play.</param>
    public WallToneSynth(Func<float> level)
    {
        _level = level ?? throw new ArgumentNullException(nameof(level));
        for (int i = 0; i < Pans.Length; i++)
        {
            // Equal-power pan, as Grimdark's mixer.
            double angle = (Pans[i] + 1) * Math.PI / 4;
            _left[i] = Math.Cos(angle);
            _right[i] = Math.Sin(angle);
        }
    }

    /// <summary>Grimdark's distance curve: silent at the range, full at a touching wall, (1 - d/range)^2 between.</summary>
    public static float Gain(float distance, float range)
    {
        if (!float.IsFinite(distance) || !float.IsFinite(range) || range <= 0 || distance >= range) return 0;
        float closeness = 1 - Math.Max(0, distance) / range;
        return closeness * closeness;
    }

    public void SetTarget(RadarDirection direction, float gain) =>
        _targets[(int)direction] = float.IsFinite(gain) ? Math.Clamp(gain, 0f, 1f) : 0f;

    public void Silence() => Array.Clear(_targets, 0, _targets.Length);

    public int Read(float[] buffer, int offset, int count)
    {
        float userLevel = _level();
        double level = ContactLevel / HarmonicPeak * (float.IsFinite(userLevel) ? Math.Max(0, userLevel) : 0);
        for (int frame = 0; frame + 1 < count; frame += Channels)
        {
            double left = 0, right = 0;
            for (int i = 0; i < _phases.Length; i++)
            {
                _gains[i] += (_targets[i] - _gains[i]) * Glide;
                double angle = 2 * Math.PI * _phases[i];
                double value = _gains[i] * Trims[i] *
                    (Math.Sin(angle) + SecondHarmonic * Math.Sin(2 * angle) + ThirdHarmonic * Math.Sin(3 * angle));
                left += value * _left[i];
                right += value * _right[i];
                // Silent tones keep their phase, so a returning wall starts smoothly.
                _phases[i] += Frequencies[i] / SampleRate;
                if (_phases[i] >= 1) _phases[i] -= 1;
            }
            buffer[offset + frame] = (float)Math.Clamp(left * level, -1, 1);
            buffer[offset + frame + 1] = (float)Math.Clamp(right * level, -1, 1);
        }
        return count;
    }

    /// <summary>A glossary preview: one direction at a touching wall, fading in and out.</summary>
    public static float[] Preview(RadarDirection direction, float level, double seconds)
    {
        if (!(seconds > 0) || !float.IsFinite(level) || level <= 0) return Array.Empty<float>();
        var synth = new WallToneSynth(() => level);
        var samples = new float[(int)(seconds * SampleRate) * Channels];
        int fade = Math.Min(samples.Length, (int)(GlideSeconds * 4 * SampleRate) * Channels);
        synth.SetTarget(direction, 1);
        synth.Read(samples, 0, samples.Length - fade);
        synth.Silence();
        synth.Read(samples, samples.Length - fade, fade);
        return samples;
    }
}
