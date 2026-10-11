using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Navigation.Guidance;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using NVector3 = System.Numerics.Vector3;

namespace LeAccess.Accessibility.Exploration;

// The D2R beacon (d2r-access docs/beacon.md): a locked target is followed by a ping routed only
// through ground the player has explored. See reference/exploration-mode.md.
internal static class ExplorationBeacon
{
    private const string ConfigSection = "Exploration beacon";
    // The reactive radar's polling; finer than the shortest ping interval.
    private const double TickSeconds = RadarSettings.PollSeconds;
    private const double StatusSeconds = 5;
    private const int DefaultAudioBufferMilliseconds = 100;
    private const int MinimumAudioBufferMilliseconds = 50;
    private const int MaximumAudioBufferMilliseconds = 300;
    private const float MinimumExploreRadius = 4f;
    private const float MaximumExploreRadius = 30f;
    private static ConfigEntry<int> _volume;
    private static ConfigEntry<float> _exploreRadius;
    private static ConfigEntry<int> _audioBuffer;
    private static readonly NavMeshWalkability Walkability = new();
    private static readonly ExploredMemory Memory = new(() => new ExploredMap(ExploredMap.DefaultCellSize, Walkability.LevelHeight));
    private static readonly BeaconState State = new() { AtHand = TargetAtHand };
    private static readonly BeaconRouter Router = RouteExplored;
    private static ExploredMap _map;
    private static Camera _camera;
    private static BeaconUpdate _last;
    private static long _lockSession, _mapSession;
    private static bool _active;
    private static double _nextTick, _nextStatus;
    private static int _refusals, _routeLosses;
    private static string _lastFailure, _heldLog, _routeLog;

    internal static string LockedKey => State.LockedKey;
    internal static float ExploreRadius => _exploreRadius?.Value ?? ExploredMap.DefaultExploreRadius;

    internal static void Initialize(ConfigFile config)
    {
        _volume = config.Bind(ConfigSection, "Volume", 100,
            new ConfigDescription("Percent of the Diablo 2 beacon volume.", new AcceptableValueRange<int>(0, 100)));
        _exploreRadius = config.Bind(ConfigSection, "ExploreRadius", ExploredMap.DefaultExploreRadius,
            new ConfigDescription("Ground within this distance (world units) and in walkable line of sight counts as explored. The beacon routes only through explored ground.",
                new AcceptableValueRange<float>(MinimumExploreRadius, MaximumExploreRadius)));
        _audioBuffer = config.Bind(ConfigSection, "AudioBufferMilliseconds", DefaultAudioBufferMilliseconds,
            new ConfigDescription("Audio buffer duration. Larger values give playback more tolerance for scheduling delays. Restart the game after changing this.",
                new AcceptableValueRange<int>(MinimumAudioBufferMilliseconds, MaximumAudioBufferMilliseconds)));
        TownExplored.Initialize(config, ConfigSection);
        Plugin.Log.LogInfo("Exploration beacon configured (routes through explored NavMesh ground)");
    }

    /// <summary>The baked agent height: one walkable floor.</summary>
    internal static float LevelHeight => Walkability.LevelHeight;

