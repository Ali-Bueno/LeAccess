using System;
using System.Collections.Generic;
using System.Numerics;
using LeAccess.Accessibility.Navigation.Guidance;

namespace LeAccess.Accessibility.Exploration;

internal readonly record struct ExplorationTarget(string Key, string Name, Vector3 Position, float ArrivalRadius);

internal enum BeaconLock { Locked, Retargeted, Cancelled, Refused, Invalid }

internal enum BeaconRelease { None, Arrived, RouteLost, TargetLost }

internal delegate bool BeaconRouter(Vector3 from, Vector3 target, float arrivalRadius,
    out IReadOnlyList<Vector3> path, out float length);

internal readonly record struct BeaconUpdate(bool Ping, float Pan, float Ahead, bool Routed, float Remaining,
    float Distance, BeaconRelease Release, ExplorationTarget Target);

// The D2R beacon tracker (d2r-access src/beacon.h), timed in seconds and measured on the XZ plane.
internal sealed class BeaconState
{
    // D2R Beacon::Settings in Last Epoch world units; navigation tuning, not game constants.
    internal const double RouteRefreshSeconds = 0.4;
    internal const float RerouteDistance = 2f;
    internal const float LookAhead = 4f;
    internal const double NearPingSeconds = 0.16;
    internal const double FarPingSeconds = 1.2;
    internal const float FarDistance = 30f;
    internal const double LockDelaySeconds = 0.6;
    internal const double RouteLossSeconds = 3;
    internal const float MinimumArrival = 1.5f;

    private ExplorationTarget _target;
    private bool _locked, _routed, _stale;
    private IReadOnlyList<Vector3> _path = Array.Empty<Vector3>();
    private Vector3 _routeTarget;
    private double _routeTime, _nextPing;
    private double? _lostSince;

    internal string LockedKey => _locked ? _target.Key : null;
    internal ExplorationTarget Target => _target;
    internal bool Routed => _routed;
    internal float RouteLength { get; private set; }

    internal static float ArrivalRadius(ExplorationTarget target) =>
        float.IsFinite(target.ArrivalRadius) && target.ArrivalRadius > MinimumArrival ? target.ArrivalRadius : MinimumArrival;

    internal static double PingInterval(float remaining)
    {
        double part = float.IsFinite(remaining) ? Math.Clamp(remaining / FarDistance, 0, 1) : 1;
        return NearPingSeconds + part * (FarPingSeconds - NearPingSeconds);
    }

    // The same key again cancels; another key retargets only when it has a route, keeping the old lock otherwise.
    internal BeaconLock Lock(ExplorationTarget target, Vector3 player, double now, BeaconRouter router)
    {
        if (string.IsNullOrEmpty(target.Key) || !GuidanceGeometry.Finite(target.Position)) return BeaconLock.Invalid;
        if (_locked && target.Key == _target.Key)
        {
            Release();
            return BeaconLock.Cancelled;
        }
        if (router is null || !GuidanceGeometry.Finite(player) ||
            !router(player, target.Position, ArrivalRadius(target), out var path, out float length) ||
            path is null || path.Count == 0) return BeaconLock.Refused;
        bool retarget = _locked;
        _target = target;
        _locked = true;
        Accept(path, length, now);
        _nextPing = now + LockDelaySeconds;
        return retarget ? BeaconLock.Retargeted : BeaconLock.Locked;
    }

    internal bool Cancel()
    {
        bool locked = _locked;
        Release();
        return locked;
    }

    internal void Release()
    {
        _locked = _routed = _stale = false;
        _target = default;
        _path = Array.Empty<Vector3>();
        RouteLength = 0;
        _lostSince = null;
    }

    // Menus, dialogue and focus loss keep the lock; time spent there does not count as a lost route.
    internal void Suspend()
    {
        _lostSince = null;
        _stale = true;
    }

