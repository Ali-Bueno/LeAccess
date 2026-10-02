using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.MapSections;

internal static class RuntimeSceneryPlaces
{
    private readonly record struct Candidate(MeshRenderer Renderer, Transform Owner, string Key);
    private static readonly List<Candidate> Candidates = new();
    private static long _session;
    private static bool _indexed;
    private static string _lastFailure;

    internal static bool TryRead(MapSectionWorldSnapshot world, out RuntimeMapPlace place)
    {
        place = default;
        try
        {
            var scene = SceneManager.GetActiveScene();
            var player = PlayerFinder.player;
            if (!scene.IsValid() || !scene.isLoaded || (int)scene.handle != world.SceneHandle ||
                scene.name != world.SceneName || scene.path != world.ScenePath || !IsAlive(player) ||
                !player.activeInHierarchy || (int)player.scene.handle != world.SceneHandle) return false;
            if (_session != world.Session) { Reset(); _session = world.Session; }
            MapPlaceDiagnostics.Begin(world);
            if (!_indexed)
            {
                Discover(scene, player);
                _indexed = true;
            }
            if (Candidates.Count == 0) return false;
            if (!MapPlayerFloor.TryGetQueryAgent(world, out var agent))
            { MapPlaceDiagnostics.Report("scenery-nav-missing", "no live NavMesh query settings owned by the current player"); return false; }
            float radius = agent.radius;
            float height = agent.height;
            if (!float.IsFinite(radius) || radius <= 0 || !float.IsFinite(height) || height <= 0 || agent.areaMask == 0) return false;
            float bakedRadius = NavMesh.GetSettingsByID(agent.agentTypeID).agentRadius;
            float navigationClearance = float.IsFinite(bakedRadius) && bakedRadius > 0 ? Math.Max(radius, bakedRadius) : radius;
            MapPlaceDiagnostics.Report("scenery-nav-ready", $"owner={MapPlaceDiagnostics.Path(agent.transform)}, enabled={agent.isActiveAndEnabled}, radius={radius}, bakedRadius={bakedRadius}, height={height}, type={agent.agentTypeID}, mask={agent.areaMask}");
            float bodyRadius = double.IsFinite(world.BodyRadius) ? Math.Max(radius, (float)world.BodyRadius) : radius;
            // D2R's useful "beside this footprint" behavior, scaled to one actual body width.
            float reach = bodyRadius * 2;
            double clearance = MapPlayerFloor.HeightAllowance(world);
            var position = new Vector3((float)world.X, (float)world.Y, (float)world.Z);
            if (!SceneryFootprint.Finite(position) || !float.IsFinite(reach) || !double.IsFinite(clearance) ||
                !NavMesh.SamplePositionFilter(position, out var origin, radius, agent.agentTypeID, agent.areaMask) ||
                !SceneryFootprint.Finite(origin.position) || Math.Abs(position.y - origin.position.y) > clearance) return false;
            Candidate? selected = null;
            float nearest = float.PositiveInfinity;
            foreach (var candidate in Candidates)
            {
                if (!Available(candidate.Renderer, world.SceneHandle) || !candidate.Renderer.enabled ||
                    !Available(candidate.Owner, world.SceneHandle) || !candidate.Renderer.transform.IsChildOf(candidate.Owner) ||
                    !AllowedBranch(candidate.Renderer.transform, player, world.SceneHandle) ||
                    SceneryFootprint.Classify(candidate.Renderer.name, candidate.Renderer.transform.parent?.name) != candidate.Key ||
                    !SceneryFootprint.TryClosest(candidate.Renderer.transform, candidate.Renderer.localBounds, position, out var closest) ||
                    Math.Abs(closest.y - position.y) > clearance) continue;
                float distance = SceneryFootprint.HorizontalDistance(position, closest);
                if (!float.IsFinite(distance) || distance > reach || distance > nearest ||
                    distance == nearest && selected.HasValue && candidate.Owner.GetInstanceID() >= selected.Value.Owner.GetInstanceID()) continue;
                if (!Reachable(position, origin.position, closest, player, candidate.Owner, agent, clearance, reach, navigationClearance)) continue;
                selected = candidate;
                nearest = distance;
            }
            if (!selected.HasValue) return false;
            var found = selected.Value;
            string identity = world.Session + ":scenery:" + found.Owner.GetInstanceID();
            // Several neighboring tents or gate pieces describe one kind of place.
            place = new RuntimeMapPlace(identity, found.Key, found.Key);
            MapPlaceDiagnostics.Report("scenery-" + found.Key, $"{MapPlaceDiagnostics.Path(found.Owner)}, footprintDistance={nearest}, reach={reach}");
            return true;
        }
        catch (Exception ex)
        {
            string failure = ex.GetType().Name + ": " + ex.Message;
            if (_lastFailure != failure) { _lastFailure = failure; Plugin.Log?.LogWarning("Map scenery: " + failure); }
            return false;
        }
    }

