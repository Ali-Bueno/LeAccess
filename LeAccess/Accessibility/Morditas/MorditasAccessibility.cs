using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Exploration;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Navigation.Guidance;
using LeAccess.Accessibility.Players;
using LeAccess.Accessibility.Quests;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using NVector3 = System.Numerics.Vector3;

namespace LeAccess.Accessibility.Morditas;

// The one speaker for the Rage of Morditas encounter (hunt, gauntlet, reward, outcome); it also feeds the scanner's
// blood trails and plays their drip. See reference/rage-of-morditas.md.
internal static class MorditasAccessibility
{
    // The dynamic objective and vitals readers' 100 ms sample.
    private const double PollSeconds = 0.1;
    private static readonly MorditasEventState State = new();
    private static MorditasData _data;
    private static MorditasRules _rules;
    private static MorditasGameTexts _texts;
    private static MorditasPhase? _loggedPhase;
    private static double _nextPoll;
    private static int _scene, _heardRefresh, _dataEncounter;
    // One step is the body width, as in the scanner; kept from the last living reading.
    private static float _step = 1;
    private static string _lastFailure;

    internal static void Initialize(ConfigFile config) => BloodTrailCue.Initialize(config);

    internal static void Update(bool otherContentActive)
    {
        try
        {
            if (LE.UI.LoadingScreen.IsVisible) { End(); return; }
            var scene = SceneManager.GetActiveScene();
            if (scene.handle != _scene) { End(); _scene = scene.handle; }
            if (!Application.isFocused) return;
            double now = Time.unscaledTimeAsDouble;
            Drip(now);
            if (now < _nextPoll) return;
            _nextPoll = now + PollSeconds;
            if (!MorditasProbe.TryRead(out var reading)) { Stop(); return; }
            if (!MorditasSpeechOwner.Active) MorditasProbe.Claim();
            // A dead player has no world reading, but the failure must still be told.
            bool alive = MapSectionWorld.TryCapturePlayer(out var world);
            if (alive && world.BodyRadius > 0) _step = (float)world.BodyRadius * 2;
            if (reading.Encounter != _dataEncounter || !Complete(_data)) Begin(reading);
            if (reading.Phase != MorditasPhase.Hunt) MorditasProbe.Trails.Clear();
            var sample = Sample(reading, alive ? world : null);
            if (_loggedPhase != reading.Phase)
            {
                _loggedPhase = reading.Phase;
                Plugin.Log.LogInfo(FormattableString.Invariant(
                    $"[Morditas] state {reading.Phase} success={reading.Success} kills={reading.HuntKills} time_left={reading.TimeLeft:F1} gauntlet_kills={reading.GauntletKills}"));
            }
            var update = State.Observe(sample, _rules, _texts, ModText.Format, now);
            MorditasSpeechOwner.Status = MorditasEventState.Status(sample, _rules, _texts, ModText.Format);
            // Menus, typing, dialogue and pauses hold it back; the latest state speaks once they close. The outcome
            // also speaks over the death screen, since the encounter may be gone after the respawn.
            bool access = QuestAnnouncementEligibility.HasGameplayAccess(otherContentActive, IntPtr.Zero) ||
                reading.Phase == MorditasPhase.Finished && !Typing();
            if (!update.HasContent || !access || !ScreenReader.TrySpeakOverPickup(update.Text, update.Interrupt)) return;
            State.Acknowledge();
            Plugin.Log.LogInfo($"[Morditas] {update.Text}");
        }
        catch (Exception ex)
        {
            Stop();
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"[Morditas] feedback unavailable: {ex}");
        }
    }

    /// <summary>The scanner's blood trail entries while the hunt runs.</summary>
    internal static int AddTrailEntries(List<ExplorationEntry> entries) => MorditasProbe.Trails.AddEntries(entries,
        number => ModText.Format("exploration.blood_trail", number), _data.TrailNear);

    // Read once per encounter; retried while the game's data or the encounter config is not loaded yet.
    private static void Begin(in MorditasReading reading)
    {
        bool fresh = reading.Encounter != _dataEncounter, wasComplete = Complete(_data);
        _dataEncounter = reading.Encounter;
        _data = MorditasProbe.ReadData(reading.Sync);
        _rules = new MorditasRules(_data.Thresholds, _data.WarningSeconds, _data.CriticalSeconds, _data.OutOfZoneSeconds,
            ExplorationCatalog.Steps(_data.Radius, _step));
        _texts = MorditasProbe.Texts();
        if (fresh || Complete(_data) && !wasComplete) Plugin.Log.LogInfo(FormattableString.Invariant(
            $"[Morditas] encounter {reading.Encounter}: thresholds=[{string.Join(",", _data.Thresholds)}] warning={_data.WarningSeconds} critical={_data.CriticalSeconds} out_of_zone={_data.OutOfZoneSeconds} radius={_data.Radius} ({_rules.RadiusSteps} steps) trails={_data.TrailNear}-{_data.TrailFar} m every {_data.TrailInterval} s"));
    }

    private static bool Complete(in MorditasData data) => data.Thresholds is { Length: > 0 } && data.Radius > 0;

    // The circle's centre from the player, for the leaving warning and the status.
    private static MorditasSample Sample(in MorditasReading reading, MapSectionWorldSnapshot? player)
    {
        int steps = 0;
        string direction = null;
        if (reading.Phase == MorditasPhase.Gauntlet && player is { } world &&
            ExplorationFeedback.TryAxes(out var forward, out var right))
        {
            var delta = reading.Centre - new NVector3((float)world.X, (float)world.Y, (float)world.Z);
            steps = ExplorationCatalog.Steps(new System.Numerics.Vector2(delta.X, delta.Z).Length(), _step);
            direction = GuidanceGeometry.Direction(delta, forward, right);
        }
        return new(reading.Encounter, reading.Phase, reading.Success, reading.HuntKills, reading.TimeLeft,
            reading.GauntletKills, reading.Inside, reading.AllOut, steps, direction);
    }

    // One drip per game refresh, from the nearest trail as the player stands when it plays.
    private static void Drip(double now)
    {
        var trails = MorditasProbe.Trails;
        if (trails.Refreshes != _heardRefresh)
        {
            _heardRefresh = trails.Refreshes;
            if (trails.Targets.Count == 0) return;
            BloodTrailCue.Queue(now + _data.TrailInterval);
            LogTrails(trails);
        }
        if (!BloodTrailCue.Pending) return;
        if (trails.Targets.Count == 0 || NavigationAudioState.IsSuspended || !GameplayInputState.OwnsInput() ||
            ScreenContentAnnouncer.HasActiveContent || DialogueAnnouncer.HasActiveContent)
        { BloodTrailCue.Drop(); return; }
        var player = LocalPlayerSource.Player;
        if (!IsAlive(player) || !ExplorationFeedback.TryAxes(out var forward, out var right) ||
            !trails.TryNearest(GuidanceRouteProbe.Convert(player.transform.position), forward, right,
                out float pan, out float ahead, out float distance)) return;
        BloodTrailCue.Play(now, pan, ahead, BloodTrails.Gain(distance, _data.TrailNear, _data.TrailFar));
    }

    private static bool Typing()
    {
        var input = EpochInputManager.instance;
        var keyboard = ScreenKeyboardManager.Instance;
        return IsAlive(input) && input.isInputFieldActive || IsAlive(keyboard) && keyboard._enabled;
    }

    private static void LogTrails(BloodTrails trails)
    {
        var player = LocalPlayerSource.Player;
        string nearest = IsAlive(player) && trails.TryNearest(GuidanceRouteProbe.Convert(player.transform.position),
            NVector3.UnitZ, NVector3.UnitX, out _, out _, out float distance)
            ? FormattableString.Invariant($"{distance:F1} m") : "unknown";
        Plugin.Log.LogInfo($"[Morditas] blood trails {trails.Targets.Count}, nearest {nearest}");
    }

    // The encounter ended or left the player's scene; the scene keeps the speaker claim for late pop-ups.
    private static void Stop()
    {
        if (State.Encounter != 0) Plugin.Log.LogInfo("[Morditas] encounter no longer followed");
        State.Reset();
        MorditasProbe.Trails.Clear();
        BloodTrailCue.Drop();
        MorditasSpeechOwner.Status = null;
        _data = default;
        _rules = default;
        _texts = default;
        _loggedPhase = null;
        _dataEncounter = 0;
    }

    // A new scene or a loading screen; the tracked encounter is kept, as a client's gauntlet can report in while loading.
    private static void End()
    {
        Stop();
        MorditasSpeechOwner.End();
    }

    internal static void Cleanup()
    {
        End();
        MorditasProbe.Reset();
        BloodTrailCue.Cleanup();
        _scene = _heardRefresh = 0;
        _nextPoll = 0;
        _step = 1;
        _lastFailure = null;
    }
}