    // current is the locked target's latest state; null when it no longer exists.
    internal BeaconUpdate Tick(Vector3 player, ExplorationTarget? current, Vector3 forward, Vector3 right, double now,
        BeaconRouter router)
    {
        if (!_locked || !GuidanceGeometry.Finite(player)) return default;
        if (current is not { } latest || latest.Key != _target.Key) return End(BeaconRelease.TargetLost);
        if (GuidanceGeometry.Finite(latest.Position)) _target = latest;
        else _target = latest with { Position = _target.Position };
        float distance = ExploredMap.Horizontal(player, _target.Position);
        if (distance <= ArrivalRadius(_target)) return End(BeaconRelease.Arrived);

        bool due = _stale || now < _routeTime || now - _routeTime >= RouteRefreshSeconds || _routed &&
            (OffPath(player) > RerouteDistance || ExploredMap.Horizontal(_target.Position, _routeTarget) > RerouteDistance);
        if (due)
        {
            _stale = false;
            if (router is not null && router(player, _target.Position, ArrivalRadius(_target), out var path, out float length) &&
                path is not null && path.Count > 0) Accept(path, length, now);
            else
            {
                _routed = false;
                _path = Array.Empty<Vector3>();
                _routeTime = now;
            }
        }
        if (!_routed)
        {
            _lostSince ??= now;
            if (now - _lostSince.Value >= RouteLossSeconds) return End(BeaconRelease.RouteLost);
            return new BeaconUpdate(false, 0, 0, false, distance, distance, BeaconRelease.None, _target);
        }
        _lostSince = null;
        float remaining = Progress(player, out var aim);
        GuidanceGeometry.Bearing(aim - player, forward, right, out float pan, out float ahead);
        bool ping = now >= _nextPing;
        if (ping || _nextPing - now > FarPingSeconds) _nextPing = now + PingInterval(remaining);
        return new BeaconUpdate(ping, pan, ahead, true, remaining, distance, BeaconRelease.None, _target);
    }

    private void Accept(IReadOnlyList<Vector3> path, float length, double now)
    {
        _path = path;
        _routed = true;
        RouteLength = float.IsFinite(length) ? length : 0;
        _routeTarget = _target.Position;
        _routeTime = now;
        _lostSince = null;
    }

    private BeaconUpdate End(BeaconRelease release)
    {
        var target = _target;
        Release();
        return new BeaconUpdate(false, 0, 0, false, 0, 0, release, target);
    }

    private float OffPath(Vector3 player)
    {
        Nearest(player, out _, out _, out float offset);
        return offset;
    }

    // Remaining route from the player's nearest point on the path; aims LookAhead along it to turn corners early.
    private float Progress(Vector3 player, out Vector3 aim)
    {
        Nearest(player, out int segment, out var point, out float offset);
        float remaining = offset, travelled = 0;
        aim = _path[^1];
        bool aimed = false;
        var previous = point;
        for (int index = segment + 1; index < _path.Count; index++)
        {
            float step = ExploredMap.Horizontal(previous, _path[index]);
            if (!aimed && travelled + step >= LookAhead)
            {
                float part = step > 0 ? (LookAhead - travelled) / step : 0;
                aim = Vector3.Lerp(previous, _path[index], part);
                aimed = true;
            }
            travelled += step;
            remaining += step;
            previous = _path[index];
        }
        if (ExploredMap.Horizontal(aim, player) <= 0) aim = _target.Position;
        return remaining;
    }

    private void Nearest(Vector3 player, out int segment, out Vector3 point, out float offset)
    {
        segment = 0;
        point = _path[0];
        offset = ExploredMap.Horizontal(player, point);
        for (int index = 0; index + 1 < _path.Count; index++)
        {
            var a = _path[index];
            var b = _path[index + 1];
            float x = b.X - a.X, z = b.Z - a.Z, squared = x * x + z * z;
            float t = squared > 0 ? Math.Clamp(((player.X - a.X) * x + (player.Z - a.Z) * z) / squared, 0, 1) : 0;
            var candidate = Vector3.Lerp(a, b, t);
            float distance = ExploredMap.Horizontal(player, candidate);
            if (distance >= offset) continue;
            offset = distance;
            point = candidate;
            segment = index;
        }
    }
}
