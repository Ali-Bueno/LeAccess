using System;
using System.Collections.Generic;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace LeAccess.Accessibility.Audio;

// Persistent device stream for the world loop mixer; same stop/recovery contract as RadarAudioOutput.
internal sealed class LoopAudioOutput : IDisposable
{
    private readonly object _gate = new();
    private readonly WorldLoopMixer _mixer;
    private readonly string _description;
    private readonly int _latency;
    private IWavePlayer _output;
    private bool _completionConfirmed, _disposed;
    private Exception _lastException;
    private int _stops, _recoveries;
    private string _backend, _wasapiFailure;

    /// <summary>Starts the device stream; voice gains glide with the given time constant.</summary>
    internal LoopAudioOutput(string description, int latencyMilliseconds, double smoothingSeconds)
    {
        if (latencyMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(latencyMilliseconds));
        _mixer = new WorldLoopMixer(WorldLoopBank.Get, smoothingSeconds);
        _description = description;
        _latency = latencyMilliseconds;
        Start();
    }

    internal bool IsRunning { get { lock (_gate) return !_disposed && _output?.PlaybackState == PlaybackState.Playing; } }

    internal string Diagnostics
    {
        get
        {
            lock (_gate)
                return FormattableString.Invariant(
                    $"running={!_disposed && _output?.PlaybackState == PlaybackState.Playing} backend={_backend ?? "none"} ") +
                    (_wasapiFailure == null ? "" : $"wasapi_failure=\"{_wasapiFailure}\" ") +
                    FormattableString.Invariant($"voices={_mixer.ActiveVoices} frames={_mixer.FramesMixed} ") +
                    FormattableString.Invariant($"max_read_gap_ms={_mixer.MaxReadGapMilliseconds:F1} ") +
                    $"stops={_stops} recoveries={_recoveries} failure={Failure() ?? "none"}";
        }
    }

    internal void SetVoices(IReadOnlyList<LoopVoice> voices)
    {
        if (!_disposed) _mixer.SetVoices(voices);
    }

    internal void Silence()
    {
        if (!_disposed) _mixer.Silence();
    }

    // Main thread, at the caller's retry interval.
    internal bool TryRecover(out string status)
    {
        lock (_gate)
        {
            status = null;
            if (_disposed) return false;
            if (_output?.PlaybackState == PlaybackState.Playing) return true;
            if (!_completionConfirmed) return false;
            string reason = Failure();
            try
            {
                Release();
                Start();
                _recoveries++;
                status = $"{_description} audio restarted after {reason}";
                return true;
            }
            catch (Exception ex)
            {
                _lastException = ex;
                _completionConfirmed = true;
                status = $"{_description} audio restart failed: {Failure()}";
                return false;
            }
        }
    }

    private void Start()
    {
        var output = CreateOutput();
        _output = output;
        output.PlaybackStopped += OnStopped;
        try
        {
            _completionConfirmed = false;
            output.Play();
        }
        catch
        {
            Release();
            throw;
        }
    }

    // D2RAccess loops through XAudio2, which renders via WASAPI; event-driven shared WASAPI
    // refills as the device drains instead of MME WaveOut's two half-latency blocks, kept as fallback.
    private IWavePlayer CreateOutput()
    {
        try
        {
            var wasapi = new WasapiOut(AudioClientShareMode.Shared, true, _latency);
            try { wasapi.Init(_mixer); }
            catch { wasapi.Dispose(); throw; }
            _backend = "wasapi";
            return wasapi;
        }
        catch (Exception ex)
        {
            _wasapiFailure = $"{ex.GetType().Name}: {ex.Message}";
        }
        var wave = new WaveOutEvent { DesiredLatency = _latency };
        try { wave.Init(_mixer); }
        catch { wave.Dispose(); throw; }
        _backend = "waveout";
        return wave;
    }

    private void OnStopped(object sender, StoppedEventArgs stopped)
    {
        lock (_gate)
        {
            if (_disposed || !ReferenceEquals(sender, _output)) return;
            _lastException = stopped.Exception;
            _stops++;
            // NAudio raises this only after its playback thread has finished.
            _completionConfirmed = true;
        }
    }

    private string Failure() => _lastException != null
        ? $"{_lastException.GetType().Name}: {_lastException.Message}"
        : _stops == 0 ? null : "stream ended without an error";

    private void Release()
    {
        var output = _output;
        _output = null;
        if (output == null) return;
        output.PlaybackStopped -= OnStopped;
        output.Dispose();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            Release();
        }
    }
}
