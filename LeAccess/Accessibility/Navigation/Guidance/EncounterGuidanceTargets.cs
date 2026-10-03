using System;
using System.Collections.Generic;
using EHG.Multiplayer;
using LeAccess.Accessibility.MapSections;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Navigation.Guidance;

internal static class EncounterGuidanceTargets
{
    // Returning to the active timed encounter takes precedence over ordinary quest/exit markers.
    private const int ReturnPriority = -1;
    private static readonly Dictionary<int, RageOfMorditasGauntletWarningPointerVisualController> Pointers = new();
    private static readonly List<int> Expired = new();
    internal static bool HasTrackedPointers => Pointers.Count != 0;

    internal static void Track(RageOfMorditasGauntletWarningPointerVisualController pointer)
    {
        try
        {
            if (!IsAlive(pointer)) return;
            int id = pointer.GetInstanceID();
            if (Pointers.TryGetValue(id, out var current) && Same(current, pointer)) return;
            Expired.Clear();
            foreach (var entry in Pointers)
                if (!IsAlive(entry.Value) || !Loaded(entry.Value.gameObject)) Expired.Add(entry.Key);
            foreach (int expired in Expired) Pointers.Remove(expired);
            Pointers[id] = pointer;
        }
        catch (Exception) { }
    }

    internal static void Collect(MapSectionWorldSnapshot world, List<GuidanceTarget> targets)
    {
        Expired.Clear();
        foreach (var entry in Pointers)
        {
            try
            {
                var pointer = entry.Value;
                if (!IsAlive(pointer) || !Loaded(pointer.gameObject)) { Expired.Add(entry.Key); continue; }
                if (TryRead(pointer, world, out var target)) targets.Add(target);
            }
            catch (Exception) { Expired.Add(entry.Key); }
        }
        foreach (int id in Expired) Pointers.Remove(id);
    }

    private static bool TryRead(RageOfMorditasGauntletWarningPointerVisualController pointer,
        MapSectionWorldSnapshot world, out GuidanceTarget target)
    {
        target = default;
        if (!pointer.isActiveAndEnabled || !InWorld(pointer.gameObject, world) ||
            !InWorld(pointer.pointerObject, world) || !pointer.pointerObject.transform.IsChildOf(pointer.transform)) return false;
        var renderer = pointer.pointerObject.GetComponent<MeshRenderer>();
        if (!IsAlive(renderer) || !renderer.enabled || renderer.forceRenderingOff) return false;
        var destination = pointer._lookAtTransform;
        if (!IsAlive(destination) || !InWorld(destination.gameObject, world)) return false;
        var gauntlet = destination.GetComponent<RageOfMorditasGauntletVisualController>();
        if (!IsAlive(gauntlet) || !gauntlet.isActiveAndEnabled ||
            !Same(gauntlet._gauntletWarningPointerVisualControllerInstance, pointer) ||
            gauntlet._currentState != RageOfMorditasLogic.State.Gauntlet) return false;
        var encounter = gauntlet._sync;
        if (!IsAlive(encounter) || !encounter.isActiveAndEnabled || !Loaded(encounter.gameObject) ||
            !Same(encounter._gauntletVisuals, gauntlet) || encounter._state != RageOfMorditasLogic.State.Gauntlet) return false;
        var local = PlayerFinder.getLocalActorSync();
        if (!IsAlive(local) || !local.isActiveAndEnabled || !Loaded(local.gameObject) ||
            !Same(pointer._playerTransform, local.transform) || !OwnsPlayer(local, world)) return false;
        var position = destination.position;
        if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z)) return false;
        target = new(gauntlet.GetInstanceID(), ModText.Get("guidance.encounter_return"), position, 0,
            GuidanceTargetKind.EncounterReturn) { Priority = ReturnPriority };
        return true;
    }

    private static bool OwnsPlayer(ActorSync sync, MapSectionWorldSnapshot world)
    {
        var player = PlayerFinder.player;
        if (!InWorld(player, world) || world.Session != ((long)world.SceneHandle << 32 | (uint)player.GetInstanceID())) return false;
        Behaviour owner;
        if (GameplayEnvironment.IsOnlineClient)
        {
            var visuals = sync.actorVisuals;
            if (!Same(visuals, PlayerFinder.playerActorVisuals) || !Same(visuals.actorSync, sync) || visuals.dead) return false;
            owner = visuals;
        }
        else
        {
            var actor = sync.gameplayActor;
            if (!Same(actor, PlayerFinder.playerActorComponent) || !Same(actor.actorSync, sync) || actor.isDying()) return false;
            owner = actor;
        }
        return owner.isActiveAndEnabled && InWorld(owner.gameObject, world) &&
            (owner.transform.IsChildOf(player.transform) || player.transform.IsChildOf(owner.transform));
    }

    private static bool Same(UnityEngine.Object first, UnityEngine.Object second) =>
        IsAlive(first) && IsAlive(second) && first.Pointer == second.Pointer;
    private static bool Loaded(GameObject value) => IsAlive(value) && value.scene.IsValid() && value.scene.isLoaded;
    private static bool InWorld(GameObject value, MapSectionWorldSnapshot world) =>
        Loaded(value) && value.activeInHierarchy && (int)value.scene.handle == world.SceneHandle;
    internal static void Reset() { Pointers.Clear(); Expired.Clear(); }
}
