using System;
using System.IO;
using LeAccess.Accessibility.WorldCues;

namespace LeAccess.Accessibility.Audio;

// Whole D2R and Last Epoch recordings for the world loop mixer (D2RAccess ground_items.h SoundFiles).
internal static class WorldLoopBank
{
    internal const int SampleRate = NaturalCueBank.SampleRate;
    // D2RAccess: recordings peak-normalize to 0.9 before the near-source gain.
    private const float PeakLevel = 0.9f;
    // D2RAccess user listening, 2026-09-26: the chest sat about 4 LU under the loot loops, the stash too loud.
    private const float ChestLevelDb = 4, StashLevelDb = -4;
    // User request 2026-10-09: equipment a little quieter so the radar carries through combat.
    private const float EquipmentLevelDb = -3;
    // One-shots (LE gold, potion, tome; D2R rune, gem, scroll clicks) sit 5-15 dB under the loops at equal peak
    // (reference/sound-design.md); they take all the mix headroom: the summed loop gain times their peak stays within full scale.
    private static readonly float OneShotLevelDb = 20 * MathF.Log10(1 / (WorldLoopSelection.MixGainLimit * PeakLevel));
    // D2RAccess D2R_STASH_LOOP_SECONDS: recordings shorter than a second are padded to repeat once a second.
    private const double ShortLoopSeconds = 1;
    private const int MaximumFrames = NaturalCueBank.MaximumLoopFrames;
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
        NaturalCueKind.Potion => "potion",
        NaturalCueKind.Tome => "tome",
        NaturalCueKind.MagicEquipment => "magic_equipment",
        NaturalCueKind.RareEquipment => "rare_equipment",
        NaturalCueKind.LegendaryEquipment => "legendary_equipment",
        NaturalCueKind.Rune => "rune",
        NaturalCueKind.Gem => "gem",
        NaturalCueKind.Scroll => "scroll",
        NaturalCueKind.QuestItem => "quest_item",
        NaturalCueKind.Shrine => "shrine",
        NaturalCueKind.Nemesis => "nemesis",
        NaturalCueKind.UnexploredRoom => "unexplored_room",
        _ => null
    };

    private static float LevelDb(NaturalCueKind kind) => kind switch
    {
        NaturalCueKind.Chest => ChestLevelDb,
        NaturalCueKind.Stash => StashLevelDb,
        NaturalCueKind.Equipment or NaturalCueKind.MagicEquipment or NaturalCueKind.RareEquipment or
            NaturalCueKind.LegendaryEquipment => EquipmentLevelDb,
        NaturalCueKind.Gold or NaturalCueKind.Potion or NaturalCueKind.Tome or
            NaturalCueKind.Rune or NaturalCueKind.Gem or NaturalCueKind.Scroll => OneShotLevelDb,
        _ => 0
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
            float scale = PeakLevel / peak * MathF.Pow(10, LevelDb(kind) / 20);
            int frames = Math.Max(samples.Length, (int)(ShortLoopSeconds * SampleRate));
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
