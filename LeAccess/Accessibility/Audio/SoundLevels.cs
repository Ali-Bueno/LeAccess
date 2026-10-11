using System;
using LeAccess.Accessibility.Combat;

namespace LeAccess.Accessibility.Audio;

/// <summary>The player's live sound levels: master, per world kind (volume, range) and per enemy type.</summary>
internal static partial class SoundLevels
{
    internal const string AudioSection = "Audio", MasterKey = "MasterVolume";
    internal const string VolumeSection = "World object sound volumes", RangeSection = "World object sound ranges";
    internal const string EnemySection = "Enemy cues", SonarSection = "Navigation audio", SonarKey = "SonarVolume";
    internal const int MaxVolumePercent = 200, MaxMasterPercent = 100, MaxRangeMetres = 30;

    /// <summary>A sound kind with a config key; HasRange is false for one-shots that have no distance.</summary>
    internal readonly record struct WorldKind(NaturalCueKind Kind, string Key, bool HasRange = true);

    internal static readonly WorldKind[] WorldKinds =
    {
        new(NaturalCueKind.Gold, "Gold"), new(NaturalCueKind.Equipment, "Equipment"),
        new(NaturalCueKind.MagicEquipment, "MagicEquipment"), new(NaturalCueKind.RareEquipment, "RareEquipment"),
        new(NaturalCueKind.LegendaryEquipment, "LegendaryEquipment"), new(NaturalCueKind.Potion, "Potion"),
        new(NaturalCueKind.Tome, "Tome"), new(NaturalCueKind.Rune, "Rune"), new(NaturalCueKind.Gem, "Glyph"),
        new(NaturalCueKind.Scroll, "AffixShard"), new(NaturalCueKind.QuestItem, "QuestItem"),
        new(NaturalCueKind.Chest, "Chest"), new(NaturalCueKind.Stash, "Stash"), new(NaturalCueKind.Portal, "Portal"),
        new(NaturalCueKind.ZoneTransition, "ZoneExit"), new(NaturalCueKind.Shrine, "Shrine"),
        new(NaturalCueKind.Nemesis, "Nemesis"), new(NaturalCueKind.UnexploredRoom, "UnexploredRoom"),
        new(NaturalCueKind.Discovery, "Discovery", false), new(NaturalCueKind.InsideUnexploredRoom, "RoomEntered", false),
    };

    /// <summary>Config keys by EnemyCueKind order.</summary>
    internal static readonly string[] EnemyKeys = { "BossVolume", "EliteVolume", "OrdinaryVolume" };

    private static volatile float _master = 1, _sonar = 1;
    private static readonly float[] Volumes = Filled(Enum.GetValues<NaturalCueKind>().Length);
    private static readonly float[] Ranges = new float[Enum.GetValues<NaturalCueKind>().Length];
    private static readonly float[] Enemies = Filled(Enum.GetValues<EnemyCueKind>().Length);

    /// <summary>Scale on every mod sound at its output stage, 0 to 1; speech is not affected.</summary>
    internal static float Master => _master;

    /// <summary>Multiplier of a kind's loop and repeated cue, 0 (silent) to 2.</summary>
    internal static float Volume(NaturalCueKind kind) => InRange((int)kind, Volumes.Length) ? Volumes[(int)kind] : 1;

    /// <summary>Metres the kind sounds out to: its own range, or groupRange when it follows the group.</summary>
    internal static float Range(NaturalCueKind kind, float groupRange) =>
        InRange((int)kind, Ranges.Length) && Ranges[(int)kind] > 0 ? Ranges[(int)kind] : groupRange;

    /// <summary>The farthest range among the audible kinds the filter accepts; 0 when none.</summary>
    internal static float LargestRange(float groupRange, Func<NaturalCueKind, bool> accept)
    {
        float largest = 0;
        foreach (var world in WorldKinds)
            if (world.HasRange && Volume(world.Kind) > 0 && accept(world.Kind))
                largest = Math.Max(largest, Range(world.Kind, groupRange));
        return largest;
    }

    internal static float Enemy(EnemyCueKind kind) => InRange((int)kind, Enemies.Length) ? Enemies[(int)kind] : 1;

    /// <summary>Volume of the continuous wall sonar tones, 0 to 1.</summary>
    internal static float Sonar => _sonar;

    internal static void SetMaster(int percent) => _master = Math.Clamp(percent, 0, MaxMasterPercent) / 100f;

    internal static void SetSonar(int percent) => _sonar = Math.Clamp(percent, 0, MaxMasterPercent) / 100f;

    internal static void SetVolume(NaturalCueKind kind, int percent)
    {
        if (InRange((int)kind, Volumes.Length)) Volumes[(int)kind] = Math.Clamp(percent, 0, MaxVolumePercent) / 100f;
    }

    internal static void SetRange(NaturalCueKind kind, int metres)
    {
        if (InRange((int)kind, Ranges.Length)) Ranges[(int)kind] = Math.Clamp(metres, 0, MaxRangeMetres);
    }

    internal static void SetEnemy(EnemyCueKind kind, int percent)
    {
        if (InRange((int)kind, Enemies.Length)) Enemies[(int)kind] = Math.Clamp(percent, 0, MaxVolumePercent) / 100f;
    }

    /// <summary>Back to the defaults: full master, every kind at 100% and following its group.</summary>
    internal static void Reset()
    {
        _master = _sonar = 1;
        Array.Fill(Volumes, 1f);
        Array.Clear(Ranges);
        Array.Fill(Enemies, 1f);
    }

    private static bool InRange(int index, int length) => index >= 0 && index < length;

    private static float[] Filled(int length)
    {
        var array = new float[length];
        Array.Fill(array, 1f);
        return array;
    }
}
