using System;
using System.Collections.Generic;
using EHG.Multiplayer;
using LE.Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using LeAccess.Accessibility.Players;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.MapSections;

internal readonly record struct MapSectionWorldSnapshot(long Session, int SceneHandle, string SceneName,
    string ScenePath, double X, double Y, double Z, double BodyRadius, int? Variant)
{
    internal bool IsFixedLayout { get; init; } = true;
}

internal static class MapSectionWorld
{
    private static SectionMap _validatedMap;
    private static int _validatedScene;
    private static int? _validatedVariant;
    private static bool _validAnchors;
    private static Transform[] _anchorTransforms;
    private static double _nextAnchorRetry;
    // Asynchronous scene roots may arrive late; retry discovery without scanning on every service tick.
    private const double AnchorRetrySeconds = 1;
    private static string _lastFailure;

    internal static bool TryCapture(out MapSectionWorldSnapshot snapshot)
    {
        if (!TryCapturePlayer(out snapshot)) return false;
        return snapshot.IsFixedLayout;
    }

    internal static bool TryCapturePlayer(out MapSectionWorldSnapshot snapshot)
    {
        snapshot = default;
        try
        {
            var scene = SceneManager.GetActiveScene();
            var player = LocalPlayerSource.Player;
            if (!scene.IsValid() || !scene.isLoaded ||
                !IsAlive(player) || !player.activeInHierarchy || (int)player.scene.handle != (int)scene.handle) return false;
            double radius = 0;
            if (GameplayEnvironment.IsOnlineClient)
            {
                var visuals = LocalPlayerSource.Visuals;
                if (!Available(visuals, scene) || visuals.dead || !OwnsPlayer(visuals.gameObject, player)) return false;
                float bodyRadius = visuals.getRadius();
                if (float.IsFinite(bodyRadius) && bodyRadius > 0) radius = bodyRadius;
            }
            else
            {
                var actor = PlayerFinder.playerActorComponent;
                if (!Available(actor, scene) || actor.isDying() || !OwnsPlayer(actor.gameObject, player)) return false;
                float bodyRadius = actor.getRadius();
                if (float.IsFinite(bodyRadius) && bodyRadius > 0) radius = bodyRadius;
            }
            var position = player.transform.position;
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z)) return false;
            int? variant = null;
            try
            {
                var service = ServiceProvider.SceneVariantService;
                if (service is not null && !service.WasCollected && service.TryGetActiveVariantIndex(out int index))
                    variant = index;
            }
            catch (Exception ex) { ReportFailure(ex); }
            long session = ((long)(int)scene.handle << 32) | (uint)player.GetInstanceID();
            snapshot = new MapSectionWorldSnapshot(session, (int)scene.handle, scene.name, scene.path,
                position.x, position.y, position.z, radius, variant)
            { IsFixedLayout = !SceneList.IsPcgScene(scene.name) };
            return true;
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    internal static bool ValidateAnchors(SectionMap map, MapSectionWorldSnapshot snapshot)
    {
        try
        {
            var scene = SceneManager.GetActiveScene();
            if (map is null || !scene.IsValid() || !scene.isLoaded || (int)scene.handle != snapshot.SceneHandle ||
                scene.name != snapshot.SceneName || scene.path != snapshot.ScenePath ||
                map.Scene != snapshot.SceneName || map.ScenePath != snapshot.ScenePath) return false;
            if (SceneList.IsMonolithEcho(scene))
                foreach (var section in map.Sections)
                    if (section.Source == null || section.Source.Colliders.Count == 0) return false;
            if (ReferenceEquals(map, _validatedMap) && _validatedScene == snapshot.SceneHandle &&
                _validatedVariant == snapshot.Variant)
            {
                if (_validAnchors)
                {
                    bool unchanged = _anchorTransforms is not null && _anchorTransforms.Length == map.Anchors.Length;
                    for (int index = 0; unchanged && index < map.Anchors.Length; index++)
                        unchanged = PositionMatches(_anchorTransforms[index], map.Anchors[index], snapshot.SceneHandle);
                    if (unchanged) return true;
                    _validAnchors = false;
                    _anchorTransforms = null;
                    _nextAnchorRetry = Time.unscaledTimeAsDouble + AnchorRetrySeconds;
                    return false;
                }
                if (Time.unscaledTimeAsDouble < _nextAnchorRetry) return false;
            }
            _validatedMap = map;
            _validatedScene = snapshot.SceneHandle;
            _validatedVariant = snapshot.Variant;
            _validAnchors = false;
            _anchorTransforms = null;
            _nextAnchorRetry = Time.unscaledTimeAsDouble + AnchorRetrySeconds;
            if (!HasAnchorBasis(map.Anchors)) return false;
            var roots = scene.GetRootGameObjects();
            if (roots is null || roots.WasCollected) return false;
            var targets = new List<Transform>();
            foreach (var anchor in map.Anchors)
            {
                var target = MapTransformResolver.Find(roots, anchor.Path);
                if (!PositionMatches(target, anchor, snapshot.SceneHandle)) return false;
                targets.Add(target);
            }
            _anchorTransforms = targets.ToArray();
            return _validAnchors = true;
        }
        catch (Exception ex)
        {
            _validAnchors = false;
            _anchorTransforms = null;
            _nextAnchorRetry = Time.unscaledTimeAsDouble + AnchorRetrySeconds;
            ReportFailure(ex);
            return false;
        }
    }

