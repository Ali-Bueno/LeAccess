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

// Found is the bare name for "the radar has logged a new ..."; Destination the scene an exit leads to;
// ListsOnScreen lists the place on sight whatever its category.
internal readonly record struct ExplorationPlace(ExplorationCategory Category, string Name, string Found, string Destination,
    bool ListsOnScreen = false);

// Files a click listener under the D2R scanner's categories (d2r-access native_entities.cpp ObjectCategory):
// the specific ones before the general, levers, notes and other objects into the catch-all.
internal static class ExplorationClassifier
{
    internal static bool TryDescribe(WorldObjectClickListener listener, out ExplorationPlace place)
    {
        place = default;
        if (!IsAlive(listener) || !listener.isActiveAndEnabled || !IsAlive(listener.gameObject) ||
            !listener.gameObject.activeInHierarchy || !listener.gameObject.scene.IsValid() ||
            !listener.gameObject.scene.isLoaded || !listener.CanInteract() || GroundLootListener.Owns(listener)) return false;
        string service = WorldServiceReader.ReadServiceKey(listener);
        // The stash and waypoints also pass the NPC test.
        if (service == "service.stash") return Place(listener, ExplorationCategory.Stash, ModText.Get(service), out place);
        if (service == "service.waypoint") return Place(listener, ExplorationCategory.Waypoint, ModText.Get(service), out place);
        var role = InteractableRoles.Read(listener);
        if (role == InteractableRole.Shrine)
            return Place(listener, ExplorationCategory.Shrine, InteractableRoles.Name(InteractableRole.Shrine, listener, Tag(listener), ShrineBuffNames.Of), out place);
        // Roled objects are listed when on screen, as shrines are.
        if (role is not (InteractableRole.None or InteractableRole.Cache))
            return Place(listener, InteractableRoles.IsTravel(role) ? ExplorationCategory.Portal : ExplorationCategory.Interactable,
                InteractableRoles.Name(role, listener, Tag(listener)), out place, onScreen: true);
        if (WorldCueIdentityReader.ReadKind(listener) == NaturalCueKind.Chest || InteractableNames.IsChestObject(listener.gameObject.name))
            return Place(listener, ExplorationCategory.Treasure, InteractableRoles.Name(InteractableRole.Cache, Tag(listener)), out place);
        if (WorldServiceReader.IsNpc(listener))
            return Place(listener, IsCharacter(listener) ? ExplorationCategory.Npc : ExplorationCategory.Interactable,
                NpcNameReader.Read(listener, out _), out place);
        if (service is not null) return Place(listener, ExplorationCategory.Interactable, ModText.Get(service), out place);
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
        if (portal) return Place(listener, ExplorationCategory.Portal, label, out place);
        // A world-event trigger area is a radius the player walks into, not something to click.
        if (IsAlive(listener.GetComponentInParent<WorldEventTriggerArea>()) && string.IsNullOrWhiteSpace(HoverName(listener))) return false;
        var tag = Tag(listener);
        var category = IsAlive(tag) && tag.hoverTargetType is HoverTagOnMouseOver.HoverTargetType.SAME_SCENE_TRANSITION or
            HoverTagOnMouseOver.HoverTargetType.CONTINUE_ECHO_CHAIN ? ExplorationCategory.Exit : ExplorationCategory.Interactable;
        return Place(listener, category, HoverName(listener), out place);
    }

    // Objects not yet known by name; each distinct GameObject name is logged once so it can be named later.
    private static readonly HashSet<string> LoggedUnnamed = new();

    // An unnamed object is still listed, under the generic name; other unnamed places stay out.
    private static bool Place(WorldObjectClickListener listener, ExplorationCategory category, string name,
        out ExplorationPlace place, bool onScreen = false)
    {
        if (string.IsNullOrWhiteSpace(name) && category == ExplorationCategory.Interactable)
        {
            if (LoggedUnnamed.Add(listener.gameObject.name))
                Plugin.Log.LogInfo($"[Exploration] unnamed interactable: {listener.gameObject.name} (parent {listener.transform.parent?.name})");
            name = ModText.Get("exploration.interactable");
        }
        place = new ExplorationPlace(category, name, name, null, onScreen);
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