    private static void Discover(Scene scene, GameObject player)
    {
        var roots = scene.GetRootGameObjects();
        if (roots is null || roots.WasCollected) return;
        foreach (var root in roots)
        {
            if (!IsAlive(root) || root.Pointer == player.Pointer || (int)root.scene.handle != (int)scene.handle) continue;
            // Include inactive phase variants in this one-time index; recheck activity on every read.
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers is null || renderers.WasCollected) continue;
            foreach (var renderer in renderers)
            {
                if (!IsAlive(renderer) || !AllowedBranch(renderer.transform, player, (int)scene.handle)) continue;
                string key = SceneryFootprint.Classify(renderer.name, renderer.transform.parent?.name);
                if (key is null) continue;
                var owner = key == "map.place.wagon" ? renderer.transform.parent : renderer.transform;
                Candidates.Add(new Candidate(renderer, owner, key));
            }
        }
        MapPlaceDiagnostics.Report("scenery-index", $"{Candidates.Count} authored model footprints in the current scene");
    }

    private static bool Reachable(Vector3 position, Vector3 origin, Vector3 closest, GameObject player,
        Transform owner, NavMeshAgent agent, double clearance, float reach, float navigationClearance)
    {
        float distance = SceneryFootprint.HorizontalDistance(position, closest);
        var approach = closest;
        if (distance > 0)
        {
            // Baked obstacle clearance can exceed the runtime component radius (Z32: 0.5 vs 0.1).
            float offset = Math.Min(distance, navigationClearance);
            approach.x += (position.x - closest.x) / distance * offset;
            approach.z += (position.z - closest.z) / distance * offset;
        }
        if (!NavMesh.SamplePositionFilter(approach, out var endpoint, navigationClearance, agent.agentTypeID, agent.areaMask) ||
            !SceneryFootprint.Finite(endpoint.position) || Math.Abs(position.y - endpoint.position.y) > clearance ||
            SceneryFootprint.HorizontalDistance(origin, endpoint.position) > reach ||
            NavMesh.RaycastFilter(origin, endpoint.position, out _, agent.agentTypeID, agent.areaMask)) return false;

        // NavMesh sampling may land on our side of an intervening wall. Check the actual sightline too.
        float eyeHeight = agent.height / 2;
        var start = position + new Vector3(0, eyeHeight, 0);
        var end = closest + new Vector3(0, eyeHeight, 0);
        var direction = end - start;
        float length = direction.magnitude;
        if (!float.IsFinite(length)) return false;
        if (length == 0) return true;
        var hits = Physics.RaycastAll(start, direction / length, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits is null || hits.WasCollected) return false;
        foreach (var hit in hits)
        {
            var collider = hit.collider;
            if (!IsAlive(collider) || !collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy ||
                collider.transform.IsChildOf(player.transform) || collider.transform.IsChildOf(owner)) continue;
            if (float.IsFinite(hit.distance) && hit.distance >= 0 && hit.distance <= length) return false;
        }
        return true;
    }

    private static bool AllowedBranch(Transform transform, GameObject player, int scene)
    {
        if (transform.IsChildOf(player.transform) || IsAlive(transform.GetComponentInParent<Actor>()) ||
            IsAlive(transform.GetComponentInParent<ActorVisuals>())) return false;
        for (var current = transform; IsAlive(current); current = current.parent)
            if ((int)current.gameObject.scene.handle != scene || RuntimePlaceNames.RejectBranch(current.name, explicitFloor: true)) return false;
        return true;
    }

    private static bool Available(Component value, int scene) => IsAlive(value) && IsAlive(value.gameObject) &&
        value.gameObject.activeInHierarchy && (int)value.gameObject.scene.handle == scene;

    internal static void Reset() { Candidates.Clear(); _session = 0; _indexed = false; _lastFailure = null; }
}
