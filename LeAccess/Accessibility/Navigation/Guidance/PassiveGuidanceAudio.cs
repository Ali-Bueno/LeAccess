using System;
using LeAccess.Accessibility.Audio;

namespace LeAccess.Accessibility.Navigation.Guidance;

internal sealed class PassiveGuidanceAudio : IDisposable
{
    // Match the existing world-cue device buffering and avoid rapid retries after a device failure.
    private const int BufferMilliseconds = 50;
    private const double RetrySeconds = 5;
    private RadarAudioOutput _output;
    private double _nextRetry;
    private string _lastFailure;

    internal bool TryPlay(double now, float pan, float forward, float gain)
    {
        if (gain <= 0) { Silence(); return false; }
        try
        {
            if (_output?.IsRunning != true)
            {
                WorldAudioReservation.Cancel(WorldAudioSource.Guidance);
                if (now < _nextRetry) return false;
                _nextRetry = now + RetrySeconds;
                if (_output == null) _output = new RadarAudioOutput("Passive orientation", BufferMilliseconds);
                else if (!_output.TryRecover(out string status)) { ReportFailure(status); return false; }
                _lastFailure = null;
            }
            if (_output.IsPlaying) return false;
            const NaturalCueKind kind = NaturalCueKind.UnexploredRoom;
            double duration = NaturalCueBank.Duration(kind);
            if (duration <= 0) { ReportFailure(NaturalCueBank.LastFailure); return false; }
            if (!WorldAudioReservation.TryBegin(WorldAudioSource.Guidance, now,
                duration, BufferMilliseconds)) return false;
            _output.Play(NaturalCueBank.Build(kind, pan, forward, gain));
            return true;
        }
        catch (Exception ex)
        {
            Silence();
            _nextRetry = now + RetrySeconds;
            ReportFailure(ex.Message);
            return false;
        }
    }

    private void ReportFailure(string message)
    {
        if (message == null || message == _lastFailure) return;
        _lastFailure = message;
        Plugin.Log.LogWarning($"Passive orientation audio unavailable: {message}");
    }

    internal void Silence()
    {
        CancelPending();
        _output?.Silence();
    }

    internal void CancelPending() => WorldAudioReservation.Cancel(WorldAudioSource.Guidance);

    public void Dispose()
    {
        Silence();
        _output?.Dispose();
        _output = null;
        _nextRetry = 0;
        _lastFailure = null;
    }
}
