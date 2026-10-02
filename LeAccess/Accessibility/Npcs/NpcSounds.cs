using System;
using BepInEx.Configuration;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Items;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;

namespace LeAccess.Accessibility.Npcs;

internal static class NpcSounds
{
    private const string ConfigSection = "NPC sounds";
    private const double AudioRetrySeconds = 5; // Match the existing navigation output recovery interval.
    private const int AudioBufferMilliseconds = 50;
    private static readonly NpcWorldProbe World = new();
    private static readonly NpcCueTiming Timing = new();
    private static readonly NpcNameAnnouncementState Names = new();
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<bool> _announceNames;
    private static ConfigEntry<int> _volume;
    private static ConfigEntry<float> _rangeMultiplier;
    private static RadarAudioOutput _audio;
    private static double _nextPoll;
    private static double _nextAudioRetry;
    private static long _session;
    private static int _targetId;
    private static bool _active;
    private static string _lastFailure;
    private static string _lastAudioFailure;

    internal static bool HasPendingName => Names.HasPendingContent;

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind(ConfigSection, "Enabled", true,
            "Play directional wooden taps for the nearest available NPC while exploring.");
        _announceNames = config.Bind(ConfigSection, "AnnounceNames", true,
            "Announce the nearest available NPC's localized name on approach or target change, independently of cue volume.");
        _volume = config.Bind(ConfigSection, "Volume", 100,
            new ConfigDescription("Percent of the D2R NPC cue level.", new AcceptableValueRange<int>(0, 100)));
        _rangeMultiplier = config.Bind(ConfigSection, "InteractionRangeMultiplier", 1f,
            "Detection distance relative to each NPC's own interaction range. Must be positive; 1 uses the game's range.");
        Plugin.Log.LogInfo("NPC sounds configured (camera-relative, nearest available NPC)");
    }

    internal static void Update(bool otherSpeechPending = false)
    {
        if (_enabled == null) return;
        try
        {
            bool playSounds = _enabled.Value && _volume.Value > 0;
            if (!playSounds && !_announceNames.Value || NavigationAudioState.IsSuspended ||
                !GameplayInputState.OwnsInput() || ScreenContentAnnouncer.HasActiveContent ||
                DialogueAnnouncer.HasActiveContent)
            {
                Suspend();
                return;
            }
            if (!playSounds)
            {
                WorldAudioReservation.Cancel(WorldAudioSource.Npc);
                if (_active) _audio?.Silence();
                _active = false;
                Timing.Reset();
            }
            if (!_announceNames.Value) Names.Reset();

            double now = Time.unscaledTimeAsDouble;
            if (now < _nextPoll) return;
            _nextPoll = now + NpcCueTiming.PollSeconds;
            float multiplier = _rangeMultiplier.Value;
            if (!float.IsFinite(multiplier) || multiplier <= 0) multiplier = 1f;
            if (!World.TryCapture(multiplier, out var target))
            {
                if (World.LastFailure != null) ReportFailure(World.LastFailure);
                Suspend();
                return;
            }

            if (_session != target.Session)
            {
                WorldAudioReservation.Cancel(WorldAudioSource.Npc);
                Timing.Reset();
                _audio?.Silence();
                _session = target.Session;
                _targetId = 0;
            }
            if (_targetId != target.InstanceId)
            {
                _targetId = target.InstanceId;
                Plugin.Log.LogInfo($"[NPC sound] target={target.InstanceId} name={target.Name} source={target.NameSource ?? "unavailable"} " +
                    $"distance={target.Distance:F2} range={target.Range:F2}");
            }
            if (_announceNames.Value && Names.Observe(target.Session, target.InstanceId, target.Name,
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame && !otherSpeechPending &&
                !InventoryAccessibility.HasActiveContent && !GroundLootAccessibility.HasPendingContent,
                out string announcement))
            {
                ScreenReader.Speak(announcement, interrupt: false);
                Plugin.Log.LogInfo($"[NPC name] target={target.InstanceId} source={target.NameSource}: {announcement}");
            }
            if (!playSounds) return;
            _active = true;
            if (!EnsureAudio(now) || _audio.IsPlaying ||
                !Timing.IsDue(target.Session, now, target.Distance, target.Range)) return;
            if (!WorldAudioReservation.TryBegin(WorldAudioSource.Npc, now,
                NpcCueSynthesizer.DurationSeconds, AudioBufferMilliseconds)) return;
            Timing.TryTake(target.Session, now, target.Distance, target.Range);
            _audio.Play(NpcCueSynthesizer.Synthesize(target.Pan, target.Forward, _volume.Value / 100f));
            _lastFailure = null;
        }
        catch (Exception ex)
        {
            Suspend();
            ReportFailure(ex.Message);
        }
    }

    private static void ReportFailure(string message)
    {
        if (_lastFailure == message) return;
        _lastFailure = message;
        Plugin.Log.LogWarning($"NPC sounds unavailable: {message}");
    }

    private static bool EnsureAudio(double now)
    {
        if (_audio?.IsRunning == true) return true;
        WorldAudioReservation.Cancel(WorldAudioSource.Npc);
        if (now < _nextAudioRetry) return false;
        _nextAudioRetry = now + AudioRetrySeconds;
        try
        {
            if (_audio == null)
            {
                _audio = new RadarAudioOutput("NPC cues", AudioBufferMilliseconds);
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
            if (_lastAudioFailure != ex.Message)
                Plugin.Log.LogWarning($"NPC audio initialization failed: {ex.Message}");
            _lastAudioFailure = ex.Message;
            return false;
        }
    }

    private static void Suspend()
    {
        WorldAudioReservation.Cancel(WorldAudioSource.Npc);
        if (_active) _audio?.Silence();
        _active = false;
        _targetId = 0;
        _session = 0;
        Timing.Reset();
        Names.Reset();
        World.Reset();
    }

    internal static void Cleanup()
    {
        Suspend();
        _audio?.Dispose();
        _audio = null;
        _enabled = null;
        _announceNames = null;
        _nextPoll = 0;
        _nextAudioRetry = 0;
        _lastFailure = null;
        _lastAudioFailure = null;
    }
}
