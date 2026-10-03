using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.MapSections;

internal readonly record struct RuntimeMapPlace(string Identity, string TextKey, string SpeechGroup);

internal static class RuntimeMapPlaces
{
    private static FootstepRaycastTrigger _trigger;
    private static ActorVisuals _visuals;
    private static long _session;
    private static string _lastFailure;
    private static double _nextDiscovery;
    // Same origin used by native HandleStepRaycast (0x15B5340), with its own range/mask.
    private const float NativeRayOriginOffset = 0.5f;
    // A character can acquire its visual hierarchy after a scene transition.
    private const double DiscoveryRetrySeconds = 1;

    internal static bool TryRead(MapSectionWorldSnapshot world, out RuntimeMapPlace place)
    {
        place = default;
        try
        {
            MapPlaceDiagnostics.Begin(world);
            var scene = SceneManager.GetActiveScene();
            var player = PlayerFinder.player;
            if (!scene.IsValid() || !scene.isLoaded || (int)scene.handle != world.SceneHandle ||
                scene.name != world.SceneName || scene.path != world.ScenePath || !IsAlive(player) ||
                !player.activeInHierarchy || (int)player.scene.handle != world.SceneHandle)
            { MapPlaceDiagnostics.Report("world-unavailable", "player or active scene does not match the current snapshot"); return false; }
            if (_session != world.Session) { Reset(); _session = world.Session; }
            if (!MapPlayerVisuals.TryResolve(player, world.SceneHandle, out var visuals)) return false;
            if (!Same(_visuals, visuals)) { _visuals = visuals; _trigger = null; _nextDiscovery = 0; }
            var visualRoot = visuals.gameObject;
            if (!Owned(_trigger, visualRoot, world.SceneHandle))
            {
                _trigger = null;
                if (Time.unscaledTimeAsDouble < _nextDiscovery) return false;
                _nextDiscovery = Time.unscaledTimeAsDouble + DiscoveryRetrySeconds;
                var triggers = visualRoot.GetComponentsInChildren<FootstepRaycastTrigger>();
                if (triggers is null || triggers.WasCollected)
                { MapPlaceDiagnostics.Report("trigger-discovery", "visual hierarchy returned no readable trigger collection"); return false; }
                foreach (var candidate in triggers)
                {
                    if (!Owned(candidate, visualRoot, world.SceneHandle)) continue;
                    if (IsAlive(_trigger)) { _trigger = null; MapPlaceDiagnostics.Report("trigger-ambiguous", "multiple active triggers belong to the current visual root"); return false; }
                    _trigger = candidate;
                }
            }
            if (!Owned(_trigger, visualRoot, world.SceneHandle))
            { MapPlaceDiagnostics.Report("trigger-missing", "no active trigger under " + MapPlaceDiagnostics.Path(visualRoot.transform)); return false; }
            if (!Same(_trigger.actorVisuals, visuals))
            { MapPlaceDiagnostics.Report("trigger-owner-mismatch", "trigger.actorVisuals does not match the current visual owner"); return false; }
            var feet = _trigger.feet;
            float distance = _trigger.raycastDistance;
            int mask = FootstepRaycastTrigger.raycastMask;
            if (feet is null || feet.WasCollected || !float.IsFinite(distance) || distance <= 0 || mask == 0)
            { MapPlaceDiagnostics.Report("foot-data-unavailable", $"validFeet={feet is not null && !feet.WasCollected}, range={distance}, mask={mask}"); return false; }
            double clearance = MapPlayerFloor.HeightAllowance(world);
            if (!double.IsFinite(world.Y)) return false;
            MapPlaceDiagnostics.Report("rig-ready", $"visual={MapPlaceDiagnostics.Path(visualRoot.transform)}, underPlayer={visualRoot.transform.IsChildOf(player.transform)}, range={distance}, mask={mask}, clearance={clearance}");
            string category = null;
            Transform source = null;
            bool anyFoot = false;
            foreach (var foot in feet)
            {
                if (!Available(foot, world.SceneHandle) || !foot.IsChildOf(visualRoot.transform) || !Finite(foot.position)) continue;
                anyFoot = true;
                if (!TryContact(foot.position, distance, mask, player, visualRoot, world, clearance,
                    out var owner, out var key, out bool blocked))
                { if (blocked) return false; continue; }
                if (category is not null && (category != key || RuntimePlaceNames.IsContext(key) && !Same(source, owner))) return false;
                category = key;
                if (!IsAlive(source) || owner.GetInstanceID() < source.GetInstanceID()) source = owner;
            }
            if (!anyFoot) MapPlaceDiagnostics.Report("feet-unowned", "no active finite foot transform belongs to the current visual root");
            if (category is null || !IsAlive(source)) return false;
            MapPlaceDiagnostics.Report("place-detected", category + " at " + MapPlaceDiagnostics.Path(source));
            string identity = world.Session + ":structure:" + source.GetInstanceID();
            string group = RuntimePlaceNames.IsContext(category) ? identity : category;
            place = new RuntimeMapPlace(identity, category, group);
            return true;
        }
        catch (Exception ex)
        {
            string failure = ex.GetType().Name + ": " + ex.Message;
            if (_lastFailure != failure) { _lastFailure = failure; Plugin.Log?.LogWarning("Map places: " + failure); }
            return false;
        }
    }

