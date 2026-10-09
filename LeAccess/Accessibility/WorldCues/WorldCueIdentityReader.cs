using LE.Gameplay.Monolith;
using LE.Gameplay.PrimalHunt;
using LE.Gameplay.Tombs;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Npcs;
using Networking.Multiplayer.Interactables.Portals;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.WorldCues;

internal static class WorldCueIdentityReader
{
    internal static NaturalCueKind? ReadKind(WorldObjectClickListener listener)
    {
        if (!Available(listener)) return null;
        string service = WorldServiceReader.ReadServiceKey(listener);
        if (service == "service.stash") return NaturalCueKind.Stash;
        if (service == "service.waypoint") return NaturalCueKind.Portal;
        // Notes, lore records and paintings sound like a tome, not like the NPC whose interaction they share.
        if (WorldServiceReader.IsReadable(listener)) return NaturalCueKind.Tome;

        var portal = listener.GetComponentInParent<PortalVisualController>();
        if (Available(portal) && IsAlive(portal.mouseListenerObj) &&
            portal.mouseListenerObj.Pointer == listener.gameObject.Pointer && !portal.isNonInteractableByPlayer())
            return NaturalCueKind.Portal;

        var tag = listener.hoverTagOnMouseOver;
        if (!IsAlive(tag)) tag = listener.GetComponent<HoverTagOnMouseOver>();
        if (Available(tag) && tag.gameObject.Pointer == listener.gameObject.Pointer)
        {
            TransitionOrGateInteraction transition = tag.hoverTargetType switch
            {
                HoverTagOnMouseOver.HoverTargetType.ZONE_TRANSITION => tag.sceneInteraction,
                HoverTagOnMouseOver.HoverTargetType.DUNGEON_TRANSITION => tag.dungeonTransitionInteraction,
                _ => null
            };
            // Destination metadata may be inactive; the listener already proved availability.
            if (IsAlive(transition) && transition.gameObject.scene.handle == listener.gameObject.scene.handle &&
                !string.IsNullOrWhiteSpace(transition.sceneName)) return NaturalCueKind.ZoneTransition;
        }

        if (Available(listener.GetComponent<ChestSync>()) ||
            Available(listener.GetComponent<MonolithChestInteraction>()) ||
            Available(listener.GetComponent<TombRewardChestInteraction>()) ||
            Available(listener.GetComponent<CaveTreasureChest>())) return NaturalCueKind.Chest;
        var vault = listener.GetComponent<VaultsOfUncertainFateChest>();
        if (Available(vault)) return vault.hasBeenOpened ? null : NaturalCueKind.Chest;
        return InteractableRoles.Read(listener) switch
        {
            InteractableRole.Shrine => NaturalCueKind.Shrine,
            // Last Epoch's own time-rift recording is the Portal loop.
            InteractableRole.TimeRift => NaturalCueKind.Portal,
            InteractableRole.Nemesis => NaturalCueKind.Nemesis,
            InteractableRole.Cache => NaturalCueKind.Chest,
            _ => null
        };
    }

    private static bool Available(Behaviour component) => IsAlive(component) &&
        component.isActiveAndEnabled && IsAlive(component.gameObject) && component.gameObject.activeInHierarchy;
}
