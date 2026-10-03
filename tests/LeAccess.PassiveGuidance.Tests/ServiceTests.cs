using BepInEx.Configuration;
using LeAccess.Accessibility;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Navigation.Guidance;
using UnityEngine;
using UnityEngine.AI;

internal static partial class Program
{
    private static ConfigFile _config;
    private static void ResetService()
    {
        PassiveGuidanceAccessibility.Cleanup();
        Application.isFocused = true;
        LE.UI.LoadingScreen.IsVisible = false;
        Time.timeScale = 1;
        Time.unscaledTimeAsDouble = 0;
        Camera.main = new();
        NavigationAudioState.IsSuspended = false;
        GameplayInputState.Owned = true;
        EpochInputManager.instance = new();
        ScreenKeyboardManager.Instance = new();
        ScreenReader.Available = true;
        ScreenReader.Speaking = ScreenReader.SpokeThisFrame = false;
        ScreenReader.Spoken.Clear();
        GuidanceTargetReader.Targets.Clear();
        GuidanceTargetReader.Available = true;
        GuidanceTargetReader.Throw = false;
        GuidanceTargetReader.Reads = 0;
        MapSectionWorld.Available = true;
        MapSectionWorld.World = new(1, 1, 0, 0, 0, 0.5);
        MapPlayerFloor.Agent = new();
        MapPlayerFloor.Allowance = 0.5;
        NavMesh.Reset();
        RadarAudioOutput.Reset();
        NaturalCueBank.Missing = false;
        WorldAudioReservation.Reset();
        LeAccess.Plugin.Log.Warnings.Clear();
        LeAccess.Plugin.Log.Info.Clear();
        _config = new();
        PassiveGuidanceAccessibility.Initialize(_config);
    }

    private static void Set<T>(string key, T value) => ((ConfigEntry<T>)_config.Values[key]).Value = value;
    private static void AddTarget(int id = 1, string name = "Objective", float distance = 10, bool quest = true, int priority = 0) =>
        GuidanceTargetReader.Targets.Add(new(id, name, new(0, 0, distance), quest ? 0 : 3, quest) { Priority = priority });
    private static void Tick(double now, bool content = false, bool pending = false)
    { Time.unscaledTimeAsDouble = now; PassiveGuidanceAccessibility.Update(content, pending); }
    private static void Settle(bool pending = false) { Tick(0, pending: pending); Tick(0.5, pending: pending); Tick(1, pending: pending); }

