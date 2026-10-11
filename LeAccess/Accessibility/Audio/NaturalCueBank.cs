using System;
using System.IO;

namespace LeAccess.Accessibility.Audio;

internal static class NaturalCueBank
{
    internal const int SampleRate = 48000;
    internal const int Channels = 2;
    // Sound design retains the existing world cue level; CuePlacement supplies the rear pitch distinction.
    private const float PeakLevel = 0.24f;
    private const double AttackSeconds = 0.004;
    private const double ReleaseSeconds = 0.015;
    // Lengths of the short cues that used to ship as separate files (reference/natural-world-sounds.md).
    internal const double GoldSeconds = 0.30, EquipmentSeconds = 0.30, ContainerSeconds = 0.24,
        TransitionSeconds = 0.30, RouteSeconds = 0.35;
    // A fallback cue is the first Seconds of the kind's loop; null plays the one-shot in Sounds/ whole.
    private readonly record struct Excerpt(string Resource, double? Seconds);
    private static readonly Excerpt[] Excerpts =
    {
        new("gold", GoldSeconds), new("equipment", EquipmentSeconds), new("chest", ContainerSeconds),
        new("stash", ContainerSeconds), new("portal", TransitionSeconds), new("zone_transition", TransitionSeconds),
        new("discovery", null), new("unexplored_room", RouteSeconds), new("inside_unexplored_room", null)
    };
    // The longest one-shot, D2R's discovery chime, runs 2.85 s; this bounds a damaged resource.
    private const int MaximumFrames = SampleRate * 3;
    // The longest supplied loop runs 28 s; this bounds a damaged loop resource.
    internal const int MaximumLoopFrames = SampleRate * 30;
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
        double safeGain = CuePlacement.Gain(gain);
        var placement = new CuePlacement(pan, forward);
        double pitch = placement.Pitch;
        double left = placement.Left * safeGain;
        double right = placement.Right * safeGain;
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
        var clips = new Lazy<float[]>[Excerpts.Length];
        for (int index = 0; index < clips.Length; index++)
        {
            var excerpt = Excerpts[index];
            clips[index] = new Lazy<float[]>(() => LoadClip(excerpt));
        }
        return clips;
    }

    private static float[] LoadClip(Excerpt excerpt)
    {
        string name = excerpt.Resource;
        try
        {
            bool fromLoop = excerpt.Seconds.HasValue;
            using Stream stream = typeof(NaturalCueBank).Assembly
                .GetManifestResourceStream($"LeAccess.Sounds.{(fromLoop ? "Loops." : "")}{name}.wav");
            float[] samples = PcmWaveReader.ReadMono48(stream, fromLoop ? MaximumLoopFrames : MaximumFrames);
            if (fromLoop) samples = samples[..Math.Min(samples.Length, (int)Math.Round(excerpt.Seconds.Value * SampleRate))];
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
