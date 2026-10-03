using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace LeAccess.Accessibility.Audio;

// Continuous voices that keep their loop position across updates (D2RAccess item_audio.cpp).
internal sealed class WorldLoopMixer : ISampleProvider
{
    internal const int Channels = 2;
    // D2RAccess GainSmoothing 0.25 per 20 ms update: roughly 80 ms to settle; this time constant matches it.
    internal const double SmoothingSeconds = 0.03;
    // Below this a voice whose target is silence is inaudible and is released.
    private const float ReleasedGain = 1e-4f;
    private readonly object _gate = new();
    private readonly List<Voice> _voices = new();
    private readonly Func<NaturalCueKind, float[]> _clips;
    private readonly double _smoothing;
    private long _frames;

    public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(WorldLoopBank.SampleRate, Channels);

    internal WorldLoopMixer(Func<NaturalCueKind, float[]> clips)
    {
        _clips = clips ?? throw new ArgumentNullException(nameof(clips));
        _smoothing = 1 - Math.Exp(-1 / (SmoothingSeconds * WorldLoopBank.SampleRate));
    }

    internal int ActiveVoices { get { lock (_gate) return _voices.Count; } }
    internal long FramesMixed { get { lock (_gate) return _frames; } }

    /// <summary>Main thread: the complete set that should sound; absent voices fade out.</summary>
    internal void SetVoices(IReadOnlyList<LoopVoice> targets)
    {
        lock (_gate)
        {
            foreach (var voice in _voices) voice.Wanted = false;
            foreach (var target in targets)
            {
                var voice = Find(target.Id, target.Kind);
                if (voice == null)
                {
                    float[] clip = _clips(target.Kind);
                    if (clip == null || clip.Length == 0) continue;
                    voice = new Voice(target.Id, target.Kind, clip, Finite(target.Pitch, 1));
                    _voices.Add(voice);
                }
                voice.Wanted = true;
                voice.TargetLeft = Finite(target.Left, 0);
                voice.TargetRight = Finite(target.Right, 0);
                voice.TargetPitch = Finite(target.Pitch, 1);
            }
            foreach (var voice in _voices)
                if (!voice.Wanted) voice.TargetLeft = voice.TargetRight = 0;
        }
    }

    /// <summary>Fades every voice out without a click.</summary>
    internal void Silence() => SetVoices(Array.Empty<LoopVoice>());

    public int Read(float[] buffer, int offset, int count)
    {
        Array.Clear(buffer, offset, count);
        int frames = count / Channels;
        lock (_gate)
        {
            for (int index = _voices.Count - 1; index >= 0; index--)
            {
                var voice = _voices[index];
                voice.Mix(buffer, offset, frames, _smoothing);
                if (!voice.Wanted && voice.Left < ReleasedGain && voice.Right < ReleasedGain) _voices.RemoveAt(index);
            }
            _frames += frames;
        }
        for (int index = offset; index < offset + frames * Channels; index++)
            buffer[index] = Math.Clamp(buffer[index], -1f, 1f);
        return count;
    }

    private Voice Find(int id, NaturalCueKind kind)
    {
        foreach (var voice in _voices)
            if (voice.Id == id && voice.Kind == kind) return voice;
        return null;
    }

    private static float Finite(float value, float fallback) => float.IsFinite(value) ? Math.Max(0, value) : fallback;

    private sealed class Voice
    {
        private readonly float[] _clip;
        private double _position;

        internal Voice(int id, NaturalCueKind kind, float[] clip, float pitch)
        {
            Id = id;
            Kind = kind;
            _clip = clip;
            Pitch = TargetPitch = pitch > 0 ? pitch : 1;
        }

        internal int Id { get; }
        internal NaturalCueKind Kind { get; }
        internal bool Wanted;
        internal float Left, Right, Pitch, TargetLeft, TargetRight, TargetPitch;

        internal void Mix(float[] buffer, int offset, int frames, double smoothing)
        {
            int length = _clip.Length;
            for (int frame = 0; frame < frames; frame++)
            {
                Left += (float)((TargetLeft - Left) * smoothing);
                Right += (float)((TargetRight - Right) * smoothing);
                Pitch += (float)((TargetPitch - Pitch) * smoothing);
                int current = (int)_position;
                int next = current + 1 == length ? 0 : current + 1;
                float sample = (float)(_clip[current] + (_clip[next] - _clip[current]) * (_position - current));
                buffer[offset + frame * Channels] += sample * Left;
                buffer[offset + frame * Channels + 1] += sample * Right;
                _position += Pitch > 0 ? Pitch : 1;
                while (_position >= length) _position -= length;
            }
        }
    }
}