    // The current player's NavMesh queries, shared with the scanner's line-of-sight test.
    internal static bool TryGetWalkability(out IWalkability nav)
    {
        nav = null;
        try
        {
            if (!MapSectionWorld.TryCapturePlayer(out var world) ||
                !Walkability.TryConfigure(world, ExploredMap.DefaultCellSize)) return false;
            nav = Walkability;
            return true;
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    // Returns the sentence to speak: locked with its reach word, cancelled (same key) or refused.
    internal static string Lock(ExplorationTarget target)
    {
        if (_volume == null) return null;
        try
        {
            if (State.LockedKey != null && State.LockedKey == target.Key) return Cancel();
            if (!TryCapture(out var world, out var player)) return ModText.Get("exploration.beacon.unavailable");
            double now = Time.unscaledTimeAsDouble;
            _map.Observe(player, Walkability);
            string previous = State.Target.Name;
            _heldLog = null;
            switch (State.Lock(target, player, now, Router))
            {
                case BeaconLock.Refused:
                    _refusals++;
                    Plugin.Log.LogInfo($"[Exploration beacon] refused reason={_map.LastFailure ?? "none"} name={target.Name} cells={_map.CellCount} {GoalText(target)}");
                    return ModText.Format("exploration.beacon.refused", target.Name);
                case BeaconLock.Cancelled:
                    return Cancelled(target.Name);
                case BeaconLock.Invalid:
                    return ModText.Get("exploration.beacon.unavailable");
                case BeaconLock.Retargeted:
                    Released("retargeted", previous);
                    break;
            }
            _lockSession = world.Session;
            _nextStatus = now + StatusSeconds;
            BeaconAudio.Queue(BeaconSound.Locked);
            bool straight = ExploredMap.StraightClear(player, target.Position, Walkability);
            Plugin.Log.LogInfo(string.Create(CultureInfo.InvariantCulture,
                $"[Exploration beacon] locked key={target.Key} name={target.Name} straight={(straight ? 1 : 0)} route={State.RouteLength:F1} cells={_map.CellCount} {GoalText(target)}"));
            return ModText.Format(straight ? "exploration.beacon.locked_straight" : "exploration.beacon.locked_around", target.Name);
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
            return ModText.Get("exploration.beacon.unavailable");
        }
    }

    internal static string Cancel()
    {
        if (State.LockedKey == null) return null;
        string name = State.Target.Name;
        State.Cancel();
        return Cancelled(name);
    }

    // Every LateUpdate; inactive keeps the lock silently. Returns an arrival or lost sentence.
    internal static string Update(bool active, ExplorationTarget? target)
    {
        if (_volume == null) return null;
        try
        {
            double now = Time.unscaledTimeAsDouble;
            if (!active)
            {
                State.Suspend();
                if (_active) BeaconAudio.Silence();
                _active = false;
                return null;
            }
            _active = true;
            string speech = null;
            if (now >= _nextTick || _nextTick - now > TickSeconds)
            {
                _nextTick = now + TickSeconds;
                speech = Tick(now, target);
            }
            BeaconAudio.Play(now, _volume.Value, _audioBuffer.Value);
            if (State.LockedKey != null && now >= _nextStatus)
            {
                _nextStatus = now + StatusSeconds;
                LogStatus();
            }
            return speech;
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
            BeaconAudio.Silence();
            return null;
        }
    }

    // A short quiet tick when the Create menu opens; skipped while the output is busy.
    internal static void PlayMenuClick()
    {
        if (_volume == null) return;
        try { BeaconAudio.Click(Time.unscaledTimeAsDouble, _volume.Value, _audioBuffer.Value); }
        catch (Exception ex) { ReportFailure(ex); }
    }

    private static string Tick(double now, ExplorationTarget? target)
    {
        if (!TryCapture(out var world, out var player))
        {
            BeaconAudio.DropPing();
            return null;
        }
        if (State.LockedKey != null && world.Session != _lockSession)
        {
            Released("scene", State.Target.Name);
            State.Release();
            BeaconAudio.DropPing();
        }
        _map.Observe(player, Walkability);
        TownExplored.Update(world, _map, Walkability);
        if (!MinimapRoomReader.HasRooms) _map.UpdateFrontier(Walkability);
        if (State.LockedKey == null || !TryAxes(out var forward, out var right)) return null;
        _last = State.Tick(player, target, forward, right, now, Router);
        string name = _last.Target.Name;
        // An unexplored room leaves the scanner once entered (D2R arrives on entry, its room cue the sound);
        // a frontier edge once seen.
        bool unexplored = RoomTracker.IsUnexplored(_last.Target.Key);
        switch (_last.Release)
        {
            case BeaconRelease.Arrived:
            case BeaconRelease.TargetLost when unexplored:
                Released("arrived", name);
                if (!_last.Target.Key.StartsWith(RoomTracker.UnexploredKeyPrefix, StringComparison.Ordinal))
                    BeaconAudio.Queue(BeaconSound.Arrived);
                return ModText.Format("exploration.beacon.arrived", name);
            case BeaconRelease.RouteLost:
                _routeLosses++;
                Released($"route-lost failure={_map.LastFailure ?? "none"}", name);
                BeaconAudio.Queue(BeaconSound.Cleared);
                return ModText.Format("exploration.beacon.route_lost", name);
            case BeaconRelease.TargetLost:
                Released("target-lost", name);
                BeaconAudio.Queue(BeaconSound.Cleared);
                return ModText.Format("exploration.beacon.target_lost", name);
        }
        if (_last.Ping) BeaconAudio.QueuePing(_last.Pan, _last.Ahead);
        return null;
    }

    private static bool TryCapture(out MapSectionWorldSnapshot world, out NVector3 player)
    {
        player = default;
        if (!MapSectionWorld.TryCapturePlayer(out world) ||
            !Walkability.TryConfigure(world, ExploredMap.DefaultCellSize)) return false;
        player = new NVector3((float)world.X, (float)world.Y, (float)world.Z);
        if (_map == null || world.Session != _mapSession)
        {
            _map = Memory.Resolve(world.SceneName, world.Variant, world.Session, world.IsFixedLayout);
            _mapSession = world.Session;
        }
        _map.ExploreRadius = _exploreRadius.Value;
        return true;
    }

    private static bool TryAxes(out NVector3 forward, out NVector3 right)
    {
        forward = right = default;
        if (!IsAlive(_camera) || !_camera.isActiveAndEnabled) _camera = Camera.main;
        if (!IsAlive(_camera) || !_camera.isActiveAndEnabled) return false;
        forward = GuidanceRouteProbe.Convert(_camera.transform.forward);
        right = GuidanceRouteProbe.Convert(_camera.transform.right);
        forward.Y = right.Y = 0;
        return forward.LengthSquared() > 0 && right.LengthSquared() > 0;
    }

    private static bool RouteExplored(NVector3 from, ExplorationTarget target, out IReadOnlyList<NVector3> path, out float length)
    {
        path = Array.Empty<NVector3>();
        length = 0;
        return _map != null && _map.TryRoute(from, target.Position, BeaconState.ArrivalRadius(target), Walkability,
            out path, out length, target.Area);
    }

    // The scanner's route to a target through explored ground, measured and aimed as the beacon would; null when none.
    internal static ExplorationRoute? RouteFor(ExplorationTarget target, NVector3 player)
    {
        try
        {
            var route = BeaconState.Measure(Router, player, target);
            string found = route is { } known
                ? string.Create(CultureInfo.InvariantCulture, $"route={known.Length:F1} direct={(known.Direct ? 1 : 0)} aim={known.Aim.X:F1},{known.Aim.Z:F1}")
                : $"route=none failure={_map?.LastFailure ?? "no map"}";
            string line = string.Create(CultureInfo.InvariantCulture,
                $"[Exploration] route name={target.Name} straight={ExploredMap.Horizontal(player, target.Position):F1} {found} {GoalText(target)}");
            if (line != _routeLog) Plugin.Log.LogInfo(_routeLog = line);
            return route;
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
            return null;
        }
    }

    // Arrival needs the target at hand, not only its range: a wall or another level between them holds it.
    private static bool TargetAtHand(NVector3 player, ExplorationTarget target)
    {
        if (_map == null) return true;
        bool atHand = _map.AtHand(player, target.Position, BeaconState.ArrivalRadius(target), target.Area, Walkability,
            out string blocked);
        string state = $"{target.Key}|{blocked ?? "none"}";
        if (state == _heldLog) return atHand;
        _heldLog = state;
        Plugin.Log.LogInfo(string.Create(CultureInfo.InvariantCulture,
            $"[Exploration beacon] {(atHand ? "at hand" : "arrival held")} name={target.Name} blocked={blocked ?? "none"} " +
            $"distance={ExploredMap.Horizontal(player, target.Position):F1} {GoalText(target)}"));
        return atHand;
    }

    // The goal a route takes for this target and why: on the mesh, its walkable approach, an area, or unsampled.
    private static string GoalText(ExplorationTarget target)
    {
        if (_map == null) return "goal=none";
        var goal = _map.ResolveGoal(target.Position, BeaconState.ArrivalRadius(target), target.Area, Walkability);
        return string.Create(CultureInfo.InvariantCulture,
            $"goal={goal.Kind} goal_at={goal.Point.X:F1},{goal.Point.Y:F1},{goal.Point.Z:F1} " +
            $"goal_offset={ExploredMap.Horizontal(goal.Point, target.Position):F1} goal_dy={goal.Point.Y - target.Position.Y:F1}");
    }

    private static string Cancelled(string name)
    {
        Released("cancelled", name);
        BeaconAudio.Queue(BeaconSound.Cleared);
        return ModText.Get("exploration.beacon.cancelled");
    }

    private static void Released(string reason, string name) =>
        Plugin.Log.LogInfo($"[Exploration beacon] released reason={reason} name={name}");

    private static void LogStatus() => Plugin.Log.LogInfo(string.Create(CultureInfo.InvariantCulture,
        $"[Exploration beacon] status name={State.Target.Name} routed={(State.Routed ? 1 : 0)} route={State.RouteLength:F1} " +
        $"remaining={_last.Remaining:F1} distance={_last.Distance:F1} cells={_map?.CellCount ?? 0} fixed_maps={Memory.FixedCount} " +
        $"refusals={_refusals} route_losses={_routeLosses} failure={_map?.LastFailure ?? "none"} " +
        $"expansions={_map?.LastExpansions ?? 0} edge_checks={_map?.EdgeChecks ?? 0} " +
        $"unverified_floors={Walkability.UnverifiedFloors} pings={BeaconAudio.Pings} audio={BeaconAudio.Diagnostics}"));

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (failure == _lastFailure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Exploration beacon unavailable: {ex}");
    }

    internal static ExploredMemory ExploredGround => Memory;

    // The explored ground of this scene session, once the beacon has resolved it.
    internal static ExploredMap CurrentMap(long session) => _map != null && _mapSession == session ? _map : null;

    // Drops the cached map so the next tick resolves it again after the memory was cleared or imported.
    internal static void ForgetMap()
    {
        _map = null;
        _mapSession = 0;
    }

    internal static void Cleanup()
    {
        BeaconAudio.Cleanup();
        State.Release();
        Memory.Clear();
        Walkability.Reset();
        TownExplored.Cleanup();
        _map = null;
        _camera = null;
        _last = default;
        _active = false;
        _lockSession = _mapSession = 0;
        _nextTick = _nextStatus = 0;
        _refusals = _routeLosses = 0;
        _lastFailure = _heldLog = _routeLog = null;
        _volume = null;
        _exploreRadius = null;
        _audioBuffer = null;
    }
}
