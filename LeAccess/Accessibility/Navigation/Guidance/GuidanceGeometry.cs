using System;
using System.Collections.Generic;
using System.Numerics;

namespace LeAccess.Accessibility.Navigation.Guidance;

internal static class GuidanceGeometry
{
    private static readonly string[] Directions =
        { "up", "up_right", "right", "down_right", "down", "down_left", "left", "up_left" };

    internal static bool Finite(Vector3 point) => float.IsFinite(point.X) &&
        float.IsFinite(point.Y) && float.IsFinite(point.Z);

    internal static string Direction(Vector3 delta, Vector3 forward, Vector3 right)
    {
        delta.Y = forward.Y = right.Y = 0;
        if (!Finite(delta) || !Finite(forward) || !Finite(right) || delta.LengthSquared() <= 0 ||
            forward.LengthSquared() <= 0 || right.LengthSquared() <= 0) return null;
        double angle = Math.Atan2(Vector3.Dot(delta, Vector3.Normalize(right)),
            Vector3.Dot(delta, Vector3.Normalize(forward)));
        double sector = 2 * Math.PI / Directions.Length;
        int index = (int)Math.Floor(angle / sector + 0.5);
        return "guidance.direction." + Directions[(index + Directions.Length) % Directions.Length];
    }

    // Horizontal bearing as stereo pan (screen right) and forward (screen up); zero when undefined.
    internal static void Bearing(Vector3 delta, Vector3 forward, Vector3 right, out float pan, out float ahead)
    {
        pan = ahead = 0;
        delta.Y = forward.Y = right.Y = 0;
        if (!Finite(delta) || !Finite(forward) || !Finite(right) || delta.LengthSquared() <= 0 ||
            forward.LengthSquared() <= 0 || right.LengthSquared() <= 0) return;
        var bearing = Vector3.Normalize(delta);
        pan = Math.Clamp(Vector3.Dot(bearing, Vector3.Normalize(right)), -1, 1);
        ahead = Math.Clamp(Vector3.Dot(bearing, Vector3.Normalize(forward)), -1, 1);
    }

    internal static bool TryRoute(IReadOnlyList<Vector3> corners, Vector3 origin, Vector3 destination,
        float tolerance, out float distance, out Vector3 next)
    {
        distance = 0;
        next = default;
        if (corners is null || corners.Count < 2 || !Finite(origin) || !Finite(destination) ||
            !float.IsFinite(tolerance) || tolerance <= 0) return false;
        if (!Finite(corners[0]) || !Finite(corners[^1]) ||
            Vector3.Distance(origin, corners[0]) > tolerance ||
            Vector3.Distance(destination, corners[^1]) > tolerance) return false;
        bool found = false;
        for (int i = 1; i < corners.Count; i++)
        {
            if (!Finite(corners[i])) return false;
            distance += Vector3.Distance(corners[i - 1], corners[i]);
            if (!found && Vector3.Distance(origin, corners[i]) > 0)
            { next = corners[i]; found = true; }
        }
        if (!found) next = destination;
        return float.IsFinite(distance);
    }
}

internal sealed class GuidanceProgress
{
    private long _session;
    private int _target;
    private bool _route, _known;
    private float _distance;
    private Vector3 _destination;

    internal string Observe(long session, int target, bool route, float distance, Vector3 destination, float bodyWidth,
        bool acknowledge = true)
    {
        if (!float.IsFinite(distance) || distance < 0 || !GuidanceGeometry.Finite(destination) ||
            !float.IsFinite(bodyWidth) || bodyWidth <= 0) { Reset(); return null; }
        bool comparable = _known && _session == session && _target == target && _route == route &&
            Vector3.Distance(destination, _destination) <= bodyWidth;
        string change = comparable && Math.Abs(distance - _distance) >= bodyWidth
            ? distance < _distance ? "guidance.closer" : "guidance.farther" : null;
        if (!acknowledge) return change;
        if (!comparable || change != null)
        {
            _distance = distance;
            _destination = destination;
        }
        _session = session;
        _target = target;
        _route = route;
        _known = true;
        return change;
    }

    internal void Reset() { _known = false; }
}
