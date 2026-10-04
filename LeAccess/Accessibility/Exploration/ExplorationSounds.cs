using System;
using System.Collections.Generic;
using System.Numerics;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.WorldCues;

namespace LeAccess.Accessibility.Exploration;

// D2R exploration sounds (d2r-access docs/exploration.md): a spatial loop at the nearest unvisited room, a
// centred cue on first entering one, and the discovery sound. Own outputs, so loot loops and world cues
// never replace them.
internal static class ExplorationSounds
{
    private const double RetrySeconds = 5;
    private const int BufferMilliseconds = 100;
    private const double GlideSeconds = RadarSettings.PollSeconds;
    private const int RoomVoiceId = 1;
    // A discovery and a room entry can coincide; anything more is dropped rather than played late.
    private const int MaximumPending = 2;
    private static readonly Queue<NaturalCueKind> Pending = new();
    private static readonly List<LoopCandidate> Candidates = new();
    private static readonly List<LoopVoice> Voices = new();
    private static LoopAudioOutput _loop;
    private static RadarAudioOutput _cues;
    private static double _nextLoopRetry, _nextCueRetry;
    private static string _lastFailure;

    internal static void Queue(NaturalCueKind kind)
    {
        if (Pending.Count < MaximumPending && !Pending.Contains(kind)) Pending.Enqueue(kind);
    }

    // rooms are the unvisited rooms; the nearest within range of its nearest point sounds, as in D2R.
    internal static void Update(double now, IReadOnlyList<MinimapRoom> rooms, Vector3 player, Vector3 forward,
        Vector3 right, float range, float volume)
    {
        try
        {
            Candidates.Clear();
            if (rooms is not null && RoomTracker.TryNearest(rooms, player, forward, right, out float distance, out float pan,
                out float front))
                Candidates.Add(new LoopCandidate(RoomVoiceId, NaturalCueKind.UnexploredRoom, distance, pan, front));
            WorldLoopSelection.Select(Candidates, range, volume, Voices);
            if (Voices.Count > 0 || _loop is not null)
                if (EnsureLoop(now)) _loop.SetVoices(Voices);
            if (Pending.Count > 0 && EnsureCues(now) && !_cues.IsPlaying)
            {
                var kind = Pending.Dequeue();
                if (volume > 0 && NaturalCueBank.TryBuild(kind, 0, 1, volume, out var samples)) _cues.Play(samples);
            }
        }
        catch (Exception ex) { Report($"Exploration sounds unavailable: {ex.Message}"); }
    }

    internal static void Silence()
    {
        Pending.Clear();
        Voices.Clear();
        _loop?.SetVoices(Voices);
        _cues?.Silence();
    }

    private static bool EnsureLoop(double now)
    {
        if (_loop?.IsRunning == true) return true;
        if (now < _nextLoopRetry) return false;
        _nextLoopRetry = now + RetrySeconds;
        if (_loop == null)
        {
            _loop = new LoopAudioOutput("Exploration rooms", BufferMilliseconds, GlideSeconds);
            return true;
        }
        bool recovered = _loop.TryRecover(out string status);
        if (status != null) Report(status);
        return recovered;
    }

    private static bool EnsureCues(double now)
    {
        if (_cues?.IsRunning == true) return true;
        if (now < _nextCueRetry) return false;
        _nextCueRetry = now + RetrySeconds;
        if (_cues == null)
        {
            _cues = new RadarAudioOutput("Exploration cues", BufferMilliseconds);
            return true;
        }
        bool recovered = _cues.TryRecover(out string status);
        if (status != null) Report(status);
        return recovered;
    }

    private static void Report(string message)
    {
        if (message == _lastFailure) return;
        _lastFailure = message;
        Plugin.Log.LogWarning(message);
    }

    internal static void Cleanup()
    {
        Pending.Clear();
        Voices.Clear();
        _loop?.Dispose();
        _loop = null;
        _cues?.Dispose();
        _cues = null;
        _nextLoopRetry = _nextCueRetry = 0;
        _lastFailure = null;
    }
}
