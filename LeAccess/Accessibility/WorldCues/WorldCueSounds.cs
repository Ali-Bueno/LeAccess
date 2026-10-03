using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;

namespace LeAccess.Accessibility.WorldCues;

internal static class WorldCueSounds
{
    private const string ConfigSection = "World object sounds";
    private const double PollSeconds = 0.1; // Match the existing NPC world probe cadence.
    private const double AudioRetrySeconds = 5;
    private const int AudioBufferMilliseconds = 100; // Match the existing terrain output buffer.
    private const double DiagnosticIntervalSeconds = 5;
    // Roughly the visible play area; D2RAccess loops fade out within a comparable radius.
    private const int DefaultLoopRangeMetres = 12;
    private const int MaxLoopRangeMetres = 30;
    private static readonly WorldCueProbe World = new();
    private static readonly WorldCueState State = new();
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<int> _volume;
    private static ConfigEntry<int> _loopRange;
    private static ConfigEntry<bool> _loops;
    private static LoopAudioOutput _loopAudio;
    private static double _nextLoopRetry;
    private static readonly List<LoopVoice> LoopVoices = new();
    private static readonly HashSet<int> LoggedLoops = new();
    private static readonly HashSet<int> CurrentLoops = new();
    private static RadarAudioOutput _audio;
    private static double _nextPoll, _nextAudioRetry;
    private static long _session;
    private static string _lastFailure;
    private static string _lastSampleFailure;
    private static double _nextDiagnostic;
    private static readonly WorldCueKey?[] LoggedTargets = new WorldCueKey?[3];
    private static WorldCueKey? _playingTarget;
    internal static string DiagnosticStatus { get; private set; } = "not initialized";

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind(ConfigSection, "Enabled", true,
            "Repeat distinct directional sounds while coins, equipment and usable world objects remain nearby.");
        _volume = config.Bind(ConfigSection, "Volume", 100,
            new ConfigDescription("World object cue volume in percent.", new AcceptableValueRange<int>(0, 100)));
        _loops = config.Bind(ConfigSection, "Loops", true,
            "Play gold, equipment, chests, the stash, portals, waypoints and zone exits as continuous D2R loops, " +
            "several at once; false restores one short repeated cue per kind.");
        _loopRange = config.Bind(ConfigSection, "LoopRange", DefaultLoopRangeMetres,
            new ConfigDescription("Metres from which looping objects (and, with loops off, portals, waypoints, exits and the stash) " +
                "sound, quieter when far. 0 limits them to the game's own range.", new AcceptableValueRange<int>(0, MaxLoopRangeMetres)));
        Plugin.Log.LogInfo("World sounds configured (coins, equipment, interactables; current or nearest per category)");
    }

    internal static void Update()
    {
        if (_enabled == null) return;
        try
        {
            double now = Time.unscaledTimeAsDouble;
            string suspension = !_enabled.Value ? "disabled" : _volume.Value <= 0 ? "volume zero" :
                NavigationAudioState.IsSuspended ? "navigation suspended" : !GameplayInputState.OwnsInput() ? "menu input" :
                ScreenContentAnnouncer.HasActiveContent ? "screen content" : DialogueAnnouncer.HasActiveContent ? "dialogue" : null;
            if (suspension != null) { ReportStatus(suspension, now); Suspend(); return; }
            if (now < _nextPoll) return;
            _nextPoll = now + PollSeconds;
            float loopRange = Math.Clamp(_loopRange.Value, 0, MaxLoopRangeMetres);
            World.LandmarkRange = loopRange;
            World.LoopRange = loopRange;
            World.Loopable = _loops.Value && loopRange > 0 ? WorldLoopBank.Has : null;
            bool found = World.TryCapture(out var target);
            if (World.LastFailure != null) ReportFailure(World.LastFailure);
            if (!World.HasWorld)
            {
                ReportStatus("world unavailable", now);
                Suspend();
                return;
            }
            if (_session != World.Session)
            {
                _audio?.Silence();
                _loopAudio?.Silence();
                State.Reset();
                WorldAudioReservation.Cancel(WorldAudioSource.Object);
                _session = World.Session;
            }
            UpdateLoops(now, loopRange);
            ReportStatus(found || LoopVoices.Count > 0 ? "targets available" : "no eligible targets", now);
            if (_playingTarget.HasValue && World.Targets[(int)_playingTarget.Value.Category]?.Key != _playingTarget.Value)
            {
                if (_audio?.IsPlaying == true) _audio.Silence();
                _playingTarget = null;
            }
            for (int i = 0; i < LoggedTargets.Length; i++)
                if (!World.Targets[i].HasValue) LoggedTargets[i] = null;
            if (!State.TryChoose(World.Targets, target, now, out target))
            { WorldAudioReservation.Cancel(WorldAudioSource.Object); return; }
            if (!EnsureAudio(now) || _audio.IsPlaying) return;
            if (!WorldAudioReservation.TryBegin(WorldAudioSource.Object, now,
                WorldCueSynthesizer.Duration(target.Key.Category, target.Sound), AudioBufferMilliseconds)) return;
            _audio.Play(WorldCueSynthesizer.Synthesize(target.Key.Category, target.Pan, target.Forward,
                _volume.Value / 100f * target.Gain, target.Sound));
            if (NaturalCueBank.LastFailure != null && _lastSampleFailure != NaturalCueBank.LastFailure)
            {
                _lastSampleFailure = NaturalCueBank.LastFailure;
                Plugin.Log.LogWarning($"Natural world cue unavailable; retaining synthesized fallback: {_lastSampleFailure}");
            }
            _playingTarget = target.Key;
            State.MarkPlayed(target, now);
            _lastFailure = null;
            if (LoggedTargets[(int)target.Key.Category] != target.Key)
            {
                Plugin.Log.LogInfo($"[World cue] category={target.Key.Category} sound={target.Sound?.ToString() ?? "category"} target={target.Key.InstanceId} distance={target.Distance:F2} range={target.Range:F2}");
                LoggedTargets[(int)target.Key.Category] = target.Key;
            }
        }
        catch (Exception ex) { Suspend(); ReportFailure(ex.Message); }
    }

    private static void UpdateLoops(double now, float range)
    {
        WorldLoopSelection.Select(World.Loops, range, _volume.Value / 100f, LoopVoices);
        CurrentLoops.Clear();
        foreach (var voice in LoopVoices) CurrentLoops.Add(voice.Id);
        LoggedLoops.IntersectWith(CurrentLoops);
        if (LoopVoices.Count == 0 && _loopAudio == null) return;
        if (!EnsureLoopAudio(now)) return;
        _loopAudio.SetVoices(LoopVoices);
        foreach (var voice in LoopVoices)
        {
            if (!LoggedLoops.Add(voice.Id)) continue;
            var candidate = World.Loops.Find(c => c.Id == voice.Id && c.Kind == voice.Kind);
            Plugin.Log.LogInfo($"[World loop] sound={voice.Kind} target={voice.Id} distance={candidate.Distance:F2} range={range:F2}");
        }
        if (WorldLoopBank.LastFailure != null && _lastSampleFailure != WorldLoopBank.LastFailure)
        {
            _lastSampleFailure = WorldLoopBank.LastFailure;
            Plugin.Log.LogWarning($"World loop unavailable; that kind keeps its repeated cue: {_lastSampleFailure}");
        }
    }

    private static bool EnsureLoopAudio(double now)
    {
        if (_loopAudio?.IsRunning == true) return true;
        if (now < _nextLoopRetry) return false;
        _nextLoopRetry = now + AudioRetrySeconds;
        try
        {
            if (_loopAudio == null) { _loopAudio = new LoopAudioOutput("World object loops", AudioBufferMilliseconds); return true; }
            bool recovered = _loopAudio.TryRecover(out string status);
            if (status != null) ReportFailure(status);
            return recovered;
        }
        catch (Exception ex) { ReportFailure($"World object loops unavailable: {ex.Message}"); return false; }
    }

    private static bool EnsureAudio(double now)
    {
        if (_audio?.IsRunning == true) return true;
        WorldAudioReservation.Cancel(WorldAudioSource.Object);
        if (now < _nextAudioRetry) return false;
        _nextAudioRetry = now + AudioRetrySeconds;
        if (_audio == null)
        { _audio = new RadarAudioOutput("World object cues", AudioBufferMilliseconds); return true; }
        bool recovered = _audio.TryRecover(out string status);
        if (status != null) ReportFailure(status);
        return recovered;
    }

    private static void Suspend()
    {
        if (_session != 0) _audio?.Silence();
        _session = 0;
        Array.Clear(LoggedTargets, 0, LoggedTargets.Length);
        _loopAudio?.Silence();
        LoopVoices.Clear();
        LoggedLoops.Clear();
        _playingTarget = null;
        State.Reset();
        World.Reset();
        WorldAudioReservation.Cancel(WorldAudioSource.Object);
    }

    private static void ReportFailure(string message)
    {
        if (_lastFailure == message) return;
        _lastFailure = message;
        Plugin.Log.LogWarning($"World sounds: {message}");
    }

    private static void ReportStatus(string status, double now)
    {
        DiagnosticStatus = status;
        if (now < _nextDiagnostic) return;
        _nextDiagnostic = now + DiagnosticIntervalSeconds;
        Plugin.Log.LogInfo($"[World sounds] {status}; {World.Diagnostics} loops={LoopVoices.Count}/{World.Loops.Count}; " +
            $"audio={(_audio == null ? "not initialized" : _audio.Diagnostics.ToString())}; " +
            $"loopAudio={(_loopAudio == null ? "not initialized" : _loopAudio.Diagnostics)}");
    }

    internal static void Cleanup()
    {
        Suspend();
        _audio?.Dispose();
        _audio = null;
        _loopAudio?.Dispose();
        _loopAudio = null;
        _nextLoopRetry = 0;
        _enabled = null;
        _loopRange = null;
        _loops = null;
        _nextPoll = _nextAudioRetry = 0;
        _lastFailure = null;
        _lastSampleFailure = null;
        _nextDiagnostic = 0;
        DiagnosticStatus = "not initialized";
    }
}
