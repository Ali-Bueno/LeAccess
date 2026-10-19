using System;
using System.Collections.Generic;
using System.Numerics;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Navigation.Guidance;

namespace LeAccess.Accessibility.Exploration;

internal readonly record struct ExplorationTarget(string Key, string Name, Vector3 Position, float ArrivalRadius,
    bool Area = false)
{
    // Minimap rooms and frontier edges are areas: entered, not walked up to.
    internal static ExplorationTarget From(ExplorationEntry entry) => new(entry.Key, entry.Name, entry.Position,
        entry.ArrivalRadius, entry.Category is ExplorationCategory.Room or ExplorationCategory.UnexploredRoom);
}

internal enum BeaconLock { Locked, Retargeted, Cancelled, Refused, Invalid }

internal enum BeaconRelease { None, Arrived, RouteLost, TargetLost }

internal delegate bool BeaconRouter(Vector3 from, ExplorationTarget target,
    out IReadOnlyList<Vector3> path, out float length);

// A way the beacon follows toward a target with no explored route: the explored route to an opening.
internal readonly record struct BeaconWay(ExplorationTarget Opening, IReadOnlyList<Vector3> Path, float Length, float Beyond)
{
    internal float Estimate => ExploredMap.WayEstimate(Length, Beyond);
}

// Ways toward a target with no explored route, best first.
internal delegate IReadOnlyList<BeaconWay> BeaconWays(Vector3 from, ExplorationTarget target);

internal enum BeaconNews { None, RouteKnown, DeadEnd }

