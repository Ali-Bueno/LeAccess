using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace LeAccess.Accessibility.Audio;

// Persistent device stream for the world loop mixer; same stop/recovery contract as RadarAudioOutput.
internal sealed class LoopAudioOutput : IDisposable
{
    private readonly object _gate = new();
    private readonly WorldLoopMixer _mixer = new(WorldLoopBank.Get);
    private readonly string _description;
    private readonly int _latency;
    private IWavePlayer _output;
    private bool _completionConfirmed, _disposed;
    private Exception _lastException;
    private int _stops, _recoveries;

    internal LoopAudioOutput(string description, int latencyMilliseconds)
    {
        if (latencyMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(latencyMilliseconds));
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
                return $"running={!_disposed && _output?.PlaybackState == PlaybackState.Playing} voices={_mixer.ActiveVoices} " +
                    $"frames={_mixer.FramesMixed} stops={_stops} recoveries={_recoveries} failure={Failure() ?? "none"}";
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
        var output = new WaveOutEvent { DesiredLatency = _latency };
        _output = output;
        output.PlaybackStopped += OnStopped;
        try
        {
            output.Init(_mixer);
            _completionConfirmed = false;
            output.Play();
        }
        catch
        {
            Release();
            throw;
        }
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
