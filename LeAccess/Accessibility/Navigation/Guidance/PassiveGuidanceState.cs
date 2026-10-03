using System;
using System.Numerics;

namespace LeAccess.Accessibility.Navigation.Guidance;

internal readonly record struct PassiveGuidanceObservation(long Session, int TargetId, string Name,
    bool IsQuest, Vector3 Player, Vector3 Destination, Vector3 Next, Vector3 Forward, Vector3 Right,
    float Distance, float BodyWidth, bool Complete);

internal enum PassiveGuidanceSpeechKind { Target, Turn, NearMarker }

internal readonly record struct PassiveGuidanceSpeech(PassiveGuidanceSpeechKind Kind, string Name,
    string Direction, bool IsQuest, float Distance);

internal sealed class PassiveGuidanceState
{
    // Listening preferences, not game geometry: debounce changing markers and avoid repeated turn narration.
    internal const double TargetSettleSeconds = 1;
    internal const double TurnSettleSeconds = 1;
    internal const double SpeechGapSeconds = 6;
    internal const double NearPulseSeconds = 0.8;
    internal const double FarPulseSeconds = 3;
    internal const float FarPulseBodyWidths = 32;
    private const float ArrivalReleaseBodyWidths = 2;
    private const int MinimumTurnSectors = 2;
    private static readonly string[] Directions =
        { "up", "up_right", "right", "down_right", "down", "down_left", "left", "up_left" };

    private PassiveGuidanceObservation _current;
    private bool _known, _valid, _near, _announcedNear;
    private double _readyAt, _directionSince, _nextPulse, _lastSpeech = double.NegativeInfinity, _lastTime;
    private string _direction, _spokenDirection;

    internal bool HasAnnouncedTarget { get; private set; }
    internal float Pan { get; private set; }
    internal float Forward { get; private set; }
    internal double PulseInterval { get; private set; }
    internal bool NearMarker => _valid && _near;

    internal bool Observe(PassiveGuidanceObservation value, double now)
    {
        if (!double.IsFinite(now)) { Suspend(); return false; }
        if (now < _lastTime || _known && value.Session != _current.Session) Reset();
        _lastTime = now;
        if (!value.Complete || string.IsNullOrWhiteSpace(value.Name) ||
            !GuidanceGeometry.Finite(value.Player) || !GuidanceGeometry.Finite(value.Destination) ||
            !GuidanceGeometry.Finite(value.Next) || !float.IsFinite(value.Distance) || value.Distance < 0 ||
            !float.IsFinite(value.BodyWidth) || value.BodyWidth <= 0)
        { Suspend(); return false; }

        bool changed = !_known || value.TargetId != _current.TargetId || value.Name != _current.Name ||
            value.IsQuest != _current.IsQuest;
        if (changed)
        {
            HasAnnouncedTarget = _near = _announcedNear = false;
            _spokenDirection = null;
            _nextPulse = now;
        }
        if (changed || !_valid) _readyAt = now + TargetSettleSeconds;
        if (_known && Vector3.Distance(value.Destination, _current.Destination) > value.BodyWidth)
            _near = _announcedNear = false;
        _near = value.Distance <= value.BodyWidth * (_near ? ArrivalReleaseBodyWidths : 1);
        if (!_near) _announcedNear = false;

        var delta = value.Next - value.Player;
        string direction = GuidanceGeometry.Direction(delta, value.Forward, value.Right);
        if (direction == null && !_near) { Suspend(); return false; }
        if (direction != _direction)
        { _direction = direction; _directionSince = now; }
        if (HasAnnouncedTarget && _spokenDirection == null && direction != null) _spokenDirection = direction;
        delta.Y = 0;
        var forward = value.Forward;
        var right = value.Right;
        forward.Y = right.Y = 0;
        if (delta.LengthSquared() > 0 && GuidanceGeometry.Finite(forward) && GuidanceGeometry.Finite(right) &&
            forward.LengthSquared() > 0 && right.LengthSquared() > 0)
        {
            var bearing = Vector3.Normalize(delta);
            Pan = Math.Clamp(Vector3.Dot(bearing, Vector3.Normalize(right)), -1, 1);
            Forward = Math.Clamp(Vector3.Dot(bearing, Vector3.Normalize(forward)), -1, 1);
        }
        else Pan = Forward = 0;
        float fraction = Math.Clamp((value.Distance / value.BodyWidth - 1) / (FarPulseBodyWidths - 1), 0, 1);
        PulseInterval = NearPulseSeconds + fraction * (FarPulseSeconds - NearPulseSeconds);
        _current = value;
        _known = _valid = true;
        return true;
    }

    internal bool PulseDue(double now) => _valid && !_near && now >= _readyAt && now >= _nextPulse;
    internal void PulsePlayed(double now) => _nextPulse = now + PulseInterval;

    internal bool TryTakeSpeech(double now, out PassiveGuidanceSpeech speech)
    {
        speech = default;
        if (!_valid || now < _readyAt || now - _lastSpeech < SpeechGapSeconds) return false;
        PassiveGuidanceSpeechKind kind;
        if (_near && !_announcedNear) kind = PassiveGuidanceSpeechKind.NearMarker;
        else if (!HasAnnouncedTarget) kind = PassiveGuidanceSpeechKind.Target;
        else if (!_near && now - _directionSince >= TurnSettleSeconds && MajorTurn(_spokenDirection, _direction))
            kind = PassiveGuidanceSpeechKind.Turn;
        else return false;
        speech = new(kind, _current.Name, _direction, _current.IsQuest, _current.Distance);
        HasAnnouncedTarget = true;
        _spokenDirection = _direction;
        _announcedNear = _near;
        _lastSpeech = now;
        return true;
    }

    private static bool MajorTurn(string from, string to)
    {
        if (from == null || to == null) return false;
        int oldSector = Array.IndexOf(Directions, from.Replace("guidance.direction.", ""));
        int newSector = Array.IndexOf(Directions, to.Replace("guidance.direction.", ""));
        if (oldSector < 0 || newSector < 0) return false;
        int difference = Math.Abs(newSector - oldSector);
        return Math.Min(difference, Directions.Length - difference) >= MinimumTurnSectors;
    }

    internal void Suspend()
    {
        _valid = false;
        _direction = null;
        _nextPulse = 0;
    }

    internal void Reset()
    {
        Suspend();
        _current = default;
        _known = _near = _announcedNear = HasAnnouncedTarget = false;
        _spokenDirection = null;
        _lastSpeech = double.NegativeInfinity;
        _readyAt = _directionSince = _lastTime = 0;
        Pan = Forward = 0;
    }
}
