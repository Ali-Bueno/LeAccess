using System.Collections.Generic;

namespace LeAccess.Accessibility.Audio;

/// <summary>One sound the mod plays: its text keys are sound.Id and sound.Id.details, and the config entries set its level.</summary>
internal readonly record struct SoundGlossaryEntry(string Id, string VolumeSection, string VolumeKey,
    string RangeSection = null, string RangeKey = null);

/// <summary>Every distinct sound, in the order a player meets them.</summary>
internal static class SoundGlossary
{
    private const string RadarSection = "Reactive radar", NpcSection = "NPC sounds",
        BeaconSection = "Exploration beacon";

    internal static IReadOnlyList<SoundGlossaryEntry> Entries { get; } = new SoundGlossaryEntry[]
    {
        new("RadarOpen", RadarSection, "Volume"),
        new("RadarBlocked", RadarSection, "Volume"),
        new("RadarUnexplored", RadarSection, "Volume"),
        new("EnemyBoss", SoundLevels.EnemySection, SoundLevels.EnemyKeys[0]),
        new("EnemyElite", SoundLevels.EnemySection, SoundLevels.EnemyKeys[1]),
        new("EnemyOrdinary", SoundLevels.EnemySection, SoundLevels.EnemyKeys[2]),
        new("NpcTap", NpcSection, "Volume"),
        World("Gold"), World("Equipment"), World("MagicEquipment"), World("RareEquipment"),
        World("LegendaryEquipment"), World("Potion"), World("Tome"), World("Rune"), World("Glyph"),
        World("AffixShard"), World("QuestItem"),
        World("Chest"),
        World("Stash"), World("Portal"), World("ZoneExit"), World("Shrine"), World("Nemesis"),
        World("UnexploredRoom"), World("RoomEntered", false), World("Discovery", false),
        new("BeaconPing", BeaconSection, "Volume"),
        new("BeaconLocked", BeaconSection, "Volume"),
        new("BeaconCleared", BeaconSection, "Volume"),
        new("BeaconArrived", BeaconSection, "Volume"),
        new("BeaconClick", BeaconSection, "Volume"),
        new("SonarAhead", SoundLevels.SonarSection, SoundLevels.SonarKey),
        new("SonarBehind", SoundLevels.SonarSection, SoundLevels.SonarKey),
        new("SonarSides", SoundLevels.SonarSection, SoundLevels.SonarKey),
    };

    private static SoundGlossaryEntry World(string key, bool range = true) => new(key, SoundLevels.VolumeSection, key,
        range ? SoundLevels.RangeSection : null, range ? key : null);
}
