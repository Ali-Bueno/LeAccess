using System;

namespace LeAccess.Accessibility.Combat;

internal sealed class NearestEnemyAnnouncementState
{
    // Accessibility speech pacing, independent of the game's combat timing.
    internal const double DefaultSpeechIntervalSeconds = 2;
    private string _lastSpokenName;
    private double _nextSpeech;
    private double _lastObservation = double.NaN;

    internal double SpeechIntervalSeconds { get; set; } = DefaultSpeechIntervalSeconds;

    internal bool TryAnnounce(string nearestName, double now, bool speechAvailable, out string announcement)
    {
        announcement = null;
        if (!double.IsFinite(now)) return false;
        if (now < _lastObservation) Reset();
        _lastObservation = now;

        if (string.IsNullOrWhiteSpace(nearestName))
        {
            _lastSpokenName = null;
            return false;
        }
        if (!speechAvailable || now < _nextSpeech || nearestName == _lastSpokenName) return false;

        announcement = nearestName;
        _lastSpokenName = nearestName;
        _nextSpeech = now + SpeechIntervalSeconds;
        return true;
    }

    internal void Reset()
    {
        _lastSpokenName = null;
        _nextSpeech = 0;
        _lastObservation = double.NaN;
    }
}
