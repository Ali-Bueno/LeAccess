using System;
using NAudio.Wave;

namespace LeAccess.Accessibility.Audio;

// The low-life warning: the start sound once on entering low life, then the loop while life stays low, gliding in and out.
internal sealed class LowLifeMixer : ISampleProvider
{
    internal const int SampleRate = LowLifeClips.SampleRate, Channels = 2;
    // Below about -80 dB the faded loop is inaudible; it stops there and starts from its beginning next time.
    internal const float Silent = 1e-4f;
    // Equal-power centre, as the loop mixer places a sound straight ahead.
    private static readonly float Centre = MathF.Sqrt(0.5f);
    private readonly object _gate = new();
    private readonly float[] _start, _loop;
    private readonly Func<float> _master;
    private readonly float _glide;
    private int _startOffset, _loopOffset;
    private float _gain, _loopGain, _target;
    private bool _low;

    /// <summary>Mono clips at 48 kHz; the loop's gain glides with the given time constant; master is read on the audio thread.</summary>
    internal LowLifeMixer(float[] start, float[] loop, double glideSeconds, Func<float> master)
    {
        _start = start ?? Array.Empty<float>();
        _loop = loop ?? Array.Empty<float>();
        _startOffset = _start.Length;
        _master = master ?? (() => 1f);
        _glide = glideSeconds > 0 ? (float)(1 - Math.Exp(-1.0 / (glideSeconds * SampleRate))) : 1f;
    }

    public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels);

    /// <summary>Whether life is low by a cue's rule: alive and under the chosen percent.</summary>
    internal static bool IsLow(int percent, int below) => percent > 0 && percent < below;

    internal bool StartPlaying { get { lock (_gate) return _startOffset < _start.Length; } }
    internal bool LoopAudible { get { lock (_gate) return _loopGain > Silent || _target > 0; } }

    /// <summary>Main thread: whether life is low, and the cue's gain. Entering low life plays the start sound,
    /// unless it is still playing from a moment ago.</summary>
    internal void Set(bool low, float gain)
    {
        gain = float.IsFinite(gain) ? Math.Max(0, gain) : 0;
        lock (_gate)
        {
            if (low && !_low && _startOffset >= _start.Length) _startOffset = 0;
            _low = low;
            _gain = gain;
            _target = low ? gain : 0;
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        lock (_gate)
        {
            float level = _master() * Centre;
            int frames = count / Channels;
            for (int frame = 0; frame < frames; frame++)
            {
                float value = 0;
                if (_startOffset < _start.Length) value += _start[_startOffset++] * _gain;
                _loopGain += (_target - _loopGain) * _glide;
                if (_loop.Length > 0 && (_loopGain > Silent || _target > 0))
                {
                    value += _loop[_loopOffset] * _loopGain;
                    _loopOffset = (_loopOffset + 1) % _loop.Length;
                }
                else
                {
                    _loopGain = 0;
                    _loopOffset = 0;
                }
                // NAudio may supply a byte-backed WaveBuffer view: write sample by sample.
                float sample = Math.Clamp(value * level, -1f, 1f);
                buffer[offset + frame * Channels] = sample;
                buffer[offset + frame * Channels + 1] = sample;
            }
            for (int index = frames * Channels; index < count; index++) buffer[offset + index] = 0;
        }
        return count;
    }
}
