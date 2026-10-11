using System;
using EHG.Multiplayer;
using LeAccess.Accessibility.Utils;
using LeAccess.Accessibility.Npcs;
using LeAccess.Accessibility.WorldCues;
using LeAccess.I18n;
using Networking.Multiplayer.Interactables.Portals;
using UnityEngine;
using UnityEngine.SceneManagement;
using LeAccess.Accessibility.Players;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Landmarks;

internal readonly record struct LandmarkTarget(long Session, int InstanceId, string Name);

internal static class LandmarkProbe
{
    internal static string LastFailure { get; private set; }
    internal static string LastStatus { get; private set; }

    internal static bool TryRead(out LandmarkTarget target)
    {
        target = default;
        LastFailure = null;
        LastStatus = "player_or_scene_unavailable";
        var scene = SceneManager.GetActiveScene();
        var player = LocalPlayerSource.Player;
        if (!scene.isLoaded || !IsAlive(player) || !player.activeInHierarchy ||
            (int)player.scene.handle != (int)scene.handle) return false;
        if (GameplayEnvironment.IsOnlineClient)
        {
            var visuals = LocalPlayerSource.Visuals;
            if (!Available(visuals, scene) || visuals.dead || !SamePlayerHierarchy(visuals.gameObject, player)) return false;
        }
        else
        {
            var actor = PlayerFinder.playerActorComponent;
            if (!Available(actor, scene) || actor.isDying() || !SamePlayerHierarchy(actor.gameObject, player)) return false;
        }
        var listeners = WorldObjectClickListener.allListener;
        LastStatus = "registry_unavailable";
        if (listeners is null || listeners.WasCollected) return false;
        Vector3 position = player.transform.position;
        long session = ((long)(int)scene.handle << 32) | (uint)player.GetInstanceID();
        bool found = false;
        float nearest = float.PositiveInfinity;
        int active = 0, tags = 0, ownedTags = 0, inRange = 0, named = 0;
        string candidateStatus = "none";
        for (int index = 0; index < listeners.Count; index++)
        {
            try
            {
                var listener = listeners[index];
                if (!Available(listener, scene)) continue;
                active++;
                var tag = listener.hoverTagOnMouseOver;
                // Installed Z12/Z22 exits keep this optional field null; the tag
                // is authored on the listener's exact GameObject.
                if (tag is null)
                {
                    tag = listener.gameObject.GetComponent<HoverTagOnMouseOver>();
                    if (IsAlive(tag)) ownedTags++;
                }
                PortalVisualController portal = null;
                if (tag is null || IsAlive(tag) &&
                    (tag.hoverTargetType == HoverTagOnMouseOver.HoverTargetType.OTHER ||
                     tag.hoverTargetType == HoverTagOnMouseOver.HoverTargetType.LOCALIZATION_KEY))
                {
                    portal = listener.gameObject.GetComponentInParent<PortalVisualController>();
                    if (!Available(portal, scene) || !IsAlive(portal.mouseListenerObj) ||
                        portal.mouseListenerObj.Pointer != listener.gameObject.Pointer)
                        portal = null;
                    if (tag is null && IsAlive(portal)) tag = portal.hoverTag;
                }
                bool hasTag = Available(tag, scene);
                if (hasTag) tags++;
                float range = listener.interactionRange;
                if (!float.IsFinite(range) || range <= 0) continue;
                Vector3 destination = listener.transform.position;
                if (listener.controllerTargetingUsePlayerY) destination.y = position.y;
                float distance = (destination - position).sqrMagnitude;
                if (!float.IsFinite(distance) || distance > range * range) continue;
                inRange++;
                int id = listener.GetInstanceID();
                if (found && (distance > nearest || distance == nearest && id >= target.InstanceId)) continue;
                string nameStatus = "no_tag";
                string name = hasTag ? ReadName(tag, scene, portal, out nameStatus) : null;
                if (string.IsNullOrWhiteSpace(name))
                {
                    // Shrines, time rifts and the Nemesis altar carry generic or key-only tags.
                    var role = InteractableRoles.Read(listener);
                    if (role != InteractableRole.None)
                    {
                        name = InteractableRoles.Name(role, listener, hasTag ? tag : null);
                        nameStatus = $"role:{role}";
                    }
                    else if (UntaggedTransitions.ReadDestination(listener) is { } toScene)
                    {
                        name = UntaggedTransitions.SceneName(toScene);
                        nameStatus = $"untagged_destination:{toScene}";
                    }
                    else if (WorldServiceReader.IsReadable(listener))
                    {
                        // Notes are no longer NPCs, so their dialogue name is announced here.
                        name = NpcNameReader.Read(listener, out _);
                        nameStatus = "readable";
                    }
                }
                string service = WorldServiceReader.ReadServiceKey(listener);
                if (service is not null)
                {
                    // NPC speech owns named services; landmarks cover objects such as the stash and waypoint.
                    if (WorldServiceReader.IsNpc(listener)) continue;
                    name = ModText.Get(service);
                    nameStatus = service;
                }
                candidateStatus = $"{id}:{(hasTag ? tag.hoverTargetType.ToString() : "no_tag")}:{nameStatus}";
                if (!string.IsNullOrWhiteSpace(name)) named++;
                if (string.IsNullOrWhiteSpace(name) || !listener.CanInteract()) continue;
                target = new LandmarkTarget(session, id, name);
                nearest = distance;
                found = true;
            }
            catch (Exception ex) { LastFailure ??= $"{ex.GetType().Name}: {ex.Message}"; }
        }
        LastStatus = $"listeners={listeners.Count} active={active} tags={tags} owned_tags={ownedTags} " +
            $"in_range={inRange} named={named} target={(found ? target.InstanceId : 0)} candidate={candidateStatus}";
        return found;
    }