    private static void ServiceTests()
    {
        ResetService(); AddTarget(); Settle();
        Check(ScreenReader.Spoken.Count == 1 && ScreenReader.Spoken[0].Contains("guidance.automatic.target[Objective|"),
            "service automatically names objective without pressing a key");
        Check(RadarAudioOutput.Plays == 1 && NaturalCueBank.Gain == 0.35f, "first named verified route emits quiet natural cue");
        Check(GuidanceTargetReader.LastIncludeExits, "passive reader requests named exits alongside quest candidates");
        for (int frame = 0; frame < 40; frame++) Tick(1.01 + frame * 0.01);
        Check(NavMesh.Calculations == 3 && GuidanceTargetReader.Reads == 3, "route and target work limited to two probes per second");
        Tick(2); Tick(3); Tick(4);
        Check(RadarAudioOutput.Plays > 1 && ScreenReader.Spoken.Count == 1, "stationary sound repeats while narration stays quiet");
        int count = RadarAudioOutput.Plays, silence = RadarAudioOutput.Silences;
        Tick(4.1, content: true);
        Check(RadarAudioOutput.Silences > silence && RadarAudioOutput.Plays == count, "new content immediately silences in-flight orientation");
        Tick(5); Tick(6);
        Check(ScreenReader.Spoken.Count == 1, "menu return does not reannounce target");
        Check(RadarAudioOutput.Plays > count, "menu return resumes newly verified sound");

        ResetService(); AddTarget(); Settle(pending: true);
        Check(ScreenReader.Spoken.Count == 0 && RadarAudioOutput.Plays == 0, "other accessibility narration receives priority before target identity");
        Tick(1.5);
        Check(ScreenReader.Spoken.Count == 1 && RadarAudioOutput.Plays == 1, "deferred current target eventually speaks and sounds");
        ResetService(); AddTarget(); ScreenReader.Speaking = true; Settle();
        Check(ScreenReader.Spoken.Count == 1 && RadarAudioOutput.Plays == 1, "stuck backend busy flag cannot disable default orientation");
        Tick(10);
        Check(ScreenReader.Spoken.Count == 1 && RadarAudioOutput.Plays > 1, "busy flag neither repeats target speech nor starves sound");
        ResetService(); AddTarget(); ScreenReader.SpokeThisFrame = true; Settle();
        Check(ScreenReader.Spoken.Count == 0, "same-frame manual or urgent speech preserved");

        ResetService(); AddTarget(); Set("Sound", false); Settle();
        Check(ScreenReader.Spoken.Count == 1 && RadarAudioOutput.Creations == 0, "speech-only requires no audio device");
        ResetService(); AddTarget(); Set("Speech", false); Settle();
        Check(ScreenReader.Spoken.Count == 0 && RadarAudioOutput.Plays == 1, "sound-only works independently");
        ResetService(); AddTarget(); ScreenReader.Available = false; Settle();
        Check(ScreenReader.Spoken.Count == 0 && RadarAudioOutput.Plays == 1, "missing screen reader does not disable sound");
        ResetService(); AddTarget(); Set("Volume", 0); Settle();
        Check(ScreenReader.Spoken.Count == 1 && RadarAudioOutput.Creations == 0, "zero-volume preserves speech");
        ResetService(); AddTarget(); Settle(); count = RadarAudioOutput.Plays; silence = RadarAudioOutput.Silences;
        Set("Sound", false); Tick(1.01);
        Check(RadarAudioOutput.Silences > silence && RadarAudioOutput.Plays == count, "sound setting mutes immediately between probes");
        Set("Speech", false); Tick(2);
        Check(GuidanceTargetReader.Reads == 3, "both outputs disabled stop native probing");

        ResetService(); AddTarget(); RadarAudioOutput.FailCreation = true; Settle();
        Check(ScreenReader.Spoken.Count == 1 && RadarAudioOutput.Creations == 1, "device failure does not block speech");
        Tick(2); Tick(3); Tick(4); Tick(5);
        Check(RadarAudioOutput.Creations == 1, "failed device retries throttled");
        RadarAudioOutput.FailCreation = false; Tick(6);
        Check(RadarAudioOutput.Creations == 2 && RadarAudioOutput.Plays == 1, "sound recovers after device retry interval");
        ResetService(); AddTarget(); NaturalCueBank.Missing = true; Settle(); Tick(2); Tick(3);
        Check(ScreenReader.Spoken.Count == 1 && RadarAudioOutput.Plays == 0 && LeAccess.Plugin.Log.Warnings.Count == 1,
            "missing sample leaves speech available and reports one diagnostic");
        Check(WorldAudioReservation.TryBegin(WorldAudioSource.Object, 3, 0.5, 50), "missing cue never reserves audio against other services");

        ResetService(); AddTarget(); Tick(0); Tick(0.5);
        WorldAudioReservation.TryBegin(WorldAudioSource.Terrain, 1, 1, 50); Tick(1);
        Check(RadarAudioOutput.Plays == 0, "terrain playback reserves audible output");
        Tick(2.5);
        Check(RadarAudioOutput.Plays == 1, "automatic guidance waits its turn behind world audio");

        ResetService(); AddTarget(); Tick(0); Tick(0.5);
        WorldAudioReservation.TryBegin(WorldAudioSource.Terrain, 1, 1, 50); Tick(1);
        GuidanceTargetReader.Targets.Clear(); AddTarget(2, "Replacement");
        Tick(1.5, pending: true); Tick(2.5, pending: true);
        Check(WorldAudioReservation.TryBegin(WorldAudioSource.Object, 2.5, 0.5, 50),
            "a guidance pulse waiting to name a replacement target cannot retain the head of the shared audio queue");
        Tick(7);
        Check(ScreenReader.Spoken.Count == 2 && ScreenReader.Spoken.Last().Contains("Replacement") && RadarAudioOutput.Plays == 1,
            "replacement guidance reacquires audio after its identity is announced without losing narration state");

        ResetService(); AddTarget(); NavMesh.Status = NavMeshPathStatus.PathPartial; Settle(); Tick(4);
        Check(ScreenReader.Spoken.Count == 0 && RadarAudioOutput.Plays == 0, "partial route never emits a suggestive direction");
        Check(NavMesh.Calculations == 2, "unavailable marker path retries are throttled");
        ResetService(); AddTarget(); NavMesh.Blocked = true; Settle();
        Check(RadarAudioOutput.Plays == 0 && ScreenReader.Spoken.Count == 0, "blocked next segment stays silent");
        ResetService(); AddTarget(); Settle(); count = RadarAudioOutput.Plays;
        GuidanceTargetReader.Targets.Clear(); Tick(1.5); Tick(10);
        Check(RadarAudioOutput.Plays == count && ScreenReader.Spoken.Count == 1, "disappearing target cancels all stale cues and speech");
        AddTarget(2, "Replacement"); Tick(11); Tick(12);
        Check(ScreenReader.Spoken.Count == 2 && ScreenReader.Spoken.Last().Contains("Replacement"), "new target is named before sound");

        ResetService(); AddTarget(distance: 0.5f); Settle();
        Check(ScreenReader.Spoken.Single().StartsWith("guidance.automatic.near_marker") && RadarAudioOutput.Plays == 0,
            "starting at marker gives proximity feedback without a constant arrival cue");
        ResetService(); AddTarget(); Settle();
        MapSectionWorld.World = MapSectionWorld.World with { Session = 2 }; Tick(2); Tick(3);
        Check(ScreenReader.Spoken.Count == 2, "new local player or scene starts a fresh orientation session");
        ExitTests(); GateTests();
        PassiveGuidanceAccessibility.Cleanup();
    }

