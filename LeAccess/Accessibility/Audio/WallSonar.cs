using System;
using LeAccess.Accessibility.Navigation;
using NAudio.Wave;
using UnityEngine;

namespace LeAccess.Accessibility.Audio;

/// <summary>Wall tones, an alternative or companion to the radar: a soft continuous sound per screen direction, louder as a
/// wall comes closer (reference/navigation-radar.md#warm-wall-sounds-2026-10-11).</summary>
public static class WallSonar
{
    private const double AudioRetrySeconds = 5;
    private const int AudioBufferMilliseconds = 100;
    private static readonly RadarDirection[] Directions = { RadarDirection.Up, RadarDirection.Left, RadarDirection.Right, RadarDirection.Down };
    // Lane starts in body radii: centre, right, left.
    private static readonly float[] Lanes = { 0f, 1f, -1f };
    private static readonly RadarWorldProbe World = new();
    private static WallToneSynth _synth;
    private static WaveOutEvent _output;
    private static double _nextPoll, _nextAudioRetry;
    private static string _lastSuspension, _lastFailure;

    public static void Initialize() => _synth = new WallToneSynth(static () => SoundLevels.Master * SoundLevels.Sonar);

    public static void Update()
    {
        if (_synth == null) return;
        try
        {
            string suspension = !Plugin.ContinuousSonarEnabled ? "disabled in configuration"
                : NavigationAudioState.SuspensionReason ?? (GameplayInputState.OwnsInput() ? null : "gameplay input not active");
            if (suspension != null) { Suspend(suspension); return; }

            double now = Time.unscaledTimeAsDouble;
            if (now < _nextPoll) return;
            _nextPoll = now + RadarSettings.PollSeconds;
            if (!World.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out var snapshot, probeWalls: false))
            {
                Suspend(World.UnavailableReason);
                return;
            }
            if (!EnsureOutput(now)) return;
            if (_lastSuspension != null)
            {
                Plugin.Log.LogInfo("[Wall tones] playing");
                _lastSuspension = null;
            }
            float range = snapshot.Settings.ReachDepth;
            foreach (var direction in Directions)
                _synth.SetTarget(direction, WallToneSynth.Gain(Clearance(direction, snapshot.BodyRadius, range), range));
        }
        catch (Exception ex)
        {
            Suspend("probe error");
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"[Wall tones] unavailable: {ex}");
        }
    }

    // The farthest of three lanes across the body (Grimdark's lane rule): a wall the player brushes or a corner beside the
    // way does not read as a wall ahead.
    private static float Clearance(RadarDirection direction, float bodyRadius, float range)
    {
        float best = float.NaN;
        foreach (float lane in Lanes)
        {
            float depth = World.LaneDepth(direction, lane * bodyRadius, range);
            if (float.IsFinite(depth) && !(depth <= best)) best = depth;
        }
        return float.IsFinite(best) ? best : range;
    }

    private static bool EnsureOutput(double now)
    {
        if (_output?.PlaybackState == PlaybackState.Playing) return true;
        if (now < _nextAudioRetry) return false;
        _nextAudioRetry = now + AudioRetrySeconds;
        try
        {
            if (_output == null)
            {
                _output = new WaveOutEvent { DesiredLatency = AudioBufferMilliseconds };
                _output.Init(_synth);
            }
            _output.Play();
            return true;
        }
        catch (Exception ex)
        {
            _output?.Dispose();
            _output = null;
            if (_lastFailure != ex.Message) Plugin.Log.LogWarning($"[Wall tones] audio unavailable: {ex.Message}");
            _lastFailure = ex.Message;
            return false;
        }
    }

    private static void Suspend(string reason)
    {
        _synth?.Silence();
        World.Reset();
        if (reason == null || reason == _lastSuspension) return;
        if (_lastSuspension != null || reason != "disabled in configuration") Plugin.Log.LogInfo($"[Wall tones] silent: {reason}");
        _lastSuspension = reason;
    }

    public static void Cleanup()
    {
        try
        {
            _output?.Stop();
            _output?.Dispose();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[Wall tones] cleanup error: {ex.Message}");
        }
        _output = null;
        _synth = null;
        _nextPoll = _nextAudioRetry = 0;
        _lastSuspension = _lastFailure = null;
        World.Reset();
    }
}
