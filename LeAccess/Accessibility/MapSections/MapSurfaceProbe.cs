using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.MapSections;

internal static class MapSurfaceProbe
{
    private static FootstepRaycastTrigger _trigger;
    private static long _session;
    private static string _lastFailure;
    private static Il2CppReferenceArray<FootstepMaterial> _materials;
    private static Il2CppStructArray<float> _volumes;
    // Native HandleStepRaycast (0x15B5340) adds this exact vertical origin offset.
    private const float NativeRayOriginOffset = 0.5f;

    internal static bool TryRead(MapSectionWorldSnapshot world, out string textKey)
    {
        textKey = null;
        try
        {
            var scene = SceneManager.GetActiveScene();
            var player = PlayerFinder.player;
            if (!scene.IsValid() || !scene.isLoaded || (int)scene.handle != world.SceneHandle ||
                scene.name != world.SceneName || scene.path != world.ScenePath || !IsAlive(player) ||
                !player.activeInHierarchy || (int)player.scene.handle != world.SceneHandle) return false;
            if (_session != world.Session || !Owned(_trigger, player, world.SceneHandle))
            {
                _session = world.Session;
                _trigger = null;
                var triggers = player.GetComponentsInChildren<FootstepRaycastTrigger>();
                if (triggers is null || triggers.WasCollected) return false;
                foreach (var candidate in triggers)
                {
                    if (!Owned(candidate, player, world.SceneHandle)) continue;
                    if (IsAlive(_trigger)) { _trigger = null; return false; }
                    _trigger = candidate;
                }
            }
            if (!Owned(_trigger, player, world.SceneHandle)) return false;
            var visuals = _trigger.actorVisuals;
            if (!Owned(visuals, player, world.SceneHandle) || visuals.dead ||
                (IsAlive(PlayerFinder.playerActorVisuals) && !Same(visuals, PlayerFinder.playerActorVisuals))) return false;
            var emitter = _trigger.emitter;
            var collider = _trigger.lastCollider;
            var holder = _trigger.lastHolder;
            if (!Owned(emitter, player, world.SceneHandle) || emitter.ignoreSurfaceSound ||
                !Available(collider, world.SceneHandle) || !collider.enabled ||
                !Available(holder, world.SceneHandle) || !holder.isActiveAndEnabled ||
                !Same(collider.GetComponentInParent<FootstepSoundHolder>(), holder)) return false;
            // Only these two inspected getters are known to perform pure data sampling.
            if (holder.TryCast<ColliderFootstepSoundHolder>() is null &&
                holder.TryCast<TerrainFootstepSoundHolder>() is null) return false;
            string current = emitter.useOverrideMaterials && !holder.ignoreOverrides
                ? DominantPair(emitter.overrideMaterialA, emitter.overrideVolumeA,
                    emitter.overrideMaterialB, emitter.overrideVolumeB)
                : Dominant(emitter.activeMaterials, emitter.activeVolumes);
            if (current is null) return false;
            var feet = _trigger.feet;
            float distance = _trigger.raycastDistance;
            int mask = FootstepRaycastTrigger.raycastMask;
            if (feet is null || feet.WasCollected || !float.IsFinite(distance) || distance <= 0 || mask == 0) return false;
            foreach (var foot in feet)
            {
                if (!Available(foot, world.SceneHandle) || !foot.IsChildOf(player.transform)) continue;
                var position = foot.position;
                if (!Finite(position)) continue;
                var hits = Physics.RaycastAll(position + new Vector3(0, NativeRayOriginOffset, 0),
                    Vector3.down, distance, mask, QueryTriggerInteraction.Ignore);
                if (hits is null || hits.WasCollected) continue;
                Collider nearest = null;
                FootstepSoundHolder nearestHolder = null;
                float nearestDistance = float.PositiveInfinity;
                bool ambiguous = false;
                foreach (var hit in hits)
                {
                    var target = hit.collider;
                    if (!Available(target, world.SceneHandle) || !target.enabled || target.isTrigger ||
                        !float.IsFinite(hit.distance) || hit.distance < 0 || hit.distance > distance) continue;
                    var soundHolder = target.GetComponentInParent<FootstepSoundHolder>();
                    if (!Available(soundHolder, world.SceneHandle) || !soundHolder.isActiveAndEnabled) continue;
                    if (hit.distance < nearestDistance)
                    { nearest = target; nearestHolder = soundHolder; nearestDistance = hit.distance; ambiguous = false; }
                    else if (hit.distance == nearestDistance && !Same(target, nearest)) ambiguous = true;
                }
                if (ambiguous || !Same(nearest, collider) || !Same(nearestHolder, holder)) continue;
                if (_materials is null || _materials.WasCollected) _materials = new Il2CppReferenceArray<FootstepMaterial>(2);
                if (_volumes is null || _volumes.WasCollected) _volumes = new Il2CppStructArray<float>(2);
                _materials[0] = _materials[1] = null;
                _volumes[0] = _volumes[1] = 0;
                // Local buffers only; never invoke Step, FootstepTrigger or audio playback.
                holder.GetSurfaceSound(position, ref _materials, ref _volumes);
                if (Dominant(_materials, _volumes) != current) continue;
                textKey = current;
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            string failure = ex.GetType().Name + ": " + ex.Message;
            if (_lastFailure != failure) { _lastFailure = failure; Plugin.Log?.LogWarning("Map surfaces: " + failure); }
            return false;
        }
    }

    private static string Dominant(Il2CppReferenceArray<FootstepMaterial> materials, Il2CppStructArray<float> volumes)
    {
        if (materials is null || materials.WasCollected || volumes is null || volumes.WasCollected ||
            materials.Length != volumes.Length) return null;
        float largest = 0;
        string selected = null;
        bool ambiguous = false;
        for (int i = 0; i < materials.Length; i++)
        {
            float weight = volumes[i];
            if (!float.IsFinite(weight) || weight <= 0) continue;
            string category = Category(materials[i]);
            if (weight > largest) { largest = weight; selected = category; ambiguous = false; }
            else if (weight == largest && category != selected) ambiguous = true;
        }
        return ambiguous ? null : selected;
    }

    private static string DominantPair(FootstepMaterial a, float weightA, FootstepMaterial b, float weightB)
    {
        bool validA = float.IsFinite(weightA) && weightA > 0;
        bool validB = float.IsFinite(weightB) && weightB > 0;
        string categoryA = validA ? Category(a) : null, categoryB = validB ? Category(b) : null;
        if (!validA) return validB ? categoryB : null;
        if (!validB) return categoryA;
        if (weightA == weightB) return categoryA == categoryB ? categoryA : null;
        return weightA > weightB ? categoryA : categoryB;
    }

    private static string Category(FootstepMaterial material)
    {
        if (!IsAlive(material)) return null;
        var sound = material.soundEvent;
        if (sound is null || sound.WasCollected || sound.IsNull) return null;
        var guid = sound.Guid;
        // Exact installed PermaLoad FootstepMaterial GUIDs: reference/footstep-surfaces.md.
        // Do not infer a floor from asset names, renderer materials or equipment events.
        string name = (guid.Data1, guid.Data2, guid.Data3, guid.Data4) switch
        {
            (887160494,1076243030,1257745840,1069063323) => "snow",
            (-1599689582,1211331962,1259039645,693955642) => "wood",
            (-1774636125,1078812808,-1665080694,1776409609) => "mud",
            (-921771577,1145676817,-27846493,-1889599482) => "ice",
            (-1651031364,1263018041,1950930355,680759971) => "sand",
            (-1232125423,1174426010,-830846533,148521210) => "concrete",
            (875980879,1198802404,-949527367,-1243303349) => "water",
            (1422184698,1315540948,1827935151,1868116228) => "grass",
            (-1694723251,1234617459,998535565,-59455498) => "gravel",
            (1156031969,1198785631,1008576413,1037585669) => "metal",
            (1352783415,1321329726,-1257089116,-471361175) => "water",
            (-657236694,1163284407,873296268,1716095648) => "dirt",
            _ => null
        };
        return name is null ? null : "map.surface." + name;
    }

    private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    private static bool Same(UnityEngine.Object a, UnityEngine.Object b) => IsAlive(a) && IsAlive(b) && a.Pointer == b.Pointer;
    private static bool Available(Component component, int scene) => IsAlive(component) &&
        IsAlive(component.gameObject) && component.gameObject.activeInHierarchy && (int)component.gameObject.scene.handle == scene;
    private static bool Owned(Behaviour component, GameObject player, int scene) => Available(component, scene) &&
        component.isActiveAndEnabled && component.transform.IsChildOf(player.transform);
    internal static void Reset()
    { _trigger = null; _session = 0; _lastFailure = null; _materials = null; _volumes = null; }
}
