using System;
using BepInEx.Configuration;
using LeAccess.Accessibility.Audio;

namespace LeAccess.Accessibility.Morditas;

// The pulse from the gauntlet circle's centre, then from the reward crystal, repeated while the player can act.
internal static class MorditasCircleCue
{
    // Mod choice (user request 2026-10-21): twice a second, so the centre is found at once.
    internal const double PulseSeconds = 0.5;
    // Device retries and buffer as the NPC cue output.
    private const double RetrySeconds = 5;
    private const int BufferMilliseconds = 100;
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<int> _volume;
    private static RadarAudioOutput _output;
    private static double _next, _nextRetry;
    private static string _lastFailure;

    internal static bool Enabled => _enabled?.Value == true && _volume?.Value > 0;

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind(BloodTrailCue.ConfigSection, "CircleSound", true,
            "During the Rage of Morditas gauntlet, pulse from the circle's centre, then from the reward crystal; lower and duller behind.");
        _volume = config.Bind(BloodTrailCue.ConfigSection, "CircleVolume", 100, new ConfigDescription(
            "Volume of the circle pulse, in percent; it is quieter toward the circle's edge.",
            new AcceptableValueRange<int>(0, 100)));
    }

    internal static void Play(double now, float pan, float ahead, float distanceGain)
    {
        if (!Enabled || now < _next) return;
        if (!EnsureOutput(now) || _output.IsPlaying) return;
        if (!WorldAudioReservation.TryBegin(WorldAudioSource.Circle, now, MorditasCircleSynthesizer.DurationSeconds,
            BufferMilliseconds)) return;
        _output.Play(MorditasCircleSynthesizer.Synthesize(pan, ahead, _volume.Value / 100f * distanceGain));
        _next = now + PulseSeconds;
    }

    internal static void Drop()
    {
        _next = 0;
        WorldAudioReservation.Cancel(WorldAudioSource.Circle);
    }

    private static bool EnsureOutput(double now)
    {
        if (_output?.IsRunning == true) return true;
        WorldAudioReservation.Cancel(WorldAudioSource.Circle);
        if (now < _nextRetry) return false;
        _nextRetry = now + RetrySeconds;
        try
        {
            if (_output == null)
            {
                _output = new RadarAudioOutput("Morditas circle cue", BufferMilliseconds);
                _lastFailure = null;
                return true;
            }
            bool recovered = _output.TryRecover(out string status);
            if (status != null && status != _lastFailure) Plugin.Log.LogInfo(status);
            _lastFailure = recovered ? null : status;
            return recovered;
        }
        catch (Exception ex)
        {
            if (_lastFailure != ex.Message) Plugin.Log.LogWarning($"Morditas circle audio initialization failed: {ex.Message}");
            _lastFailure = ex.Message;
            return false;
        }
    }

    internal static void Cleanup()
    {
        Drop();
        _output?.Dispose();
        _output = null;
        _nextRetry = 0;
        _lastFailure = null;
        _enabled = null;
        _volume = null;
    }
}
