using System;
using BepInEx.Configuration;
using LeAccess.Accessibility.Combat;
using LeAccess.Accessibility.Exploration;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.WorldCues;

namespace LeAccess.Accessibility.Audio;

// Plays one glossary sound centred on its own output, anywhere: master is applied by the output, group and own levels here.
internal static class SoundPreview
{
    private const double LoopSeconds = 2;
    private const int BufferMilliseconds = 100;
    private const string WorldGroupSection = "World object sounds";
    private static RadarAudioOutput _output;

    internal static void Play(string id)
    {
        try
        {
            var samples = Build(id);
            if (samples.Length == 0) { Stop(); return; }
            if (_output == null) _output = new RadarAudioOutput("Mod menu preview", BufferMilliseconds);
            else if (!_output.IsRunning && !_output.TryRecover(out _)) return;
            _output.Silence();
            _output.Play(samples);
        }
        catch (Exception ex) { Plugin.Log.LogWarning($"Sound preview unavailable: {ex.Message}"); }
    }

    internal static void Stop() => _output?.Silence();

    internal static void Cleanup()
    {
        _output?.Dispose();
        _output = null;
    }

    private static float[] Build(string id)
    {
        foreach (var world in SoundLevels.WorldKinds)
            if (world.Key == id) return World(world.Kind);
        return id switch
        {
            "RadarOpen" => Radar(RadarState.Open, false),
            "RadarBlocked" => Radar(RadarState.Blocked, false),
            "RadarUnexplored" => Radar(RadarState.Open, true),
            "EnemyBoss" => Enemy(EnemyCueKind.Boss),
            "EnemyElite" => Enemy(EnemyCueKind.Elite),
            "EnemyOrdinary" => Enemy(EnemyCueKind.Ordinary),
            "NpcTap" => NpcCueSynthesizer.Synthesize(0, 1, Group("NPC sounds", "Volume")),
            "BeaconPing" => Beacon(BeaconSound.Ping),
            "BeaconLocked" => Beacon(BeaconSound.Locked),
            "BeaconCleared" => Beacon(BeaconSound.Cleared),
            "BeaconArrived" => Beacon(BeaconSound.Arrived),
            "BeaconClick" => BeaconCueSynthesizer.Click(Group("Exploration beacon", "Volume")),
            "SonarAhead" => Sonar(WallSonar.FreqForward),
            "SonarBehind" => Sonar(WallSonar.FreqBack),
            "SonarSides" => Sonar(WallSonar.FreqSides),
            _ => Array.Empty<float>()
        };
    }

    private static float[] World(NaturalCueKind kind)
    {
        float level = Math.Min(1, Group(WorldGroupSection, "Volume")) * SoundLevels.Volume(kind);
        if (!(level > 0)) return Array.Empty<float>();
        float[] loop = WorldLoopBank.Get(kind);
        if (loop.Length > 0)
            return PreviewClips.Loop(loop, Math.Min(WorldLoopSelection.NearGain * level, WorldLoopSelection.MixGainLimit), LoopSeconds);
        return NaturalCueBank.TryBuild(kind, 0, 1, level, out var samples) ? samples : Array.Empty<float>();
    }

    private static float[] Radar(RadarState state, bool unexplored) =>
        RadarCueSynthesizer.Synthesize(state, RadarDirection.Up, Group("Reactive radar", "Volume"), unexplored);

    private static float[] Enemy(EnemyCueKind kind) =>
        EnemyCueSynthesizer.Synthesize(kind, 0, 1, Group(SoundLevels.EnemySection, "Volume") * SoundLevels.Enemy(kind));

    private static float[] Beacon(BeaconSound sound) =>
        BeaconCueSynthesizer.Synthesize(sound, 0, 1, Group("Exploration beacon", "Volume"));

    private static float[] Sonar(double frequency) =>
        PreviewClips.Tone(frequency, WallSonar.BaseVolume * SoundLevels.Sonar, LoopSeconds);

    // A group's Volume entry in percent as a factor; 1 when the module has not bound it.
    private static float Group(string section, string key) =>
        SoundLevels.Config != null && SoundLevels.Config.TryGetEntry<int>(new ConfigDefinition(section, key), out var entry)
            ? entry.Value / 100f : 1;
}
