using LeAccess.Accessibility.Npcs;
using UnityEngine;

internal static class ServiceTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool value, string name) { checks++; if (!value) throw new Exception(name); }
        var go = new GameObject();
        var listener = go.Add(new WorldObjectClickListener());
        var tooltip = go.Add(new NpcNameTooltip { TopText = "Isfarger", BottomText = "Merchant" });
        Check(NpcNameReader.Read(listener, out _) == "Isfarger. " + tooltip.BottomText,
            "Native NpcNameTooltip.OnPointerEnter renders both authored text fields");
        tooltip.BottomTextKey = "npc.role";
        Localization.Translations["npc.role"] = "Mercader";
        Check(NpcNameReader.Read(listener, out _) == "Isfarger. " + Localization.Translations["npc.role"], "Native subtitle uses its own localization key");
        tooltip.BottomTextKey = null;
        tooltip.BottomText = "Isfarger";
        Check(NpcNameReader.Read(listener, out _) == "Isfarger", "Duplicate title does not repeat name");
        tooltip.BottomText = null;
        var relay = go.Add(new RelayNPCInteraction());
        listener._NPCinteraction = relay;
        var interactionGo = go.Child();
        var condition = interactionGo.Add(new ConditionHandler());
        var shop = interactionGo.Add(new OpenShop { isActiveAndEnabled = false });
        Check(WorldServiceReader.ReadServiceKey(listener) is null, "An arbitrary shop descendant does not identify a service");
        relay.otherInteractables.Add(condition);
        Check(NpcNameReader.Read(listener, out var source) == "Isfarger. " + LeAccess.I18n.ModText.Get("service.merchant") && source.Contains("native service"),
            "Z32 Isfarger relay references its exact ConditionHandler GO with disabled OpenShop");
        tooltip.BottomText = "Mercader";
        Check(NpcNameReader.Read(listener, out _) == "Isfarger. " + LeAccess.I18n.ModText.Get("service.merchant"), "Authored role and native service do not duplicate speech");
        tooltip.BottomText = null;
        condition.isActiveAndEnabled = false;
        Check(WorldServiceReader.ReadServiceKey(listener) is null, "Inactive relay branch cannot claim merchant");
        condition.isActiveAndEnabled = true;
        interactionGo.activeInHierarchy = false;
        Check(WorldServiceReader.ReadServiceKey(listener) is null, "Inactive phase GO cannot claim merchant");
        interactionGo.activeInHierarchy = true;
        var nestedOwner = interactionGo.Add(new WorldObjectClickListener());
        Check(WorldServiceReader.ReadServiceKey(listener) is null, "Another nested listener owns its own shop");
        interactionGo.Components.Remove(nestedOwner);
        shop.WasCollected = true;
        Check(WorldServiceReader.ReadServiceKey(listener) is null, "Collected shop metadata is ignored");
        shop.WasCollected = false;
        relay.otherInteractables.WasCollected = true;
        Check(WorldServiceReader.ReadServiceKey(listener) is null, "Collected relay registry is not read");
        relay.otherInteractables.WasCollected = false;
        listener._NPCinteraction = null;
        Check(WorldServiceReader.ReadServiceKey(listener) == "service.merchant", "Authored relay works before listener cache initialization");
        var foreign = new GameObject();
        var foreignListener = foreign.Add(new WorldObjectClickListener());
        var foreignRelay = foreign.Add(new RelayNPCInteraction());
        foreign.Add(new OpenShop());
        listener._NPCinteraction = foreignRelay;
        Check(WorldServiceReader.ReadServiceKey(listener) is null, "Foreign cached relay cannot supply another NPC's role");
        listener._NPCinteraction = relay;
        var secondGo = go.Child();
        var second = secondGo.Add(new ConditionHandler());
        secondGo.Add(new OpenMenuInteraction { menu = OpenMenuInteraction.MenuToOpen.CraftingPanel });
        relay.otherInteractables.Add(second);
        Check(WorldServiceReader.ReadServiceKey(listener) is null, "Conflicting live service branches are not guessed");
        relay.otherInteractables.Remove(second);
        tooltip.TopText = "";
        Check(NpcNameReader.Read(listener, out _) == LeAccess.I18n.ModText.Get("service.merchant"), "A proven service remains useful if NPC localization is unavailable");
        relay.isActiveAndEnabled = false;
        Check(WorldServiceReader.ReadServiceKey(listener) is null, "Disabled relay does not supply service metadata");
        relay.isActiveAndEnabled = true;
        // Menu objects name the service they open; the no-op Stash menu stays unnamed.
        var menuGo = new GameObject();
        var menuListener = menuGo.Add(new WorldObjectClickListener());
        var menus = menuGo.Add(new OpenMenuInteraction());
        var kinds = new Dictionary<string, List<OpenMenuInteraction.MenuToOpen>>();
        foreach (OpenMenuInteraction.MenuToOpen each in Enum.GetValues(typeof(OpenMenuInteraction.MenuToOpen)))
        {
            string key = WorldServiceReader.MenuServiceKey(each);
            if (key is null) continue;
            if (!kinds.TryGetValue(key, out var group)) kinds[key] = group = new();
            group.Add(each);
        }
        Check(WorldServiceReader.MenuServiceKey(OpenMenuInteraction.MenuToOpen.ArenaEnterPanel) == "service.arena" &&
            WorldServiceReader.MenuServiceKey(OpenMenuInteraction.MenuToOpen.TheWeaverVendor) == "service.weaver" &&
            WorldServiceReader.MenuServiceKey(OpenMenuInteraction.MenuToOpen.CraftingPanel) == "service.forge" &&
            WorldServiceReader.MenuServiceKey(OpenMenuInteraction.MenuToOpen.IdolEnchantment) == "service.crafting",
            "Menu values map to their service keys");
        Check(kinds["service.dungeon"].Count == 3 && kinds["service.observatory"].Count == 4 && kinds["service.gambler"].Count == 2,
            "Dungeon, observatory and gambler menus share one key each");
        Check(WorldServiceReader.MenuServiceKey(OpenMenuInteraction.MenuToOpen.Stash) is null &&
            WorldServiceReader.MenuServiceKey(OpenMenuInteraction.MenuToOpen.MtxShop) == "service.mtx_shop" &&
            WorldServiceReader.MenuServiceKey(OpenMenuInteraction.MenuToOpen.Map) is null, "The stash and map menus have no service name; the cosmetic shop has its own");
        menus.menu = OpenMenuInteraction.MenuToOpen.SanctuaryOfEterraPanel;
        Check(WorldServiceReader.ReadServiceKey(menuListener) == "service.sanctuary", "A menu object on the listener names its service");
        return checks;
    }
}
