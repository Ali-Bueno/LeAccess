using System;
using LeAccess.Accessibility.Audio;

namespace LeAccess.Accessibility.Exploration;

internal enum BeaconSound { Ping, Locked, Cleared, Arrived }

// Ported from d2r-access src/beacon_audio.cpp Synthesize: a soft mid-register blip, unlike the radar's
// knock and breath. A lock rises a fifth, a cancel falls, arrival rises on to the octave.
internal static class BeaconCueSynthesizer
{
    internal const int SampleRate = RadarCueSynthesizer.SampleRate;
    internal const int Channels = RadarCueSynthesizer.Channels;
    internal const double PingHz = 620;
    internal const double NoteSeconds = 0.080;
    internal const double GapSeconds = 0.060;
    internal const double RearPitch = 0.94;
    internal const double Level = 0.15;
    private const double Attack = 0.004, Release = 0.012, Decay = 0.035;
    private const double Harmonic = 2.0, HarmonicLevel = 0.15;
    private const double Fifth = 1.5, Octave = 2.0;
    // Menu click (not in D2R): the ping's octave cut to a tick, at half its level.
    internal const double ClickSeconds = 0.020;
    private const double ClickAttack = 0.001, ClickRelease = 0.005, ClickDecay = 0.006;

    // pan: -1 left .. +1 right; ahead: +1 in front .. -1 behind, which lowers the pitch.
    internal static float[] Synthesize(BeaconSound sound, float pan, float ahead, float gain)
    {
        double[] notes = sound switch
        {
            BeaconSound.Locked => new[] { PingHz, PingHz * Fifth },
            BeaconSound.Cleared => new[] { PingHz * Fifth, PingHz },
            BeaconSound.Arrived => new[] { PingHz, PingHz * Fifth, PingHz * Octave },
            _ => new[] { PingHz },
        };
        double behind = float.IsFinite(ahead) ? Math.Clamp(-ahead, 0, 1) : 0;
        double pitch = 1 + (RearPitch - 1) * behind;
        int noteFrames = (int)(NoteSeconds * SampleRate), gapFrames = (int)(GapSeconds * SampleRate);
        var samples = new float[(notes.Length * noteFrames + (notes.Length - 1) * gapFrames) * Channels];
        for (int note = 0; note < notes.Length; note++)
            Tone(samples, note * (noteFrames + gapFrames), noteFrames, notes[note] * pitch, Attack, Release, Decay,
                Level * Gain(gain), pan);
        return samples;
    }

    internal static double Duration(BeaconSound sound)
    {
        int notes = sound switch { BeaconSound.Locked or BeaconSound.Cleared => 2, BeaconSound.Arrived => 3, _ => 1 };
        int frames = notes * (int)(NoteSeconds * SampleRate) + (notes - 1) * (int)(GapSeconds * SampleRate);
        return (double)frames / SampleRate;
    }

    internal static float[] Click(float gain)
    {
        int frames = (int)(ClickSeconds * SampleRate);
        var samples = new float[frames * Channels];
        Tone(samples, 0, frames, PingHz * Octave, ClickAttack, ClickRelease, ClickDecay, Level / 2 * Gain(gain), 0);
        return samples;
    }

    private static double Gain(float gain) => float.IsFinite(gain) ? Math.Clamp(gain, 0f, 1f) : 0;

    private static void Tone(float[] samples, int startFrame, int frames, double hz, double attack, double release,
        double decay, double level, float pan)
    {
        double side = float.IsFinite(pan) ? Math.Clamp(pan, -1, 1) : 0;
        double left = Math.Sqrt((1 - side) / 2), right = Math.Sqrt((1 + side) / 2);
        double duration = (double)frames / SampleRate;
        for (int frame = 0; frame < frames; frame++)
        {
            double time = (double)frame / SampleRate;
            double phase = 2 * Math.PI * hz * time;
            double envelope = Math.Min(1, time / attack) * Math.Min(1, (duration - time) / release) * Math.Exp(-time / decay);
            double tone = level * envelope * (Math.Sin(phase) + HarmonicLevel * Math.Sin(phase * Harmonic));
            int index = (startFrame + frame) * Channels;
            samples[index] = (float)Math.Clamp(tone * left, -1, 1);
            samples[index + 1] = (float)Math.Clamp(tone * right, -1, 1);
        }
    }
}
