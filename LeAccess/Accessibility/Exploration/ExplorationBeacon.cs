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
    private static readonly BeaconState State = new();
    private static readonly BeaconRouter Router = RouteExplored;
    private static ExploredMap _map;
    private static Camera _camera;
    private static BeaconUpdate _last;
    private static long _lockSession, _mapSession;
    private static bool _active;
    private static double _nextTick, _nextStatus;
    private static int _refusals, _routeLosses;
    private static string _lastFailure;

    internal static string LockedKey => State.LockedKey;

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
        Plugin.Log.LogInfo("Exploration beacon configured (routes through explored NavMesh ground)");
    }

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
            switch (State.Lock(target, player, now, Router))
            {
                case BeaconLock.Refused:
                    _refusals++;
                    Plugin.Log.LogInfo($"[Exploration beacon] refused reason={_map.LastFailure ?? "none"} name={target.Name} cells={_map.CellCount}");
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
                $"[Exploration beacon] locked key={target.Key} name={target.Name} straight={(straight ? 1 : 0)} route={State.RouteLength:F1} cells={_map.CellCount}"));
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
        if (State.LockedKey == null || !TryAxes(out var forward, out var right)) return null;
        _last = State.Tick(player, target, forward, right, now, Router);
        string name = _last.Target.Name;
        switch (_last.Release)
        {
            case BeaconRelease.Arrived:
                Released("arrived", name);
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

    private static bool RouteExplored(NVector3 from, NVector3 target, float arrivalRadius,
        out IReadOnlyList<NVector3> path, out float length)
    {
        path = Array.Empty<NVector3>();
        length = 0;
        return _map != null && _map.TryRoute(from, target, arrivalRadius, Walkability, out path, out length);
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
        _map = null;
        _camera = null;
        _last = default;
        _active = false;
        _lockSession = _mapSession = 0;
        _nextTick = _nextStatus = 0;
        _refusals = _routeLosses = 0;
        _lastFailure = null;
        _volume = null;
        _exploreRadius = null;
        _audioBuffer = null;
    }
}
