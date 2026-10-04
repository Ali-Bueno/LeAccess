using System;
using LeAccess.Accessibility.Audio;

namespace LeAccess.Accessibility.Exploration;

// The beacon's own output: one waiting sound, taking turns with the other world cues.
internal static class BeaconAudio
{
    // Device startup retries are spaced out as in the reactive radar.
    private const double RetrySeconds = 5;
    private static RadarAudioOutput _output;
    private static BeaconSound? _pending;
    private static float _pan, _ahead;
    private static double _nextRetry;
    private static string _lastFailure;

    internal static int Pings { get; private set; }
    internal static string Diagnostics => _output == null ? "not initialized" : _output.Diagnostics.ToString();

    // Confirmations are never replaced by a ping still waiting, as in D2R.
    internal static void Queue(BeaconSound sound)
    {
        _pending = sound;
        _pan = _ahead = 0;
    }

    internal static void QueuePing(float pan, float ahead)
    {
        if (_pending is not (null or BeaconSound.Ping)) return;
        _pending = BeaconSound.Ping;
        _pan = pan;
        _ahead = ahead;
    }

    internal static void DropPing()
    {
        if (_pending == BeaconSound.Ping) _pending = null;
    }

    internal static void Play(double now, int volume, int bufferMilliseconds)
    {
        if (_pending is not { } sound || volume <= 0)
        {
            _pending = null;
            WorldAudioReservation.Cancel(WorldAudioSource.Guidance);
            return;
        }
        if (!EnsureOutput(now, bufferMilliseconds) || _output.IsPlaying) return;
        if (!WorldAudioReservation.TryBegin(WorldAudioSource.Guidance, now, BeaconCueSynthesizer.Duration(sound),
            bufferMilliseconds)) return;
        _output.Play(BeaconCueSynthesizer.Synthesize(sound, _pan, _ahead, volume / 100f));
        _pending = null;
        if (sound == BeaconSound.Ping) Pings++;
    }

    // UI feedback, so it skips rather than waits when the output is busy.
    internal static void Click(double now, int volume, int bufferMilliseconds)
    {
        if (volume <= 0 || !EnsureOutput(now, bufferMilliseconds) || _output.IsPlaying) return;
        _output.Play(BeaconCueSynthesizer.Click(volume / 100f));
    }

    internal static void Silence()
    {
        _pending = null;
        WorldAudioReservation.Cancel(WorldAudioSource.Guidance);
        _output?.Silence();
    }

    private static bool EnsureOutput(double now, int bufferMilliseconds)
    {
        if (_output?.IsRunning == true) return true;
        WorldAudioReservation.Cancel(WorldAudioSource.Guidance);
        if (now < _nextRetry) return false;
        try
        {
            if (_output != null)
            {
                _nextRetry = now + RetrySeconds;
                bool recovered = _output.TryRecover(out string status);
                if (status != null && status != _lastFailure)
                {
                    if (recovered) Plugin.Log.LogInfo(status);
                    else Plugin.Log.LogWarning(status);
                }
                _lastFailure = recovered ? null : status;
                return recovered;
            }
            _output = new RadarAudioOutput("Exploration beacon", bufferMilliseconds);
            _lastFailure = null;
            return true;
        }
        catch (Exception ex)
        {
            _nextRetry = now + RetrySeconds;
            if (_lastFailure != ex.Message)
                Plugin.Log.LogWarning($"Exploration beacon audio initialization failed: {ex.Message}");
            _lastFailure = ex.Message;
            return false;
        }
    }

    internal static void Cleanup()
    {
        Silence();
        _output?.Dispose();
        _output = null;
        _nextRetry = 0;
        _lastFailure = null;
        Pings = 0;
    }
}
