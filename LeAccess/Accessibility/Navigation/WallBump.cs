using System;
using BepInEx.Configuration;
using LeAccess.Accessibility.Audio;
using UnityEngine;
using NumericsVector2 = System.Numerics.Vector2;

namespace LeAccess.Accessibility.Navigation;

/// <summary>A thud, and silent footsteps, while the stick pushes the player into a wall without advancing
/// (reference/navigation-radar.md#wall-bump-2026-10-11).</summary>
public static class WallBump
{
    private const string ConfigSection = "Reactive radar";
    private const double AudioRetrySeconds = 5;
    // The radar's listening-tested default buffer.
    private const int AudioBufferMilliseconds = 100;
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<int> _volume;
    private static ConfigEntry<bool> _muteFootsteps;
    private static readonly RadarWorldProbe World = new();
    private static WallBumpDetector _detector;
    private static RadarSettings _settings;
    private static RadarAudioOutput _audio;
    private static double _nextPoll, _nextAudioRetry;
    private static string _lastSuspension = "initializing", _lastFailure;
    private static MovementIntent.Source _lastSource;
    private static bool _stallLogged;

    public static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind(ConfigSection, "WallBump", true,
            "Play a thud when the stick pushes the player into a wall and the player stops walking.");
        _volume = config.Bind(ConfigSection, "WallBumpVolume", 100,
            new ConfigDescription("Percent of the wall bump volume.", new AcceptableValueRange<int>(0, 100)));
        _muteFootsteps = config.Bind(ConfigSection, "MuteFootstepsAtWalls", true,
            "Silence the character's footsteps while the stick pushes into a wall without advancing.");
    }

    public static void Update()
    {
        if (_enabled == null) return;
        try
        {
            bool bumps = _enabled.Value && _volume.Value > 0;
            string suspension = !bumps && !_muteFootsteps.Value ? "disabled in configuration"
                : NavigationAudioState.SuspensionReason;
            if (suspension == null && !GameplayInputState.OwnsInput()) suspension = "gameplay input not active";
            if (suspension != null) { Suspend(suspension); return; }

            double now = Time.unscaledTimeAsDouble;
            if (now < _nextPoll) return;
            _nextPoll = now + RadarSettings.PollSeconds;
            if (!World.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out var snapshot, probeWalls: false))
            {
                Suspend(World.UnavailableReason);
                return;
            }
            if (_lastSuspension != null)
            {
                Plugin.Log.LogInfo("[Wall bump] ready");
                _lastSuspension = null;
            }
            if (!ReferenceEquals(_settings, snapshot.Settings))
            {
                _settings = snapshot.Settings;
                _detector = new WallBumpDetector(_settings.ConfirmDistance);
            }

            var right = Ground(snapshot.Axes[(int)RadarDirection.Right]);
            var forward = Ground(snapshot.Axes[(int)RadarDirection.Up]);
            if (!MovementIntent.TryGetPush(right, forward, out var push, out var source))
            {
                _detector.Release();
                _stallLogged = false;
                FootstepMute.Restore();
                return;
            }
            if (source != _lastSource)
            {
                Plugin.Log.LogInfo($"[Wall bump] push read from the {source} state");
                _lastSource = source;
            }

            float clearance = World.Clearance(push, snapshot.BodyRadius);
            bool contact = clearance < snapshot.BodyRadius;
            bool bump = _detector.Tick(snapshot.Position, new NumericsVector2(push.x, push.z), contact, now);
            if (!_detector.Stalled) _stallLogged = false;
            else if (!contact && !_stallLogged)
            {
                Plugin.Log.LogInfo($"[Wall bump] stopped without a wall within {snapshot.BodyRadius:0.00} (clearance {clearance:0.00})");
                _stallLogged = true;
            }
            if (_detector.Pressing && _muteFootsteps.Value) FootstepMute.Mute();
            else FootstepMute.Restore();
            if (bump && bumps) Play(push, right, clearance, now);
        }
        catch (Exception ex)
        {
            Suspend("probe error");
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"[Wall bump] unavailable: {ex}");
        }
    }

    private static void Play(Vector3 push, Vector3 right, float clearance, double now)
    {
        if (!EnsureAudio(now)) return;
        float pan = Vector3.Dot(push.normalized, right);
        _audio.Silence();
        _audio.Play(BumpCueSynthesizer.Synthesize(pan, _volume.Value / 100f));
        Plugin.Log.LogInfo($"[Wall bump] bump pan={pan:0.00} clearance={clearance:0.00}");
    }

    private static bool EnsureAudio(double now)
    {
        if (_audio?.IsRunning == true) return true;
        if (now < _nextAudioRetry) return false;
        _nextAudioRetry = now + AudioRetrySeconds;
        try
        {
            if (_audio != null) return _audio.TryRecover(out _);
            _audio = new RadarAudioOutput("Wall bump", AudioBufferMilliseconds);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[Wall bump] audio initialization failed: {ex.Message}");
            return false;
        }
    }

    private static Vector3 Ground(NumericsVector2 axis) => new(axis.X, 0f, axis.Y);

    private static void Suspend(string reason)
    {
        if (reason != null && reason != _lastSuspension)
        {
            Plugin.Log.LogInfo($"[Wall bump] suspended: {reason}");
            _lastSuspension = reason;
        }
        _detector?.Release();
        _stallLogged = false;
        FootstepMute.Restore();
        World.Reset();
    }

    public static void Cleanup()
    {
        Suspend(null);
        _audio?.Dispose();
        _audio = null;
        _detector = null;
        _settings = null;
        _enabled = null;
        _muteFootsteps = null;
        _nextPoll = _nextAudioRetry = 0;
        _lastSuspension = "initializing";
        _lastFailure = null;
        _lastSource = MovementIntent.Source.None;
    }
}
