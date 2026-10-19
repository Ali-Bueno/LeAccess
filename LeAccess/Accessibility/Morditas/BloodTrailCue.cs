using System;
using BepInEx.Configuration;
using LeAccess.Accessibility.Audio;

namespace LeAccess.Accessibility.Morditas;

// One drip per blood-trail refresh, waiting its turn with the other world cues until the game's next refresh is due.
internal static class BloodTrailCue
{
    internal const string ConfigSection = "Rage of Morditas";
    // Device retries and buffer as the NPC cue output.
    private const double RetrySeconds = 5;
    private const int BufferMilliseconds = 100;
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<int> _volume;
    private static RadarAudioOutput _output;
    private static double _pendingUntil = double.NegativeInfinity, _nextRetry;
    private static string _lastFailure;

    internal static bool Pending => _pendingUntil > double.NegativeInfinity;

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind(ConfigSection, "BloodTrailSound", true,
            "During the Rage of Morditas hunt, play a drip from the direction of the nearest blood trail's target each time the game draws the trails.");
        _volume = config.Bind(ConfigSection, "BloodTrailVolume", 100, new ConfigDescription(
            "Volume of the blood trail drip, in percent; it is quieter when the target is far.",
            new AcceptableValueRange<int>(0, 100)));
    }

    internal static void Queue(double until)
    {
        if (_enabled?.Value == true && _volume.Value > 0) _pendingUntil = until;
    }

    internal static void Play(double now, float pan, float ahead, float distanceGain)
    {
        if (!Pending) return;
        if (now > _pendingUntil || _enabled?.Value != true || _volume.Value <= 0) { Drop(); return; }
        if (!EnsureOutput(now) || _output.IsPlaying) return;
        if (!WorldAudioReservation.TryBegin(WorldAudioSource.Trail, now, BloodTrailSynthesizer.DurationSeconds,
            BufferMilliseconds)) return;
        _output.Play(BloodTrailSynthesizer.Synthesize(pan, ahead, _volume.Value / 100f * distanceGain));
        _pendingUntil = double.NegativeInfinity;
    }

    internal static void Drop()
    {
        _pendingUntil = double.NegativeInfinity;
        WorldAudioReservation.Cancel(WorldAudioSource.Trail);
    }

    private static bool EnsureOutput(double now)
    {
        if (_output?.IsRunning == true) return true;
        WorldAudioReservation.Cancel(WorldAudioSource.Trail);
        if (now < _nextRetry) return false;
        _nextRetry = now + RetrySeconds;
        try
        {
            if (_output == null)
            {
                _output = new RadarAudioOutput("Blood trail cue", BufferMilliseconds);
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
            if (_lastFailure != ex.Message) Plugin.Log.LogWarning($"Blood trail audio initialization failed: {ex.Message}");
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
