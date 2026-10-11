using System.Collections.Generic;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Landmarks;
using LeAccess.Accessibility.Npcs;
using LeAccess.Accessibility.Utils;
using LeAccess.Accessibility.WorldCues;
using LeAccess.I18n;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Exploration;

// Found is the bare name for "the radar has logged a new ..."; Destination the scene an exit leads to.
internal readonly record struct ExplorationPlace(ExplorationCategory Category, string Name, string Found, string Destination);

// Files a click listener under the D2R scanner's categories (d2r-access native_entities.cpp ObjectCategory):
// the specific ones before the general, levers, notes and other objects into the catch-all.
internal static class ExplorationClassifier
{
    internal static bool TryDescribe(WorldObjectClickListener listener, out ExplorationPlace place)
    {
        place = default;
        if (!IsAlive(listener) || !listener.isActiveAndEnabled || !IsAlive(listener.gameObject) ||
            !listener.gameObject.activeInHierarchy || !listener.gameObject.scene.IsValid() ||
            !listener.gameObject.scene.isLoaded || !listener.CanInteract() || IsLoot(listener)) return false;
        string service = WorldServiceReader.ReadServiceKey(listener);
        // The stash and waypoints also pass the NPC test.
        if (service == "service.stash") return Place(ExplorationCategory.Stash, ModText.Get(service), out place);
        if (service == "service.waypoint") return Place(ExplorationCategory.Waypoint, ModText.Get(service), out place);
        var role = InteractableRoles.Read(listener);
        if (role == InteractableRole.Shrine)
            return Place(ExplorationCategory.Shrine, InteractableRoles.Name(InteractableRole.Shrine, listener, Tag(listener)), out place);
        if (role is InteractableRole.TimeRift or InteractableRole.Nemesis)
            return Place(role == InteractableRole.TimeRift ? ExplorationCategory.Portal : ExplorationCategory.Interactable,
                InteractableRoles.Name(role, Tag(listener)), out place);
        if (WorldCueIdentityReader.ReadKind(listener) == NaturalCueKind.Chest)
            return Place(ExplorationCategory.Treasure, InteractableRoles.Name(InteractableRole.Cache, Tag(listener)), out place);
        if (WorldServiceReader.IsNpc(listener))
            return Place(IsCharacter(listener) ? ExplorationCategory.Npc : ExplorationCategory.Interactable,
                NpcNameReader.Read(listener, out _), out place);
        if (service is not null) return Place(ExplorationCategory.Interactable, ModText.Get(service), out place);
        string label = LandmarkProbe.ReadLabel(listener, listener.gameObject.scene, out bool portal, out bool transition);
        if (transition && !string.IsNullOrWhiteSpace(label))
        {
            place = new ExplorationPlace(ExplorationCategory.Exit, ModText.Format("guidance.exit_to", label), label,
                Destination(listener));
            return true;
        }
        if (UntaggedTransitions.ReadDestination(listener) is { } scene &&
            UntaggedTransitions.SceneName(scene) is { } destination && !string.IsNullOrWhiteSpace(destination))
        {
            place = new ExplorationPlace(ExplorationCategory.Exit, ModText.Format("guidance.exit_to", destination),
                destination, scene);
            return true;
        }
        if (portal) return Place(ExplorationCategory.Portal, label, out place);
        var tag = Tag(listener);
        var category = IsAlive(tag) && tag.hoverTargetType is HoverTagOnMouseOver.HoverTargetType.SAME_SCENE_TRANSITION or
            HoverTagOnMouseOver.HoverTargetType.CONTINUE_ECHO_CHAIN ? ExplorationCategory.Exit : ExplorationCategory.Interactable;
        return Place(category, HoverName(listener), out place);
    }

    // Ground loot has its own loops and the item category; its listeners are never places.
    private static bool IsLoot(WorldObjectClickListener listener) =>
        listener.GetComponent<GroundGoldVisuals>() is not null || listener.GetComponent<GroundItemVisuals>() is not null ||
        listener.GetComponent<GroundPotionVisuals>() is not null || listener.GetComponent<GroundXPTomeVisuals>() is not null ||
        listener.GetComponent<GroundFavorTomeVisuals>() is not null;

    private static bool Place(ExplorationCategory category, string name, out ExplorationPlace place)
    {
        place = new ExplorationPlace(category, name, name, null);
        return !string.IsNullOrWhiteSpace(name);
    }

    // NPCs have an NPC interaction or a body; notes and journals open a conversation with only a name tooltip.
    private static bool IsCharacter(WorldObjectClickListener listener)
    {
        if (WorldServiceReader.IsReadable(listener)) return false;
        var relay = listener._NPCinteraction;
        if (IsAlive(relay) && relay.isActiveAndEnabled) return true;
        var npc = listener.GetComponent<NPCInteraction>();
        if (IsAlive(npc) && npc.isActiveAndEnabled) return true;
        return IsAlive(listener.GetComponentInParent<Actor>()) || IsAlive(listener.GetComponentInParent<ActorVisuals>());
    }

    private static HoverTagOnMouseOver Tag(WorldObjectClickListener listener)
    {
        var tag = listener.hoverTagOnMouseOver;
        if (!IsAlive(tag)) tag = listener.GetComponent<HoverTagOnMouseOver>();
        return IsAlive(tag) && tag.isActiveAndEnabled ? tag : null;
    }

    // The label the game shows on hover, localized when it carries a key.
    private static string HoverName(WorldObjectClickListener listener) =>
        InteractableRoles.Name(InteractableRole.None, Tag(listener));

    private static string Destination(WorldObjectClickListener listener)
    {
        var tag = Tag(listener);
        if (tag is null) return null;
        TransitionOrGateInteraction transition = tag.hoverTargetType switch
        {
            HoverTagOnMouseOver.HoverTargetType.ZONE_TRANSITION => tag.sceneInteraction,
            HoverTagOnMouseOver.HoverTargetType.DUNGEON_TRANSITION => tag.dungeonTransitionInteraction,
            _ => null
        };
        return IsAlive(transition) ? transition.sceneName : null;
    }
}
