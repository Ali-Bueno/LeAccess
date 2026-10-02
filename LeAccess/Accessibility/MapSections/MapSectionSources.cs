using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.MapSections;

internal static class MapSectionSources
{
    // Match the map anchor retry interval for asynchronously loaded scene roots.
    private const double RetrySeconds = 1;
    private static readonly Dictionary<MapSection, Entry> Cache = new();
    private static int _scene;
    private static int? _variant;
    private static string _lastFailure;

    internal static bool IsAvailable(MapSection section, MapSectionWorldSnapshot world)
    {
        if (section.Source == null) return true;
        try
        {
            if (_scene != world.SceneHandle || _variant != world.Variant)
            { Reset(); _scene = world.SceneHandle; _variant = world.Variant; }
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || (int)scene.handle != world.SceneHandle ||
                scene.name != world.SceneName || scene.path != world.ScenePath) return false;
            if (!Cache.TryGetValue(section, out var entry)) Cache[section] = entry = new Entry();
            if (entry.Target is not null)
            {
                if (Matches(entry.Target, section.Source, world.SceneHandle) &&
                    CollidersMatch(entry.Boxes, section.Source, world.SceneHandle)) return true;
                entry.Target = null;
                entry.Boxes = null;
                entry.NextRetry = Time.unscaledTimeAsDouble + RetrySeconds;
            }
            if (Time.unscaledTimeAsDouble < entry.NextRetry) return false;
            entry.NextRetry = Time.unscaledTimeAsDouble + RetrySeconds;
            var roots = scene.GetRootGameObjects();
            if (roots is null || roots.WasCollected) return false;
            var target = MapTransformResolver.Find(roots, section.Source.Path);
            if (!Matches(target, section.Source, world.SceneHandle)) return false;
            var boxes = new BoxCollider[section.Source.Colliders.Count];
            for (int index = 0; index < boxes.Length; index++)
            {
                var child = MapTransformResolver.Find(roots, section.Source.Colliders[index].Transform.Path);
                if (!IsAlive(child)) return false;
                var components = child.gameObject.GetComponents<BoxCollider>();
                if (components is null || components.WasCollected || components.Length != 1) return false;
                boxes[index] = components[0];
            }
            if (!CollidersMatch(boxes, section.Source, world.SceneHandle)) return false;
            entry.Target = target;
            entry.Boxes = boxes;
            return true;
        }
        catch (Exception ex)
        {
            string failure = ex.GetType().Name + ": " + ex.Message;
            if (_lastFailure != failure)
            { _lastFailure = failure; Plugin.Log.LogWarning("Map section source unavailable: " + failure); }
            return false;
        }
    }

    private static bool Matches(Transform target, MapSectionSource source, int scene)
    {
        if (!MapTransformResolver.Matches(target, source.Path, scene)) return false;
        var matrix = target.localToWorldMatrix;
        for (int index = 0; index < 16; index++)
        {
            double value = matrix[index / 4, index % 4];
            if (!double.IsFinite(value) || Math.Abs(value - source.Matrix[index]) > source.Tolerance) return false;
        }
        return true;
    }

    private static bool CollidersMatch(BoxCollider[] boxes, MapSectionSource source, int scene)
    {
        if (boxes is null || boxes.Length != source.Colliders.Count) return false;
        for (int index = 0; index < boxes.Length; index++)
        {
            var box = boxes[index]; var expected = source.Colliders[index];
            if (!IsAlive(box) || !box.enabled || box.isTrigger || !Matches(box.transform, expected.Transform, scene)) return false;
            var center = box.center; var size = box.size;
            for (int axis = 0; axis < 3; axis++)
                if (!float.IsFinite(center[axis]) || !float.IsFinite(size[axis]) ||
                    Math.Abs(center[axis] - expected.Center[axis]) > expected.Transform.Tolerance ||
                    Math.Abs(size[axis] - expected.Size[axis]) > expected.Transform.Tolerance) return false;
        }
        return true;
    }

    internal static void Reset() { Cache.Clear(); _scene = 0; _variant = null; _lastFailure = null; }
    private sealed class Entry { internal Transform Target; internal BoxCollider[] Boxes; internal double NextRetry; }
}
