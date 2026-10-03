using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using LeAccess.Accessibility.MapSections;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Navigation.Guidance;

internal static class PassiveGuidanceAccessibility
{
    private const string ConfigSection = "Automatic orientation";
    // One candidate path every half second; unavailable candidates yield to other visible markers.
    internal const double PollSeconds = 0.5;
    private const double UnavailableRetrySeconds = 3;
    private const double AlternateRetrySeconds = 10;
    private const float DefaultLocalExitBodyWidths = 32;
    private readonly record struct UnavailableTarget(double RetryAt, int Priority, string Name);
    private static readonly List<GuidanceTarget> Targets = new();
    private static readonly Dictionary<int, UnavailableTarget> Unavailable = new();
    private static readonly List<int> Expired = new();
    private static readonly PassiveGuidanceState State = new();
    private static readonly PassiveGuidanceAudio Audio = new();
    private static ConfigEntry<bool> _enabled, _sound, _speech, _exits;
    private static ConfigEntry<int> _volume;
    private static ConfigEntry<float> _exitRange;
    private static long _session;
    private static int? _selected;
    private static double _nextProbe, _nextAlternateProbe;
    private static string _lastFailure;

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind(ConfigSection, "Enabled", true,
            "Automatically describe and sound a verified local route without pressing a key or moving the character.");
        _sound = config.Bind(ConfigSection, "Sound", true,
            "Repeat a directional natural sound toward the next verified path corner; faster near the destination.");
        _speech = config.Bind(ConfigSection, "Speech", true,
            "Name the destination, stable major direction changes and proximity to its map marker.");
        _volume = config.Bind(ConfigSection, "Volume", 35,
            new ConfigDescription("Automatic orientation sound volume, percent.", new AcceptableValueRange<int>(0, 100)));
        _exits = config.Bind(ConfigSection, "GuideToNamedExits", true,
            "Also orient toward a nearby visible named exit when no reachable tracked quest marker is available. This does not identify the correct quest exit.");
        _exitRange = config.Bind(ConfigSection, "LocalExitRangeInBodyWidths", DefaultLocalExitBodyWidths,
            new ConfigDescription("Maximum verified path distance to an automatically described exit, measured in actual navigation body widths.",
                new AcceptableValueRange<float>(4, 100)));
        Reset();
    }

    internal static void Update(bool otherContentActive = false, bool otherSpeechPending = false)
    {
        if (_enabled?.Value != true || !_speech.Value && (!_sound.Value || _volume.Value == 0))
        { Reset(); return; }
        try
        {
            if (!_sound.Value || _volume.Value == 0) Audio.Silence();
            var input = EpochInputManager.instance;
            var keyboard = ScreenKeyboardManager.Instance;
            if (otherContentActive || NavigationAudioState.IsSuspended || !GameplayInputState.OwnsInput() ||
                IsAlive(input) && input.isInputFieldActive || IsAlive(keyboard) && keyboard._enabled)
            { Suspend(); return; }
            double now = Time.unscaledTimeAsDouble;
            if (now < _nextProbe) return;
            _nextProbe = now + PollSeconds;
            if (!MapSectionWorld.TryCapturePlayer(out var world)) { Suspend(); return; }
            if (_session != world.Session) { Reset(); _session = world.Session; _nextProbe = now + PollSeconds; }
            var camera = Camera.main;
            if (!IsAlive(camera) || !camera.isActiveAndEnabled) { Suspend(); return; }
            Targets.Clear();
            if (!GuidanceTargetReader.TryRead(world, Targets, includeExits: _exits.Value))
            {
                Suspend();
                ReportFailure(GuidanceTargetReader.LastFailure);
                return;
            }
            var position = new Vector3((float)world.X, (float)world.Y, (float)world.Z);
            if (!TrySelect(position, now, out var target)) { Suspend(); return; }
            var route = GuidanceRouteProbe.Read(world, target.Position, verifyNext: true);
            if (!route.Complete || !target.IsQuest && route.Distance > route.BodyWidth * _exitRange.Value)
            {
                Unavailable[target.Id] = new(now + UnavailableRetrySeconds, target.Priority, target.Name);
                // A failed alternate probe cannot invalidate the retained target. This tick emits no cached direction.
                if (_selected.HasValue && _selected != target.Id && Targets.Exists(candidate => candidate.Id == _selected))
                { Audio.Silence(); return; }
                if (_selected == target.Id) _selected = null;
                Suspend();
                return;
            }
            Unavailable.Remove(target.Id);
            if (_selected != target.Id) _nextAlternateProbe = now + AlternateRetrySeconds;
            _selected = target.Id;
            var value = new PassiveGuidanceObservation(world.Session, target.Id, target.Name, target.IsQuest,
                GuidanceRouteProbe.Convert(position), GuidanceRouteProbe.Convert(target.Position),
                GuidanceRouteProbe.Convert(route.Next), GuidanceRouteProbe.Convert(camera.transform.forward),
                GuidanceRouteProbe.Convert(camera.transform.right), route.Distance, route.BodyWidth, route.Complete);
            if (!State.Observe(value, now)) { Audio.Silence(); return; }
            if (State.NearMarker) Audio.Silence();
            bool readerAvailable = ScreenReader.IsAvailable();
            if (_speech.Value && readerAvailable && !otherSpeechPending && !ScreenReader.SpokeThisFrame &&
                State.TryTakeSpeech(now, out var announcement))
                Speak(announcement);
            if (_sound.Value && _volume.Value > 0 && State.PulseDue(now) &&
                (!_speech.Value || !readerAvailable || State.HasAnnouncedTarget) &&
                Audio.TryPlay(now, State.Pan, State.Forward, _volume.Value / 100f)) State.PulsePlayed(now);
        }
        catch (Exception ex) { Suspend(); ReportFailure(ex.Message); }
    }

    private static bool TrySelect(Vector3 position, double now, out GuidanceTarget target)
    {
        Expired.Clear();
        foreach (var entry in Unavailable)
            if (!Targets.Exists(candidate => candidate.Id == entry.Key && candidate.Priority == entry.Value.Priority &&
                candidate.Name == entry.Value.Name)) Expired.Add(entry.Key);
        foreach (int id in Expired) Unavailable.Remove(id);
        target = default;
        bool found = false;
        float closest = float.PositiveInfinity;
        foreach (var candidate in Targets)
        {
            float distance = (candidate.Position - position).sqrMagnitude;
            if ((!candidate.IsQuest && !_exits.Value) || Unavailable.ContainsKey(candidate.Id) ||
                !float.IsFinite(distance) || string.IsNullOrWhiteSpace(candidate.Name)) continue;
            if (found && (candidate.Priority > target.Priority || candidate.Priority == target.Priority &&
                (target.Id == _selected || candidate.Id != _selected &&
                    (distance > closest || distance == closest && candidate.Id >= target.Id)))) continue;
            target = candidate;
            closest = distance;
            found = true;
        }
        if (found && (target.Id != _selected || now < _nextAlternateProbe)) return true;
        var retry = default(GuidanceTarget);
        double earliest = double.PositiveInfinity;
        bool retryFound = false;
        foreach (var candidate in Targets)
        {
            if ((!candidate.IsQuest && !_exits.Value) || !Unavailable.TryGetValue(candidate.Id, out var failed) ||
                now < failed.RetryAt || found && candidate.Priority >= target.Priority) continue;
            if (retryFound && (failed.RetryAt > earliest || failed.RetryAt == earliest && candidate.Id >= retry.Id)) continue;
            retry = candidate;
            earliest = failed.RetryAt;
            retryFound = true;
        }
        if (!retryFound) return found;
        if (found) _nextAlternateProbe = now + AlternateRetrySeconds;
        target = retry;
        return true;
    }

    private static void Speak(PassiveGuidanceSpeech value)
    {
        string text = value.Kind switch
        {
            PassiveGuidanceSpeechKind.NearMarker => ModText.Format("guidance.automatic.near_marker", value.Name),
            PassiveGuidanceSpeechKind.Turn => ModText.Format("guidance.automatic.turn", ModText.Get(value.Direction)),
            _ => ModText.Format("guidance.automatic.target", value.Name, ModText.Get(value.Direction), Math.Ceiling(value.Distance))
        };
        if (!value.IsQuest && value.Kind != PassiveGuidanceSpeechKind.Turn)
            text += ". " + ModText.Get("guidance.exit_unconfirmed");
        ScreenReader.Speak(text, interrupt: false);
        Plugin.Log.LogInfo($"[Automatic orientation] {text}");
    }

    private static void ReportFailure(string message)
    {
        if (message == null || message == _lastFailure) return;
        _lastFailure = message;
        Plugin.Log.LogWarning($"Automatic orientation unavailable: {message}");
    }

    private static void Suspend() { State.Suspend(); Audio.Silence(); }

    private static void Reset()
    {
        Suspend();
        State.Reset();
        Targets.Clear();
        Unavailable.Clear();
        Expired.Clear();
        _selected = null;
        _session = 0;
        _nextProbe = _nextAlternateProbe = 0;
    }

    internal static void Cleanup()
    {
        Reset();
        Audio.Dispose();
        _enabled = _sound = _speech = _exits = null;
        _volume = null;
        _exitRange = null;
        _lastFailure = null;
    }
}
