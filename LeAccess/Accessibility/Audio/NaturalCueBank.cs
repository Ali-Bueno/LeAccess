using System;
using System.IO;

namespace LeAccess.Accessibility.Audio;

internal enum NaturalCueKind
{
    Gold, Equipment, Chest, Stash, Portal, ZoneTransition, Discovery, UnexploredRoom, InsideUnexploredRoom
}

internal static class NaturalCueBank
{
    internal const int SampleRate = 48000;
    internal const int Channels = 2;
    // Sound design retains the existing world cue level and rear pitch distinction.
    private const float PeakLevel = 0.24f;
    private const double RearPitch = 0.8;
    private const double AttackSeconds = 0.004;
    private const double ReleaseSeconds = 0.015;
    private static readonly string[] ResourceNames =
    {
        "gold", "equipment", "chest", "stash", "portal", "zone_transition",
        "discovery", "unexplored_room", "inside_unexplored_room"
    };
    private static readonly Lazy<float[]>[] Clips = CreateClips();

    internal static string LastFailure { get; private set; }

    internal static double Duration(NaturalCueKind kind)
        => (double)GetClip(kind).Length / SampleRate;

    internal static float[] Build(NaturalCueKind kind, float pan, float forward, float gain)
    {
        TryBuild(kind, pan, forward, gain, out var samples);
        return samples;
    }

    internal static bool TryBuild(NaturalCueKind kind, float pan, float forward, float gain, out float[] samples)
    {
        float[] clip = GetClip(kind);
        samples = Array.Empty<float>();
        if (clip.Length == 0) return false;
        double safeGain = float.IsFinite(gain) ? Math.Clamp(gain, 0, 1) : 0;
        double rightAxis = float.IsFinite(pan) ? Math.Clamp(pan, -1, 1) : 0;
        double frontAxis = float.IsFinite(forward) ? Math.Clamp(forward, -1, 1) : 1;
        double pitch = 1 + (RearPitch - 1) * Math.Clamp(-frontAxis, 0, 1);
        double left = Math.Sqrt((1 - rightAxis) / 2) * safeGain;
        double right = Math.Sqrt((1 + rightAxis) / 2) * safeGain;
        samples = new float[clip.Length * Channels];
        for (int frame = 0; frame < clip.Length; frame++)
        {
            double source = frame * pitch;
            int index = (int)source;
            double blend = source - index;
            double value = clip[index] + (clip[Math.Min(index + 1, clip.Length - 1)] - clip[index]) * blend;
            double edge = Math.Min(1, frame / (AttackSeconds * SampleRate))
                * Math.Min(1, (clip.Length - 1 - frame) / (ReleaseSeconds * SampleRate));
            value *= edge;
            samples[frame * Channels] = (float)(value * left);
            samples[frame * Channels + 1] = (float)(value * right);
        }
        return true;
    }

    private static float[] GetClip(NaturalCueKind kind)
    {
        int index = (int)kind;
        return index >= 0 && index < Clips.Length ? Clips[index].Value : Array.Empty<float>();
    }

    private static Lazy<float[]>[] CreateClips()
    {
        var clips = new Lazy<float[]>[ResourceNames.Length];
        for (int index = 0; index < clips.Length; index++)
        {
            string name = ResourceNames[index];
            clips[index] = new Lazy<float[]>(() => LoadClip(name));
        }
        return clips;
    }

    private static float[] LoadClip(string name)
    {
        try
        {
            using Stream stream = typeof(NaturalCueBank).Assembly
                .GetManifestResourceStream($"LeAccess.Sounds.{name}.wav");
            float[] samples = PcmWaveReader.ReadMono48(stream);
            float peak = 0;
            foreach (float sample in samples) peak = Math.Max(peak, Math.Abs(sample));
            if (peak == 0) throw new InvalidDataException("Cue contains only silence.");
            for (int frame = 0; frame < samples.Length; frame++) samples[frame] *= PeakLevel / peak;
            return samples;
        }
        catch (Exception ex)
        {
            LastFailure = $"{name}: {ex.Message}";
            return Array.Empty<float>();
        }
    }
}
