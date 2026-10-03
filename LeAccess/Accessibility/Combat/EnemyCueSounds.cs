using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;

namespace LeAccess.Accessibility.Combat;

// Directional growls for nearby enemies in sight, ported from D2R; see reference/enemy-cues.md.
internal static class EnemyCueSounds
{
    private const string ConfigSection = "Enemy cues";
    private const string ActiveStatus = "active";
    private const double AudioRetrySeconds = 5; // Match the other world outputs' recovery interval.
    private const int AudioBufferMilliseconds = 50; // Match the NPC output, the other D2R entity-cue stream.
    private const double DiagnosticIntervalSeconds = 5;
    private static readonly EnemyCueProbe World = new();
    private static readonly EnemyCueTracker Tracker = new();
    private static ConfigEntry<bool> _enabled, _bosses, _ordinary;
    private static ConfigEntry<int> _volume;
    private static RadarAudioOutput _audio;
    private static double _nextPoll, _nextAudioRetry, _nextDiagnostic;
    private static long _session, _played;
    private static bool _ready;
    private static string _status, _lastFailure, _lastError, _lastAudioFailure;

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind(ConfigSection, "Enabled", true,
            "Play short directional growls for the nearest enemies in sight, faster when closer.");
        _bosses = config.Bind(ConfigSection, "Bosses", true,
            "Give the nearest boss or miniboss its own deep swoop; when off, bosses growl like other enemies.");
        _ordinary = config.Bind(ConfigSection, "Ordinary", true,
            "Growl for the nearest ordinary enemies in the streams a boss leaves free.");
        _volume = config.Bind(ConfigSection, "Volume", 100,
            new ConfigDescription("Percent of the D2R enemy cue level.", new AcceptableValueRange<int>(0, 100)));
        Plugin.Log.LogInfo("Enemy cues configured (camera-relative, up to three enemies in sight)");
    }

    internal static void Update()
    {
        if (_enabled == null) return;
        try
        {
            double now = Time.unscaledTimeAsDouble;
            string suspension = !_enabled.Value ? "disabled" : _volume.Value <= 0 ? "volume zero"
                : !_bosses.Value && !_ordinary.Value ? "no enemy classes enabled"
                : NavigationAudioState.SuspensionReason ?? (!GameplayInputState.OwnsInput() ? "menu input"
                : ScreenContentAnnouncer.HasActiveContent ? "screen content"
                : DialogueAnnouncer.HasActiveContent ? "dialogue" : null);
            if (suspension != null) { Suspend(suspension, now); return; }
            if (now >= _nextPoll)
            {
                _nextPoll = now + EntityCueCadence.PollSeconds;
                if (!World.TryCapture(_bosses.Value, _ordinary.Value)) { Pause(World.UnavailableReason, now); return; }
                if (World.LastFailure != null) Report(ref _lastFailure, World.LastFailure);
                else _lastFailure = null;
                if (_session != World.Session)
                {
                    Halt();
                    _session = World.Session;
                }
                _ready = true;
                ReportStatus(ActiveStatus, now);
            }
            if (!_ready) return;
            // Starts are checked every frame against the latest snapshot so a shared-channel turn is not lost.
            if (!Tracker.TryChoose(World.Selected, now, out var cue))
            { WorldAudioReservation.Cancel(WorldAudioSource.Enemy); return; }
            if (!EnsureAudio(now) || _audio.IsPlaying) return;
            if (!WorldAudioReservation.TryBegin(WorldAudioSource.Enemy, now,
                EnemyCueSynthesizer.DurationSeconds, AudioBufferMilliseconds)) return;
            Tracker.MarkPlayed(cue, now);
            _audio.Play(EnemyCueSynthesizer.Synthesize(cue.Kind, cue.Pan, cue.Forward, _volume.Value / 100f));
            _played++;
            _lastError = null;
        }
        catch (Exception ex)
        {
            Halt();
            _session = 0;
            _ready = false;
            Report(ref _lastError, ex.Message);
        }
    }

    private static bool EnsureAudio(double now)
    {
        if (_audio?.IsRunning == true) return true;
        WorldAudioReservation.Cancel(WorldAudioSource.Enemy);
        if (now < _nextAudioRetry) return false;
        _nextAudioRetry = now + AudioRetrySeconds;
        try
        {
            if (_audio == null)
            {
                _audio = new RadarAudioOutput("Enemy cues", AudioBufferMilliseconds);
                _lastAudioFailure = null;
                return true;
            }
            bool recovered = _audio.TryRecover(out string status);
            if (status != null && status != _lastAudioFailure) Plugin.Log.LogInfo(status);
            _lastAudioFailure = recovered ? null : status;
            return recovered;
        }
        catch (Exception ex)
        {
            if (_lastAudioFailure != ex.Message) Plugin.Log.LogWarning($"Enemy cue audio initialization failed: {ex.Message}");
            _lastAudioFailure = ex.Message;
            return false;
        }
    }

    // Silences output and clears cadence and pending turns; the next snapshot starts fresh.
    private static void Halt()
    {
        WorldAudioReservation.Cancel(WorldAudioSource.Enemy);
        if (_session != 0) _audio?.Silence();
        Tracker.Reset();
    }

    private static void Suspend(string reason, double now)
    {
        Halt();
        _session = 0;
        _ready = false;
        World.Reset();
        ReportStatus(reason, now);
    }

    // A failed capture silences without resetting cadence, so a brief gap in the same session does not burst.
    private static void Pause(string reason, double now)
    {
        WorldAudioReservation.Cancel(WorldAudioSource.Enemy);
        if (_ready) _audio?.Silence();
        _ready = false;
        ReportStatus(reason, now);
    }

    private static void Report(ref string last, string message)
    {
        if (last == message) return;
        last = message;
        Plugin.Log.LogWarning($"Enemy cues unavailable: {message}");
    }

    // Suspension reasons are logged when they change; the active state also reports a 5 s heartbeat.
    private static void ReportStatus(string status, double now)
    {
        if (status == _status && (status != ActiveStatus || now < _nextDiagnostic)) return;
        _status = status;
        _nextDiagnostic = now + DiagnosticIntervalSeconds;
        Plugin.Log.LogInfo($"[Enemy cues] {status}; in_range={World.InRange} selected={Describe(World.Selected)} " +
            $"played={_played} audio={(_audio == null ? "not initialized" : _audio.Diagnostics.ToString())}");
    }

    private static string Describe(IReadOnlyList<EnemyCue> cues) => cues.Count == 0 ? "none"
        : string.Join(",", cues.Select(cue => FormattableString.Invariant(
            $"{cue.Kind}#{cue.Id}@{cue.Distance:F1}/slot{cue.Slot}")));

    internal static void Cleanup()
    {
        Halt();
        _session = 0;
        _ready = false;
        World.Reset();
        _audio?.Dispose();
        _audio = null;
        _enabled = _bosses = _ordinary = null;
        _volume = null;
        _nextPoll = _nextAudioRetry = _nextDiagnostic = 0;
        _played = 0;
        _status = _lastFailure = _lastError = _lastAudioFailure = null;
    }
}
