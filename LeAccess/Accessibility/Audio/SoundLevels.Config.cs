using BepInEx.Configuration;
using LeAccess.Accessibility.Combat;

namespace LeAccess.Accessibility.Audio;

// Config binding for SoundLevels: every entry pushes its value into the static levels as it changes.
internal static partial class SoundLevels
{
    /// <summary>The plugin's config, for reading the group volumes of other modules.</summary>
    internal static ConfigFile Config { get; private set; }

    internal static void Initialize(ConfigFile config)
    {
        Config = config;
        Reset();
        Bind(config, AudioSection, MasterKey, 100, MaxMasterPercent,
            "Percent that scales every mod sound (radar, world objects, enemies, beacon, sonar); speech is not affected.",
            SetMaster);
        Bind(config, SonarSection, SonarKey, 100, MaxMasterPercent,
            "Percent volume of the wall tones.", SetSonar);
        foreach (var world in WorldKinds)
        {
            var kind = world.Kind;
            Bind(config, VolumeSection, world.Key, 100, MaxVolumePercent,
                $"Percent volume of the {world.Key} sound on top of the group volume; 0 silences it, above 100 boosts it.",
                percent => SetVolume(kind, percent));
            if (world.HasRange)
                Bind(config, RangeSection, world.Key, 0, MaxRangeMetres,
                    $"Metres from which the {world.Key} sound is heard; 0 follows World object sounds LoopRange.",
                    metres => SetRange(kind, metres));
        }
        for (int index = 0; index < EnemyKeys.Length; index++)
        {
            var kind = (EnemyCueKind)index;
            Bind(config, EnemySection, EnemyKeys[index], 100, MaxVolumePercent,
                $"Percent of the enemy cue volume for {kind.ToString().ToLowerInvariant()} enemies; 0 silences them, above 100 boosts them.",
                percent => SetEnemy(kind, percent));
        }
    }

    private static void Bind(ConfigFile config, string section, string key, int defaultValue, int max,
        string description, System.Action<int> apply)
    {
        var entry = config.Bind(section, key, defaultValue,
            new ConfigDescription(description, new AcceptableValueRange<int>(0, max)));
        apply(entry.Value);
        entry.SettingChanged += (_, _) => apply(entry.Value);
    }
}