    // Exploration scanner: the same hover-tag and portal resolution as TryRead, for one listener.
    internal static string ReadLabel(WorldObjectClickListener listener, Scene scene, out bool portalLabel, out bool transition)
    {
        portalLabel = transition = false;
        var tag = listener.hoverTagOnMouseOver;
        if (tag is null) tag = listener.gameObject.GetComponent<HoverTagOnMouseOver>();
        PortalVisualController portal = null;
        if (tag is null || IsAlive(tag) &&
            (tag.hoverTargetType == HoverTagOnMouseOver.HoverTargetType.OTHER ||
             tag.hoverTargetType == HoverTagOnMouseOver.HoverTargetType.LOCALIZATION_KEY))
        {
            portal = listener.gameObject.GetComponentInParent<PortalVisualController>();
            if (!Available(portal, scene) || !IsAlive(portal.mouseListenerObj) ||
                portal.mouseListenerObj.Pointer != listener.gameObject.Pointer)
                portal = null;
            if (tag is null && IsAlive(portal)) tag = portal.hoverTag;
        }
        if (!Available(tag, scene)) return null;
        string name = ReadName(tag, scene, portal, out string status);
        portalLabel = status == "owned_portal_label";
        transition = status.StartsWith("destination:", StringComparison.Ordinal);
        return name;
    }

    private static string ReadName(HoverTagOnMouseOver tag, Scene scene, PortalVisualController portal,
        out string status)
    {
        status = "authored_label";
        if (tag.hoverTargetType == HoverTagOnMouseOver.HoverTargetType.SIGNPOST)
            return TextUtils.CleanText(tag.hoverText);
        if (tag.hoverTargetType == HoverTagOnMouseOver.HoverTargetType.OTHER ||
            tag.hoverTargetType == HoverTagOnMouseOver.HoverTargetType.LOCALIZATION_KEY)
        {
            // Native portal initialization localizes this text; other generic tags
            // are not evidence of a portal. See reference/map-landmarks.md.
            status = "portal_unavailable";
            if (!IsAlive(portal) || !IsAlive(portal.hoverTag) || portal.hoverTag.Pointer != tag.Pointer ||
                portal.isNonInteractableByPlayer()) return null;
            status = "owned_portal_label";
            return TextUtils.CleanText(tag.hoverText);
        }
        status = "unsupported_type";
        TransitionOrGateInteraction interaction;
        if (tag.hoverTargetType == HoverTagOnMouseOver.HoverTargetType.ZONE_TRANSITION)
            interaction = tag.sceneInteraction;
        else if (tag.hoverTargetType == HoverTagOnMouseOver.HoverTargetType.DUNGEON_TRANSITION)
            interaction = tag.dungeonTransitionInteraction;
        else return null;
        status = "destination_metadata_unavailable";
        // Native hover labels read these typed references even when their
        // metadata GameObject is inactive; availability belongs to the listener.
        if (!IsAlive(interaction) || (int)interaction.gameObject.scene.handle != (int)scene.handle ||
            string.IsNullOrWhiteSpace(interaction.sceneName)) return null;
        status = $"unknown_destination:{interaction.sceneName}";
        if (!SceneList.TryGetNonDefaultSceneDetails(interaction.sceneName, out var details) ||
            details is null || details.WasCollected) return null;
        status = $"destination:{interaction.sceneName}:metadata_active={interaction.isActiveAndEnabled}";
        return TextUtils.CleanText(details.LocalizedName);
    }

    private static bool Available(Behaviour component, Scene scene) => IsAlive(component) &&
        component.isActiveAndEnabled && (int)component.gameObject.scene.handle == (int)scene.handle;

    private static bool SamePlayerHierarchy(GameObject componentOwner, GameObject player) =>
        componentOwner.transform.IsChildOf(player.transform) || player.transform.IsChildOf(componentOwner.transform);
}
