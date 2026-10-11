using System;
using LeAccess.Accessibility.Navigation;
using NAudio.Wave;

namespace LeAccess.Accessibility.Audio;

/// <summary>Four continuous wall sounds, one soft band of air per screen direction, louder as a wall comes closer
/// (Grimdark's design; reference/navigation-radar.md#warm-wall-sounds-2026-10-11).</summary>
public sealed class WallToneSynth : ISampleProvider
{
    public const int SampleRate = 48000;
    private const int Channels = 2;
    // Band centres: Grimdark's warm loop set (ahicks92/grimdark assets/audio/walltones/1, about 945/528/266 Hz) lowered
    // a little more, as the user found the bright tones harsh; ahead, sides, behind in RadarDirection order.
    internal static readonly double[] Frequencies = { 800, 450, 450, 250 };
    private static readonly double[] Pans = { 0, -1, 1, 0 };
    // Grimdark lifts the low band behind 3 dB by ear (src/screens/in_game.cpp).
    private static readonly double[] Trims = { 1, 1, 1, Math.Pow(10, 3 / 20.0) };
    // Two band-pass stages at this Q give a band about half an octave wide: a pitch you can place, heard as air.
    private const double BandQ = 1.8;
    // Rolls off what the band-pass skirts let through above the bands, so nothing hisses.
    private const double WarmthCutoff = 1800;
    /// <summary>RMS of one direction at a touching wall, before the user level (about -28 dBFS, the old tones' level).</summary>
    public const double ContactRms = 0.04;
    // Half the radar poll: a step between polls glides instead of clicking.
    private const double GlideSeconds = 0.025;
    private static readonly double Glide = 1 - Math.Exp(-1 / (GlideSeconds * SampleRate));

    private readonly NoiseBand[] _bands = new NoiseBand[RadarEngine.DirectionCount];
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
            _bands[i] = new NoiseBand(Frequencies[i], (uint)(0x9E3779B9u * (i + 1)));
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
        double level = ContactRms * (float.IsFinite(userLevel) ? Math.Max(0, userLevel) : 0);
        for (int frame = 0; frame + 1 < count; frame += Channels)
        {
            double left = 0, right = 0;
            for (int i = 0; i < _bands.Length; i++)
            {
                _gains[i] += (_targets[i] - _gains[i]) * Glide;
                // Silent bands keep running, so a returning wall starts smoothly.
                double value = _gains[i] * Trims[i] * _bands[i].Next();
                left += value * _left[i];
                right += value * _right[i];
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

    /// <summary>White noise through two band-pass stages and a gentle low-pass, scaled to unit RMS.</summary>
    private sealed class NoiseBand
    {
        // Variance of white noise uniform in [-1, 1).
        private const double UniformVariance = 1 / 3.0;
        private readonly double _b0, _a1, _a2, _smooth, _scale;
        private Stage _first, _second;
        private double _low;
        private uint _seed;

        internal NoiseBand(double centre, uint seed)
        {
            // RBJ band-pass with a constant 0 dB peak: b1 = 0, b2 = -b0.
            double w0 = 2 * Math.PI * centre / SampleRate, alpha = Math.Sin(w0) / (2 * BandQ), a0 = 1 + alpha;
            _b0 = alpha / a0;
            _a1 = -2 * Math.Cos(w0) / a0;
            _a2 = (1 - alpha) / a0;
            _smooth = 1 - Math.Exp(-2 * Math.PI * WarmthCutoff / SampleRate);
            _scale = 1 / Math.Sqrt(ImpulseEnergy() * UniformVariance);
            _seed = seed == 0 ? 1 : seed;
        }

        internal double Next()
        {
            _seed ^= _seed << 13;
            _seed ^= _seed >> 17;
            _seed ^= _seed << 5;
            return Filter(_seed / 2147483648.0 - 1) * _scale;
        }

        private double Filter(double input)
        {
            double band = _second.Process(_first.Process(input, _b0, _a1, _a2), _b0, _a1, _a2);
            _low += (band - _low) * _smooth;
            return _low;
        }

        // The chain's output variance is the input variance times the energy of its impulse response.
        private double ImpulseEnergy()
        {
            double energy = 0;
            for (int n = 0; n < SampleRate; n++)
            {
                double h = Filter(n == 0 ? 1 : 0);
                energy += h * h;
            }
            _first = _second = default;
            _low = 0;
            return energy;
        }

        private struct Stage
        {
            private double _x1, _x2, _y1, _y2;

            internal double Process(double input, double b0, double a1, double a2)
            {
                double output = b0 * (input - _x2) - a1 * _y1 - a2 * _y2;
                _x2 = _x1; _x1 = input;
                _y2 = _y1; _y1 = output;
                return output;
            }
        }
    }
}