    private static bool TryContact(Vector3 position, float distance, int mask, GameObject player, GameObject visualRoot,
        MapSectionWorldSnapshot world, double clearance, out Transform owner, out string key, out bool blocked)
    {
        owner = null;
        key = null;
        blocked = false;
        var hits = Physics.RaycastAll(position + new Vector3(0, NativeRayOriginOffset, 0), Vector3.down,
            distance, mask, QueryTriggerInteraction.Ignore);
        if (hits is null || hits.WasCollected)
        { MapPlaceDiagnostics.Report("ray-unavailable", "physics returned no readable hit collection"); return false; }
        float nearestDistance = float.PositiveInfinity;
        foreach (var hit in hits)
        {
            if (!Physical(hit.collider, mask, player, visualRoot) || !float.IsFinite(hit.distance) ||
                hit.distance < 0 || hit.distance > distance) continue;
            if (hit.distance < nearestDistance) nearestDistance = hit.distance;
        }
        if (!float.IsFinite(nearestDistance))
        { MapPlaceDiagnostics.Report("ray-no-ground", $"no eligible physical floor, footY={position.y}, range={distance}, mask={mask}"); return false; }
        blocked = true;
        foreach (var hit in hits)
        {
            var collider = hit.collider;
            if (hit.distance != nearestDistance || !Physical(collider, mask, player, visualRoot)) continue;
            if (!Available(collider, world.SceneHandle) || !Finite(hit.normal) || hit.normal.y <= 0 ||
                IsAlive(collider.GetComponentInParent<Actor>()) || IsAlive(collider.GetComponentInParent<ActorVisuals>()))
            { MapPlaceDiagnostics.Report("ground-owner-rejected", MapPlaceDiagnostics.Path(collider.transform)); return false; }
            if (!Finite(hit.point) || Math.Abs(world.Y - hit.point.y) > clearance)
            { MapPlaceDiagnostics.Report("ground-height-rejected", $"playerY={world.Y}, floorY={hit.point.y}, allowance={clearance}, floor={MapPlaceDiagnostics.Path(collider.transform)}"); return false; }
            if (!TryClassify(collider.transform, world.SceneHandle, out var candidate, out var candidateKey))
            { MapPlaceDiagnostics.UnknownFloor(collider.transform); return false; }
            if (key is not null && (key != candidateKey || RuntimePlaceNames.IsContext(key) && !Same(owner, candidate))) return false;
            key = candidateKey;
            if (!IsAlive(owner) || candidate.GetInstanceID() < owner.GetInstanceID()) owner = candidate;
        }
        if (!IsAlive(owner)) return false;
        blocked = false;
        return true;
    }

    private static bool Physical(Collider collider, int mask, GameObject player, GameObject visualRoot) => IsAlive(collider) &&
        IsAlive(collider.gameObject) && collider.gameObject.activeInHierarchy && collider.enabled && !collider.isTrigger &&
        !collider.transform.IsChildOf(player.transform) && !collider.transform.IsChildOf(visualRoot.transform) &&
        (mask & (1 << collider.gameObject.layer)) != 0;

    private static bool TryClassify(Transform contact, int scene, out Transform owner, out string key)
    {
        owner = null;
        key = null;
        bool canClassify = true, floor = false;
        Transform contextOwner = null;
        string contextKey = null;
        for (var current = contact; IsAlive(current); current = current.parent)
        {
            if (!Available(current, scene) || RuntimePlaceNames.RejectBranch(current.name, floor)) return false;
            if (!IsAlive(current.parent) || current.name is "Visuals" or "Foreground" or "Environment") canClassify = false;
            if (!canClassify) continue;
            floor |= RuntimePlaceNames.IsFloor(current.name);
            if (floor && contextKey is null)
            {
                contextKey = RuntimePlaceNames.ClassifyContext(current.name);
                if (contextKey is not null) contextOwner = current;
            }
            if (key is null)
            {
                key = RuntimePlaceNames.Classify(current.name);
                if (key is not null) owner = current;
            }
        }
        if (contextKey is not null && (key is null || key == "map.section.walkway")) { key = contextKey; owner = contextOwner; }
        return key is not null;
    }

    private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    private static bool Same(UnityEngine.Object a, UnityEngine.Object b) => IsAlive(a) && IsAlive(b) && a.Pointer == b.Pointer;
    private static bool Available(Component component, int scene) => IsAlive(component) &&
        IsAlive(component.gameObject) && component.gameObject.activeInHierarchy && (int)component.gameObject.scene.handle == scene;
    private static bool Owned(Behaviour component, GameObject player, int scene) => Available(component, scene) &&
        component.isActiveAndEnabled && component.transform.IsChildOf(player.transform);
    internal static void Reset() { _trigger = null; _visuals = null; _session = 0; _lastFailure = null; _nextDiscovery = 0; MapPlayerFloor.Reset(); }
}