internal readonly record struct BeaconUpdate(bool Ping, float Pan, float Ahead, bool Routed, float Remaining,
    float Distance, BeaconRelease Release, ExplorationTarget Target, BeaconNews News = BeaconNews.None,
    ExplorationTarget Way = default);

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
    // A way is kept until another promises at least two explored cells less, so near-equal ways do not trade places.
    internal const float WaySwitchMargin = 2 * ExploredMap.DefaultCellSize;
    // A short list to choose from; past it the openings lead well away from the target.
    internal const int MaxWays = 5;

    private ExplorationTarget _target;
    private bool _locked, _routed, _stale;
    private IReadOnlyList<Vector3> _path = Array.Empty<Vector3>();
    private Vector3 _routeTarget;
    private double _routeTime, _nextPing;
    private double? _lostSince;
    private BeaconWay? _way;
    private ExplorationTarget _lastOpening;
    // The openings listed when the current way was chosen: going back to one of them means a dead end.
    private readonly HashSet<string> _earlier = new(StringComparer.Ordinal);
    private double? _wayGone;
    private bool _knownSaid;

    internal string LockedKey => _locked ? _target.Key : null;
    internal ExplorationTarget Target => _target;
    internal bool Routed => _routed;
    internal float RouteLength { get; private set; }
    internal BeaconWay? Way => _locked ? _way : null;

    /// <summary>How long a way just entered keeps the beacon before the next is chosen: the map's time to take in new ground.</summary>
    internal double WaySettleSeconds { get; set; }

    /// <summary>Whether the target is at hand from the player within its range; null lets the range alone decide.</summary>
    internal Func<Vector3, ExplorationTarget, bool> AtHand { get; set; }

    internal static float ArrivalRadius(ExplorationTarget target) =>
        float.IsFinite(target.ArrivalRadius) && target.ArrivalRadius > MinimumArrival ? target.ArrivalRadius : MinimumArrival;

    internal static double PingInterval(float remaining, float speed = 1)
    {
        double part = float.IsFinite(remaining) ? Math.Clamp(remaining / FarDistance, 0, 1) : 1;
        return (NearPingSeconds + part * (FarPingSeconds - NearPingSeconds)) / speed;
    }

    // The same key again cancels; another key retargets only when it has a route or a way, keeping the old lock
    // otherwise. Without an explored route the preferred way is taken when listed, else the best.
    internal BeaconLock Lock(ExplorationTarget target, Vector3 player, double now, BeaconRouter router,
        BeaconWays ways = null, string preferred = null)
    {
        if (string.IsNullOrEmpty(target.Key) || !GuidanceGeometry.Finite(target.Position)) return BeaconLock.Invalid;
        if (_locked && target.Key == _target.Key)
        {
            Release();
            return BeaconLock.Cancelled;
        }
        if (router is null || !GuidanceGeometry.Finite(player)) return BeaconLock.Refused;
        bool routed = router(player, target, out var path, out float length) && path is { Count: > 0 };
        var listed = routed ? null : ways?.Invoke(player, target);
        if (!routed && listed is not { Count: > 0 }) return BeaconLock.Refused;
        bool retarget = _locked;
        _target = target;
        _locked = true;
        ResetWay();
        if (routed) Accept(path, length, now);
        else
        {
            var way = Choose(listed, Find(listed, preferred) ?? listed[0]);
            Accept(way.Path, way.Length, now);
        }
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
        ResetWay();
    }

    private void ResetWay()
    {
        _way = null;
        _lastOpening = default;
        _earlier.Clear();
        _wayGone = null;
        _knownSaid = false;
    }

    // Menus, dialogue and focus loss keep the lock; time spent there does not count as a lost route.
    internal void Suspend()
    {
        _lostSince = null;
        _stale = true;
    }

    // current is the locked target's latest state; null when it no longer exists.
    internal BeaconUpdate Tick(Vector3 player, ExplorationTarget? current, Vector3 forward, Vector3 right, double now,
        BeaconRouter router, BeaconWays ways = null)
    {
        if (!_locked || !GuidanceGeometry.Finite(player)) return default;
        if (current is not { } latest || latest.Key != _target.Key) return End(BeaconRelease.TargetLost);
        if (GuidanceGeometry.Finite(latest.Position)) _target = latest;
        else _target = latest with { Position = _target.Position };
        float distance = ExploredMap.Horizontal(player, _target.Position);
        if (distance <= ArrivalRadius(_target) && (AtHand is null || AtHand(player, _target))) return End(BeaconRelease.Arrived);

        bool due = _stale || now < _routeTime || now - _routeTime >= RouteRefreshSeconds || _routed &&
            (OffPath(player) > RerouteDistance || ExploredMap.Horizontal(_target.Position, _routeTarget) > RerouteDistance);
        var news = BeaconNews.None;
        if (due)
        {
            _stale = false;
            if (router is not null && router(player, _target, out var path, out float length) &&
                path is not null && path.Count > 0)
            {
                if (_lastOpening.Key != null && !_knownSaid)
                {
                    news = BeaconNews.RouteKnown;
                    _knownSaid = true;
                }
                _way = null;
                _lastOpening = default;
                _wayGone = null;
                Accept(path, length, now);
            }
            else if (FollowWay(ways?.Invoke(player, _target), now, ref news) is { } way) Accept(way.Path, way.Length, now);
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
            return new BeaconUpdate(false, 0, 0, false, distance, distance, BeaconRelease.None, _target, news, _lastOpening);
        }
        _lostSince = null;
        // A way's pace and aim follow the opening it leads to; the target lies beyond it.
        float remaining = Follow(_path, player, _way?.Opening.Position ?? _target.Position, out var aim);
        GuidanceGeometry.Bearing(aim - player, forward, right, out float pan, out float ahead);
        bool ping = now >= _nextPing;
        float speed = SoundLevels.RepeatSpeed(RepeatGroup.Beacon);
        if (ping || _nextPing - now > FarPingSeconds / speed) _nextPing = now + PingInterval(remaining, speed);
        return new BeaconUpdate(ping, pan, ahead, true, remaining, distance, BeaconRelease.None, _target, news,
            _way?.Opening ?? default);
    }

    // The current way is kept while listed unless another promises clearly less. Once it is gone (entered or seen),
    // the beacon keeps leading into it while the map takes in the new ground, then takes the best way; that being
    // one listed before the gone way was chosen means it led nowhere new.
    private BeaconWay? FollowWay(IReadOnlyList<BeaconWay> listed, double now, ref BeaconNews news)
    {
        listed ??= Array.Empty<BeaconWay>();
        if (_way is { } current)
        {
            if (Find(listed, current.Opening.Key) is { } still)
            {
                _wayGone = null;
                if (listed[0].Estimate + WaySwitchMargin < still.Estimate) return Choose(listed, listed[0]);
                return _way = still;
            }
            _wayGone ??= now;
            if (now - _wayGone.Value < WaySettleSeconds) return current;
            _wayGone = null;
            _way = null;
            if (listed.Count == 0) return null;
            if (_earlier.Contains(listed[0].Opening.Key)) news = BeaconNews.DeadEnd;
        }
        return listed.Count > 0 ? Choose(listed, listed[0]) : null;
    }

    private BeaconWay Choose(IReadOnlyList<BeaconWay> listed, BeaconWay way)
    {
        _earlier.Clear();
        foreach (var other in listed) _earlier.Add(other.Opening.Key);
        _way = way;
        _lastOpening = way.Opening;
        return way;
    }

    private static BeaconWay? Find(IReadOnlyList<BeaconWay> listed, string key)
    {
        if (key == null) return null;
        foreach (var way in listed)
            if (way.Opening.Key == key) return way;
        return null;
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
        var opening = _lastOpening;
        Release();
        return new BeaconUpdate(false, 0, 0, false, 0, 0, release, target, Way: opening);
    }

    private float OffPath(Vector3 player)
    {
        Nearest(_path, player, out _, out _, out float offset);
        return offset;
    }

    // The scanner's reading of a route, measured and aimed as Tick does, with corners when asked; null when there is none.
    internal static ExplorationRoute? Measure(BeaconRouter router, Vector3 player, ExplorationTarget target,
        Func<IReadOnlyList<Vector3>, IReadOnlyList<Vector3>> corners = null) =>
        router is not null && router(player, target, out var path, out _) && path is { Count: > 0 }
            ? new ExplorationRoute(Follow(path, player, target.Position, out var aim), aim, path.Count == 2, corners?.Invoke(path))
            : null;

    // Remaining route from the player's nearest point on the path; aims LookAhead along it to turn corners early.
    internal static float Follow(IReadOnlyList<Vector3> path, Vector3 player, Vector3 target, out Vector3 aim)
    {
        aim = target;
        if (path is null || path.Count == 0) return ExploredMap.Horizontal(player, target);
        Nearest(path, player, out int segment, out var point, out float offset);
        float remaining = offset, travelled = 0;
        aim = path[^1];
        bool aimed = false;
        var previous = point;
        for (int index = segment + 1; index < path.Count; index++)
        {
            float step = ExploredMap.Horizontal(previous, path[index]);
            if (!aimed && travelled + step >= LookAhead)
            {
                float part = step > 0 ? (LookAhead - travelled) / step : 0;
                aim = Vector3.Lerp(previous, path[index], part);
                aimed = true;
            }
            travelled += step;
            remaining += step;
            previous = path[index];
        }
        if (ExploredMap.Horizontal(aim, player) <= 0) aim = target;
        return remaining;
    }

    private static void Nearest(IReadOnlyList<Vector3> path, Vector3 player, out int segment, out Vector3 point, out float offset)
    {
        segment = 0;
        point = path[0];
        offset = ExploredMap.Horizontal(player, point);
        for (int index = 0; index + 1 < path.Count; index++)
        {
            var a = path[index];
            var b = path[index + 1];
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
