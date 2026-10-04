using System;
using BepInEx.Configuration;
using EHG.Multiplayer;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Navigation.Guidance;
using LeAccess.Accessibility.WorldCues;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using NVector3 = System.Numerics.Vector3;

namespace LeAccess.Accessibility.Exploration;

// D2R exploration feedback beside the scanner (docs/exploration.md): "the radar has logged a new exit/place"
// with the discovery sound, the unexplored-room loop and entry cue, and the last death place.
internal static class ExplorationFeedback
{
    private const string ConfigSection = "Exploration";
    // The room loop follows the player as smoothly as the radar polls.
    private const double TickSeconds = RadarSettings.PollSeconds;
    private static ConfigEntry<bool> _roomSounds, _discoveries;
    private static Camera _camera;
    private static NVector3 _lastAlive;
    private static bool _hasAlive, _dead, _sounding;
    private static double _nextTick;
    private static string _lastFailure;

    internal static void Initialize(ConfigFile config)
    {
        _roomSounds = config.Bind(ConfigSection, "RoomSounds", true,
            "A loop at the nearest minimap room not yet entered and a cue on first entering one (Diablo 2 mod sounds).");
        _discoveries = config.Bind(ConfigSection, "Discoveries", true,
            "Say \"the radar has logged a new exit/place\" with a sound when an exit, waypoint or the stash is first seen.");
    }

    // After each scanner refresh: what it logged and the rooms just entered.
    internal static void Refreshed()
    {
        if (_discoveries == null) return;
        bool found = false;
        if (_discoveries.Value)
            foreach (var find in ExplorationProbe.NewFinds)
            {
                string key = find.Category == ExplorationCategory.Exit ? "exploration.found_exit" : "exploration.found_place";
                string text = ModText.Format(key, find.Name);
                if (ScreenReader.TrySpeak(text, false)) Plugin.Log.LogInfo($"[Exploration] {text}");
                found = true;
            }
        if (found) ExplorationSounds.Queue(NaturalCueKind.Discovery);
        if (_roomSounds.Value && ExplorationProbe.EnteredRooms.Count > 0)
        {
            ExplorationSounds.Queue(NaturalCueKind.InsideUnexploredRoom);
            foreach (var room in ExplorationProbe.EnteredRooms)
                Plugin.Log.LogInfo($"[Exploration] entered unexplored room {room.Index + 1}");
        }
    }

    internal static void Update(bool gameplay, double now)
    {
        if (_roomSounds == null || now < _nextTick && _nextTick - now <= TickSeconds) return;
        _nextTick = now + TickSeconds;
        try
        {
            TrackDeath();
            if (!gameplay || !MapSectionWorld.TryCapturePlayer(out var world) || !TryAxes(out var forward, out var right))
            {
                Silence();
                return;
            }
            var player = new NVector3((float)world.X, (float)world.Y, (float)world.Z);
            _lastAlive = player;
            _hasAlive = true;
            _sounding = true;
            ExplorationSounds.Update(now, _roomSounds.Value ? ExplorationProbe.UnvisitedRooms : null, player, forward, right,
                WorldCueSounds.LoopRange, WorldCueSounds.Volume);
        }
        catch (Exception ex)
        {
            Silence();
            if (ex.Message == _lastFailure) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"Exploration feedback unavailable: {ex.Message}");
        }
    }

    internal static void Silence()
    {
        if (!_sounding) return;
        _sounding = false;
        ExplorationSounds.Silence();
    }

    // The place of the last death, where the player last stood alive, kept with that scene's seen places.
    private static void TrackDeath()
    {
        bool dead = PlayerDead();
        if (dead && !_dead && _hasAlive)
        {
            ExplorationProbe.RecordDeath(_lastAlive);
            Plugin.Log.LogInfo(FormattableString.Invariant($"[Exploration] death place {_lastAlive.X:F1},{_lastAlive.Z:F1}"));
        }
        _dead = dead;
        if (dead) _hasAlive = false;
    }

    // The same checks MapSectionWorld makes before accepting the player.
    private static bool PlayerDead()
    {
        if (GameplayEnvironment.IsOnlineClient)
        {
            var visuals = PlayerFinder.playerActorVisuals;
            return IsAlive(visuals) && visuals.dead;
        }
        var actor = PlayerFinder.playerActorComponent;
        return IsAlive(actor) && actor.isDying();
    }

    private static bool TryAxes(out NVector3 forward, out NVector3 right)
    {
        forward = right = default;
        if (!IsAlive(_camera) || !_camera.isActiveAndEnabled) _camera = Camera.main;
        if (!IsAlive(_camera) || !_camera.isActiveAndEnabled) return false;
        forward = GuidanceRouteProbe.Convert(_camera.transform.forward);
        right = GuidanceRouteProbe.Convert(_camera.transform.right);
        forward.Y = right.Y = 0;
        if (forward.LengthSquared() <= 0 || right.LengthSquared() <= 0) return false;
        forward = NVector3.Normalize(forward);
        right = NVector3.Normalize(right);
        return true;
    }

    internal static void Cleanup()
    {
        ExplorationSounds.Cleanup();
        _roomSounds = _discoveries = null;
        _camera = null;
        _hasAlive = _dead = _sounding = false;
        _nextTick = 0;
        _lastFailure = null;
    }
}
