using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Npcs;

internal static class WorldServiceReader
{
    internal static string ReadServiceKey(WorldObjectClickListener listener)
    {
        if (!IsAlive(listener) || !listener.isActiveAndEnabled) return null;
        string service = ReadScope(listener, listener);
        var relay = listener._NPCinteraction;
        if (!IsAlive(relay)) relay = listener.GetComponent<RelayNPCInteraction>();
        if (!Owned(relay, listener) || !relay.isActiveAndEnabled) return service;
        var interactions = relay.otherInteractables;
        if (interactions is null || interactions.WasCollected) return service;
        foreach (var interaction in interactions)
        {
            if (!Owned(interaction, listener) || !interaction.isActiveAndEnabled) continue;
            string candidate = ReadScope(interaction, listener);
            if (candidate is null) continue;
            if (service is not null && service != candidate) return null;
            service = candidate;
        }
        return service;
    }

    internal static bool IsNpc(WorldObjectClickListener listener)
    {
        var relay = listener._NPCinteraction;
        if (Owned(relay, listener) && relay.isActiveAndEnabled) return true;
        var npc = listener.GetComponent<NPCInteraction>();
        if (IsAlive(npc) && npc.isActiveAndEnabled) return true;
        var name = listener.GetComponent<NpcNameTooltip>();
        var conversation = listener._leConversationTrigger;
        return IsAlive(name) && name.isActiveAndEnabled && IsAlive(conversation) && conversation.isActiveAndEnabled;
    }

    private static string ReadScope(Component root, WorldObjectClickListener listener)
    {
        // OpenShop disables itself after opening; enabled is not a service-identity flag.
        // Only this exact interaction GO is inspected, never unrelated children.
        if (Owned(root.GetComponent<OpenShop>(), listener)) return "service.merchant";
        var stash = root.GetComponent<OpenStash>();
        if (Owned(stash, listener) && stash.isActiveAndEnabled) return "service.stash";
        var menu = root.GetComponent<OpenMenuInteraction>();
        if (!Owned(menu, listener) || !menu.isActiveAndEnabled) return null;
        return menu.menu switch
        {
            OpenMenuInteraction.MenuToOpen.CraftingPanel => "service.forge",
            OpenMenuInteraction.MenuToOpen.Map when HasWaypoint(root, listener) => "service.waypoint",
            _ => null
        };
    }

    private static bool HasWaypoint(Component root, WorldObjectClickListener listener)
    {
        var waypoints = root.GetComponentsInChildren<EnableWaypointInteraction>(true);
        if (waypoints is null) return false;
        foreach (var waypoint in waypoints)
            if (Owned(waypoint, listener, requireActive: false)) return true;
        return false;
    }

    private static bool Owned(Component component, WorldObjectClickListener listener, bool requireActive = true)
    {
        if (!IsAlive(component) || requireActive && !component.gameObject.activeInHierarchy) return false;
        var owner = component.GetComponentInParent<WorldObjectClickListener>(true);
        return IsAlive(owner) && owner.GetInstanceID() == listener.GetInstanceID();
    }
}