    private static void ExitTests()
    {
        ResetService(); AddTarget(1, "Nearby exit", 10, quest: false, priority: 3); Settle();
        Check(ScreenReader.Spoken.Single().Contains("Nearby exit") && ScreenReader.Spoken[0].Contains("guidance.exit_unconfirmed"),
            "visible local exit explicitly not claimed as current quest route");
        ResetService(); AddTarget(1, "Distant exit", 100, quest: false, priority: 3); Settle();
        Check(ScreenReader.Spoken.Count == 0 && RadarAudioOutput.Plays == 0, "distant exits outside actual body-width path limit remain silent");
        ResetService(); AddTarget(1, "Nearby exit", 10, quest: false, priority: 3); Set("GuideToNamedExits", false); Settle();
        Check(ScreenReader.Spoken.Count == 0 && !GuidanceTargetReader.LastIncludeExits, "named exits can be disabled");
        ResetService(); AddTarget(1, "Quest", 15); AddTarget(2, "Exit", 4, quest: false, priority: 3); Settle();
        Check(ScreenReader.Spoken.Single().Contains("[Quest|"), "reachable quest wins over nearer exit");
        ResetService(); AddTarget(1, "Blocked quest", 15); AddTarget(2, "Safe exit", 4, quest: false, priority: 3);
        NavMesh.RayBlocked = (_, to) => to.z > 10;
        Tick(0); Tick(0.5); Tick(1); Tick(1.5);
        Check(ScreenReader.Spoken.Single().Contains("Safe exit"), "unreachable quest yields to separately identified verified local exit");
        Check(NavMesh.Calculations == 4, "multiple candidates still use at most one path per probe");
        ResetService();
        for (int id = 1; id <= 6; id++) AddTarget(id, $"Blocked {id}", 10 + id);
        AddTarget(7, "Reachable exit", 4, quest: false, priority: 3);
        NavMesh.RayBlocked = (_, to) => to.z > 10;
        for (double now = 0; now <= 4; now += 0.5) Tick(now);
        Check(ScreenReader.Spoken.Single().Contains("Reachable exit"), "six failed preferred targets never starve untested reachable exit");
        Check(NavMesh.Calculations == 9, "failed candidate fairness still limits native work to one path per probe");
        int spoken = ScreenReader.Spoken.Count, plays = RadarAudioOutput.Plays;
        for (double now = 4.5; now <= 12.5; now += 0.5) Tick(now);
        Check(ScreenReader.Spoken.Count == spoken && RadarAudioOutput.Plays > plays, "known failures do not repeatedly suspend selected safe route");
        Tick(13);
        plays = RadarAudioOutput.Plays;
        Tick(13.5);
        Check(ScreenReader.Spoken.Count == spoken, "unsuccessful occasional alternate probe never renames retained target");
        Check(RadarAudioOutput.Plays == plays + 1, "failed alternate resumes fresh verified retained route next tick without settle delay");
        NavMesh.RayBlocked = (_, _) => false;
        for (double now = 14; now <= 24; now += 0.5) Tick(now);
        Check(ScreenReader.Spoken.Count == spoken + 1 && ScreenReader.Spoken.Last().Contains("Blocked 2"),
            "formerly unavailable preferred quest replaces retained exit when route becomes valid while stationary");
    }

    private static void GateTests()
    {
        Action[] gates =
        {
            () => Application.isFocused = false,
            () => LE.UI.LoadingScreen.IsVisible = true,
            () => Time.timeScale = 0,
            () => NavigationAudioState.IsSuspended = true,
            () => GameplayInputState.Owned = false,
            () => EpochInputManager.instance.isInputFieldActive = true,
            () => ScreenKeyboardManager.Instance._enabled = true,
            () => MapSectionWorld.Available = false,
            () => Camera.main.isActiveAndEnabled = false,
            () => Set("Enabled", false)
        };
        foreach (var gate in gates)
        {
            ResetService(); AddTarget(); Settle();
            int plays = RadarAudioOutput.Plays, speech = ScreenReader.Spoken.Count, silence = RadarAudioOutput.Silences;
            gate(); Tick(2); Tick(3);
            Check(RadarAudioOutput.Plays == plays && ScreenReader.Spoken.Count == speech, "suspended/dead/typing/menu service cannot output");
            Check(RadarAudioOutput.Silences > silence, "suspension cancels existing playback");
        }
        ResetService(); AddTarget(); GuidanceTargetReader.Throw = true; Settle();
        Check(RadarAudioOutput.Plays == 0 && ScreenReader.Spoken.Count == 0 && LeAccess.Plugin.Log.Warnings.Count == 1,
            "native target failures stay silent with bounded diagnostics");
    }
}
