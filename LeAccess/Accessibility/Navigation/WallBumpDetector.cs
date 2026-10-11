using System;
using System.Numerics;

namespace LeAccess.Accessibility.Navigation;

/// <summary>Detects the player pushing the stick into a wall without advancing (reference/navigation-radar.md#wall-bump-2026-10-11).</summary>
public sealed class WallBumpDetector
{
    // Turning the push by half of a radar screen sector (four sectors) means a different wall, such as a corner's other side.
    private static readonly float TurnCosine = MathF.Cos(MathF.PI / 4);

    private readonly float _moveDistance;
    private Vector2 _anchor, _bumpDirection;
    private double _anchorTime, _lastTick, _lastBump;
    private bool _hasAnchor, _armed = true, _hasBump;

    /// <summary>Pushing without moving one confirmation step for the radar's stopped time, wall or not.</summary>
    public bool Stalled { get; private set; }
    /// <summary>Stalled with a wall that way: the footsteps the game keeps playing are not real steps.</summary>
    public bool Pressing { get; private set; }

    /// <param name="moveDistance">Travel that counts as moving: the radar's confirmation distance.</param>
    public WallBumpDetector(float moveDistance)
    {
        if (!float.IsFinite(moveDistance) || moveDistance <= 0) throw new ArgumentOutOfRangeException(nameof(moveDistance));
        _moveDistance = moveDistance;
    }

    /// <summary>One poll; <paramref name="push"/> is the stick's world XZ direction (zero when released),
    /// <paramref name="contact"/> whether a wall lies within the body radius that way. True when a bump should sound.</summary>
    public bool Tick(Vector2 position, Vector2 push, bool contact, double now)
    {
        bool gap = _hasAnchor && (now < _lastTick || now - _lastTick > RadarSettings.MaxGapSeconds);
        _lastTick = now;
        if (gap || !Finite(position) || !Finite(push) || push.LengthSquared() <= 0 || !double.IsFinite(now))
        {
            Release();
            return false;
        }

        if (!_hasAnchor || Vector2.Distance(position, _anchor) >= _moveDistance)
        {
            if (_hasAnchor) _armed = true;
            _anchor = position;
            _anchorTime = now;
            _hasAnchor = true;
        }
        Stalled = now - _anchorTime >= RadarSettings.StoppedSeconds;
        Pressing = contact && Stalled;
        if (!Pressing) return false;

        var direction = Vector2.Normalize(push);
        if (!_armed && Vector2.Dot(direction, _bumpDirection) < TurnCosine) _armed = true;
        if (!_armed || _hasBump && now - _lastBump < RadarSettings.OpeningIntervalSeconds) return false;
        _armed = false;
        _hasBump = true;
        _bumpDirection = direction;
        _lastBump = now;
        return true;
    }

    /// <summary>The stick was released or the poll interrupted: the next push into a wall bumps again.</summary>
    public void Release()
    {
        _hasAnchor = false;
        _armed = true;
        Stalled = Pressing = false;
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
