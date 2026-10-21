using System.Collections.Generic;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Npcs;

internal static class WorldServiceReader
{
    // Listeners known to own a character model; a model never leaves, but one may load after its listener.
    private static readonly HashSet<int> Modelled = new();

    /// <summary>A conversation object without a character model: notes, lore records, paintings.</summary>
    internal static bool IsReadable(WorldObjectClickListener listener) =>
        IsNpc(listener) && !HasCharacterModel(listener) && ReadServiceKey(listener) is null;

    /// <summary>An NPC with a character model; readable objects share the NPC interaction but are not characters.</summary>
    internal static bool IsCharacterNpc(WorldObjectClickListener listener) => IsNpc(listener) && !IsReadable(listener);

    // NPCs and examinable bodies carry a skinned model under their listener; notes do not (reference/natural-world-sounds.md).
    private static bool HasCharacterModel(WorldObjectClickListener listener)
    {
        int id = listener.GetInstanceID();
        if (Modelled.Contains(id)) return true;
        var model = listener.GetComponentInChildren<SkinnedMeshRenderer>(true);
        // A listener on a collider child has its model beside it, under the relay's or the named NPC's root.
        var relay = IsAlive(model) ? null : listener._NPCinteraction;
        if (IsAlive(relay)) model = relay.GetComponentInChildren<SkinnedMeshRenderer>(true);
        var owner = IsAlive(model) ? null : listener.GetComponentInParent<NpcNameTooltip>();
        if (IsAlive(owner)) model = owner.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (!IsAlive(model)) return false;
        Modelled.Add(id);
        return true;
    }

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
        return menu.menu == OpenMenuInteraction.MenuToOpen.Map ? HasWaypoint(root, listener) ? "service.waypoint" : null
            : MenuServiceKey(menu.menu);
    }

    /// <summary>The service a menu object opens, named with the game's own terms; null for menus with no service name.</summary>
    internal static string MenuServiceKey(OpenMenuInteraction.MenuToOpen menu) => menu switch
    {
        OpenMenuInteraction.MenuToOpen.CraftingPanel => "service.forge",
        OpenMenuInteraction.MenuToOpen.SetShattering or OpenMenuInteraction.MenuToOpen.RandomiseLegendaryPotential or
            OpenMenuInteraction.MenuToOpen.ExperimentalAffixCrafting or OpenMenuInteraction.MenuToOpen.AttributeSwap or
            OpenMenuInteraction.MenuToOpen.RerollUnique or OpenMenuInteraction.MenuToOpen.IdolEnchantment => "service.crafting",
        OpenMenuInteraction.MenuToOpen.MasteryPanel => "service.mastery",
        OpenMenuInteraction.MenuToOpen.ArenaEnterPanel => "service.arena",
        OpenMenuInteraction.MenuToOpen.Dungeon1EnterPanel or OpenMenuInteraction.MenuToOpen.LightlessArborDungeonEnterPanel or
            OpenMenuInteraction.MenuToOpen.SoulfireBastionDungeonEnterPanel => "service.dungeon",
        OpenMenuInteraction.MenuToOpen.VaultsOfUncertainFatePanel => "service.vault",
        OpenMenuInteraction.MenuToOpen.SanctuaryOfEterraPanel => "service.sanctuary",
        OpenMenuInteraction.MenuToOpen.FactionPanel or OpenMenuInteraction.MenuToOpen.FactionIntroduction => "service.faction",
        OpenMenuInteraction.MenuToOpen.FactionMainCoF or OpenMenuInteraction.MenuToOpen.FactionMainCoFRanks => "service.circle_of_fortune",
        OpenMenuInteraction.MenuToOpen.FactionMainMG or OpenMenuInteraction.MenuToOpen.FactionMGMyStall => "service.merchants_guild",
        OpenMenuInteraction.MenuToOpen.FactionMainFK => "service.forgotten_knights",
        OpenMenuInteraction.MenuToOpen.FactionMainTW => "service.the_woven",
        OpenMenuInteraction.MenuToOpen.ObservatoryNorth or OpenMenuInteraction.MenuToOpen.ObservatorySouth or
            OpenMenuInteraction.MenuToOpen.ObservatoryEast or OpenMenuInteraction.MenuToOpen.ObservatoryWest => "service.observatory",
        OpenMenuInteraction.MenuToOpen.CircleOfFortuneGambler or OpenMenuInteraction.MenuToOpen.MerchantsGuildGambler => "service.gambler",
        OpenMenuInteraction.MenuToOpen.TheWeaverVendor => "service.weaver",
        OpenMenuInteraction.MenuToOpen.HunterVendor => "service.hunter_vendor",
        OpenMenuInteraction.MenuToOpen.TimeglassVendor => "service.timeglass_vendor",
        OpenMenuInteraction.MenuToOpen.WovenEchoExchange => "service.woven_echo_exchange",
        OpenMenuInteraction.MenuToOpen.MtxShop => "service.mtx_shop",
        _ => null
    };

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
