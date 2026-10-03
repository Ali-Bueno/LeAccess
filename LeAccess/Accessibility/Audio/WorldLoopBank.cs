using System;
using System.IO;

namespace LeAccess.Accessibility.Audio;

// Whole D2R loop recordings for the world loop mixer (D2RAccess ground_items.h SoundFiles).
internal static class WorldLoopBank
{
    internal const int SampleRate = NaturalCueBank.SampleRate;
    // D2RAccess: recordings peak-normalize to 0.9 before the near-source gain.
    private const float PeakLevel = 0.9f;
    // D2RAccess user listening, 2026-09-26: the chest sat about 4 LU under the loot loops, the stash too loud.
    private const float ChestLevelDb = 4, StashLevelDb = -4;
    // D2RAccess D2R_STASH_LOOP_SECONDS: the 0.3 s stash recording is padded to repeat once a second.
    private const double StashLoopSeconds = 1;
    // The longest supplied loop runs 15 s; this bounds a damaged resource.
    private const int MaximumFrames = SampleRate * 20;
    private static readonly Lazy<float[]>[] Clips = CreateClips();

    internal static string LastFailure { get; private set; }

    internal static bool Has(NaturalCueKind kind) => Get(kind).Length > 0;

    /// <summary>The shared mono loop at 48 kHz; empty when this kind has no loop or it failed to load.</summary>
    internal static float[] Get(NaturalCueKind kind)
    {
        int index = (int)kind;
        return index >= 0 && index < Clips.Length && Clips[index] != null ? Clips[index].Value : Array.Empty<float>();
    }

    private static string ResourceName(NaturalCueKind kind) => kind switch
    {
        NaturalCueKind.Gold => "gold",
        NaturalCueKind.Equipment => "equipment",
        NaturalCueKind.Chest => "chest",
        NaturalCueKind.Stash => "stash",
        NaturalCueKind.Portal => "portal",
        NaturalCueKind.ZoneTransition => "zone_transition",
        _ => null
    };

    private static Lazy<float[]>[] CreateClips()
    {
        var kinds = Enum.GetValues<NaturalCueKind>();
        var clips = new Lazy<float[]>[kinds.Length];
        foreach (var kind in kinds)
            if (ResourceName(kind) != null) clips[(int)kind] = new Lazy<float[]>(() => Load(kind));
        return clips;
    }

    private static float[] Load(NaturalCueKind kind)
    {
        string name = ResourceName(kind);
        try
        {
            using Stream stream = typeof(WorldLoopBank).Assembly
                .GetManifestResourceStream($"LeAccess.Sounds.Loops.{name}.wav");
            float[] samples = PcmWaveReader.ReadMono48(stream, MaximumFrames);
            float peak = 0;
            foreach (float sample in samples) peak = Math.Max(peak, Math.Abs(sample));
            if (peak == 0) throw new InvalidDataException("Loop contains only silence.");
            float levelDb = kind switch { NaturalCueKind.Chest => ChestLevelDb, NaturalCueKind.Stash => StashLevelDb, _ => 0 };
            float scale = PeakLevel / peak * MathF.Pow(10, levelDb / 20);
            int frames = kind == NaturalCueKind.Stash ? Math.Max(samples.Length, (int)(StashLoopSeconds * SampleRate)) : samples.Length;
            var loop = new float[frames];
            for (int frame = 0; frame < samples.Length; frame++) loop[frame] = samples[frame] * scale;
            return loop;
        }
        catch (Exception ex)
        {
            LastFailure = $"{name}: {ex.Message}";
            return Array.Empty<float>();
        }
    }
}