    private static bool PositionMatches(Transform target, MapAnchor anchor, int sceneHandle)
    {
        if (!MapTransformResolver.Matches(target, anchor.Path, sceneHandle)) return false;
        var position = target.position;
        double dx = position.x - anchor.X, dy = position.y - anchor.Y, dz = position.z - anchor.Z;
        double distance = dx * dx + dy * dy + dz * dz;
        return double.IsFinite(distance) && distance <= anchor.Tolerance * anchor.Tolerance;
    }

    private static bool HasAnchorBasis(MapAnchor[] anchors)
    {
        if (anchors is null || anchors.Length < 3) return false;
        var paths = new HashSet<string>(StringComparer.Ordinal);
        var points = new HashSet<(double, double)>();
        foreach (var anchor in anchors)
        {
            if (string.IsNullOrWhiteSpace(anchor.Path) || !paths.Add(anchor.Path) || !points.Add((anchor.X, anchor.Z)) ||
                !double.IsFinite(anchor.X) || !double.IsFinite(anchor.Y) || !double.IsFinite(anchor.Z) ||
                !double.IsFinite(anchor.Tolerance) || anchor.Tolerance <= 0) return false;
            foreach (var part in anchor.Path.Split('/')) if (string.IsNullOrEmpty(part)) return false;
        }
        var a = anchors[0]; var b = anchors[1];
        for (int index = 2; index < anchors.Length; index++)
        {
            var c = anchors[index];
            double area = (b.X - a.X) * (c.Z - a.Z) - (b.Z - a.Z) * (c.X - a.X);
            if (double.IsFinite(area) && area != 0) return true;
        }
        return false;
    }

    private static bool Available(Behaviour component, Scene scene) => IsAlive(component) &&
        component.isActiveAndEnabled && (int)component.gameObject.scene.handle == (int)scene.handle;
    private static bool OwnsPlayer(GameObject owner, GameObject player) =>
        owner.transform.IsChildOf(player.transform) || player.transform.IsChildOf(owner.transform);
    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Map section world unavailable: {failure}");
    }
    internal static void Reset()
    {
        _validatedMap = null;
        _validatedScene = 0;
        _validatedVariant = null;
        _validAnchors = false;
        _anchorTransforms = null;
        _nextAnchorRetry = 0;
        _lastFailure = null;
    }
}
