using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using LeAccess.Accessibility.MapSections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using NVector3 = System.Numerics.Vector3;

namespace LeAccess.Accessibility.Exploration;

// The active scene's minimap persistence volumes (MinimapVolumesManager.volumes, one SphereCollider each)
// with the game's revealed statuses. Geometry is read once per scene session, statuses on every read.
internal static class MinimapRoomReader
{
    // Scenes without a manager (procedural maps, some campaign zones) are searched again at this pace in
    // case it loads late; the search walks the whole scene, so it never runs per frame.
    private const double SearchSeconds = 5;
    private static readonly List<(MinimapVolume Volume, NVector3 Centre, float Radius)> Volumes = new();
    private static MinimapVolumesManager _manager;
    private static long _session;
    private static double _nextSearch;

    internal static string Layout { get; private set; }
    // Rooms of the current scene session; until found, the frontier stands in for them.
    internal static bool HasRooms => Volumes.Count > 0 && IsAlive(_manager);

    internal static bool TryRead(MapSectionWorldSnapshot world, double now, List<MinimapRoom> rooms)
    {
        rooms.Clear();
        if (world.Session != _session)
        {
            Reset();
            _session = world.Session;
        }
        if (!IsAlive(_manager))
        {
            _manager = null;
            Volumes.Clear();
            if (now < _nextSearch) return false;
            _nextSearch = now + SearchSeconds;
            var found = FindInScene(world.SceneHandle);
            if (!IsAlive(found) || !Capture(found)) return false;
            _manager = found;
        }
        var statuses = _manager.Statuses;
        for (int index = 0; index < Volumes.Count; index++)
        {
            var (volume, centre, radius) = Volumes[index];
            bool revealed = statuses is not null && index < statuses.Length && statuses[index];
            if (!revealed && IsAlive(volume)) revealed = volume.hasBeenTriggered;
            rooms.Add(new MinimapRoom(index, centre, radius, revealed));
        }
        return rooms.Count > 0;
    }

    // Only the active scene's manager: a generated cave keeps its parent scene loaded, rooms and all.
    private static MinimapVolumesManager FindInScene(int sceneHandle)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || (int)scene.handle != sceneHandle) return null;
        var roots = scene.GetRootGameObjects();
        if (roots is null || roots.WasCollected) return null;
        foreach (var root in roots)
        {
            // The non-generic overload avoids depending on a generic instantiation in the game binary.
            var found = IsAlive(root)
                ? root.GetComponentInChildren(Il2CppType.Of<MinimapVolumesManager>(), false)?.TryCast<MinimapVolumesManager>()
                : null;
            if (IsAlive(found)) return found;
        }
        return null;
    }

    // Keeps the manager's order, which the game's statuses follow; an unreadable volume stays as a gap.
    private static bool Capture(MinimapVolumesManager manager)
    {
        Volumes.Clear();
        var volumes = manager.volumes;
        if (volumes is null || volumes.Length == 0) return false;
        for (int index = 0; index < volumes.Length; index++)
        {
            var volume = volumes[index];
            NVector3 centre = default;
            float radius = 0;
            var sphere = IsAlive(volume) ? volume.col : null;
            if (IsAlive(sphere))
            {
                var transform = sphere.transform;
                var point = transform.TransformPoint(sphere.center);
                var scale = transform.lossyScale;
                centre = new NVector3(point.x, point.y, point.z);
                radius = sphere.radius * Math.Max(Math.Abs(scale.x), Math.Max(Math.Abs(scale.y), Math.Abs(scale.z)));
            }
            Volumes.Add((volume, centre, radius));
        }
        Layout = $"{manager.version}.{volumes.Length}";
        return true;
    }

    internal static void Reset()
    {
        Volumes.Clear();
        _manager = null;
        _session = 0;
        _nextSearch = 0;
        Layout = null;
    }
}
