using EHG.Multiplayer;
using LeAccess.Accessibility.Landmarks;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class ServiceTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool value, string name) { checks++; if (!value) throw new Exception(name); }
        T Add<T>(GameObject go, T component) where T : Component
        { component.gameObject = go; go.Components.Add(component); return component; }
        SceneManager.Active = new Scene { handle = 20, isLoaded = true };
        GameplayEnvironment.IsOnlineClient = false;
        var actor = new Actor();
        PlayerFinder.player = actor.gameObject;
        PlayerFinder.playerActorComponent = actor;
        WorldObjectClickListener.allListener = new();
        var go = new GameObject();
        var listener = Add(go, new WorldObjectClickListener { interactionRange = 2 });
        listener.transform.position = new Vector3 { x = 1 };
        WorldObjectClickListener.allListener.Items.Add(listener);
        var menu = Add(go, new OpenMenuInteraction { menu = OpenMenuInteraction.MenuToOpen.Map });
        Check(!LandmarkProbe.TryRead(out _), "Generic map interaction alone is not a waypoint");
        var activator = new GameObject();
        activator.transform.parent = go.transform;
        go.Children.Add(activator);
        var waypoint = Add(activator, new EnableWaypointInteraction());
        Check(LandmarkProbe.TryRead(out var result) && result.Name == LeAccess.I18n.ModText.Get("service.waypoint"),
            "Z32 Map interaction with owned EnableWaypoint child and no HoverTag is announced");
        Check(LandmarkProbe.LastStatus.Contains("service.waypoint"), "Service source is available in bounded landmark diagnostics");
        var childListener = Add(activator, new WorldObjectClickListener());
        Check(!LandmarkProbe.TryRead(out _), "Nested listener's waypoint does not label the parent map");
        activator.Components.Remove(childListener);
        activator.activeInHierarchy = false;
        Check(LandmarkProbe.TryRead(out result) && result.Name == LeAccess.I18n.ModText.Get("service.waypoint"),
            "Owned waypoint identity survives an inactive activation trigger after discovery");
        activator.activeInHierarchy = true;
        waypoint.WasCollected = true;
        Check(!LandmarkProbe.TryRead(out _), "Collected waypoint marker is ignored");
        waypoint.WasCollected = false;
        menu.isActiveAndEnabled = false;
        Check(!LandmarkProbe.TryRead(out _), "Disabled open-menu interaction is ignored");
        menu.isActiveAndEnabled = true;
        listener.Allowed = false;
        Check(!LandmarkProbe.TryRead(out _), "Native CanInteract gates services without labels");
        listener.Allowed = true;
        listener.transform.position = new Vector3 { x = 3 };
        Check(!LandmarkProbe.TryRead(out _), "Native waypoint range is not expanded");
        listener.transform.position = new Vector3 { x = 1 };
        menu.menu = OpenMenuInteraction.MenuToOpen.Stash;
        Check(!LandmarkProbe.TryRead(out _), "Current native OpenMenu Stash enum is a no-op and not a service");
        var stash = Add(go, new OpenStash());
        Check(LandmarkProbe.TryRead(out result) && result.Name == LeAccess.I18n.ModText.Get("service.stash"), "Native OpenStash.OnUse target announced without tag");
        stash.isActiveAndEnabled = false;
        Check(!LandmarkProbe.TryRead(out _), "Inactive direct stash action is ignored");
        go.Components.Remove(stash);
        menu.menu = OpenMenuInteraction.MenuToOpen.CraftingPanel;
        Check(LandmarkProbe.TryRead(out result) && result.Name == LeAccess.I18n.ModText.Get("service.forge"), "Typed crafting interaction announced without tag");
        menu.menu = OpenMenuInteraction.MenuToOpen.MtxShop;
        Check(!LandmarkProbe.TryRead(out _), "Cosmetic shop is not mistaken for an equipment merchant");
        var shop = Add(go, new OpenShop { enabled = false });
        Check(LandmarkProbe.TryRead(out result) && result.Name == LeAccess.I18n.ModText.Get("service.merchant"), "Typed shop without an NPC has one landmark announcement");
        var npc = Add(go, new NPCInteraction());
        Check(!LandmarkProbe.TryRead(out _), "NPC sound and name reader own merchant speech without duplicate landmarks");
        go.Components.Remove(npc);
        var relay = Add(go, new RelayNPCInteraction());
        listener._NPCinteraction = relay;
        Check(!LandmarkProbe.TryRead(out _), "Cached NPC relay also excludes duplicate service landmarks");
        relay.isActiveAndEnabled = false;
        listener._NPCinteraction = null;
        go.Components.Remove(relay);
        go.scene = new Scene { handle = 21, isLoaded = true };
        Check(!LandmarkProbe.TryRead(out _), "Stale-scene service listener is rejected");
        go.scene = SceneManager.Active;
        listener.WasCollected = true;
        Check(!LandmarkProbe.TryRead(out _), "Collected service listener is rejected");
        listener.WasCollected = false;
        go.activeInHierarchy = false;
        Check(!LandmarkProbe.TryRead(out _), "Inactive service listener is rejected");

        // Shrines, time rifts and the Nemesis altar: generic or key-only tags still yield a name.
        WorldObjectClickListener.allListener = new();
        var altar = Add(new GameObject(), new WorldObjectClickListener { interactionRange = 3 });
        altar.transform.position = new Vector3 { x = 1 };
        WorldObjectClickListener.allListener.Items.Add(altar);
        Check(!LandmarkProbe.TryRead(out _), "A plain interactable without a tag stays unnamed");
        var role = Add(altar.gameObject, new Shrine());
        Check(LandmarkProbe.TryRead(out result) && result.Name == "santuario", "A shrine without a tag gets the mod name");
        altar.hoverTagOnMouseOver = new HoverTagOnMouseOver { hoverTargetType = HoverTagOnMouseOver.HoverTargetType.LOCALIZATION_KEY,
            localizationKey = "shrine_key" };
        Check(LandmarkProbe.TryRead(out result) && result.Name == "santuario", "An unresolved key falls back to the mod name");
        Localization.Texts["shrine_key"] = "Santuario de fuego";
        Check(LandmarkProbe.TryRead(out result) && result.Name == "Santuario de fuego",
            "A key-only tag resolves through the game's localization");
        altar.gameObject.Components.Remove(role);
        Check(!LandmarkProbe.TryRead(out _), "The same key-only tag without a recognized role stays unnamed");
        Add(altar.gameObject, new TimeRiftInteraction());
        Check(LandmarkProbe.TryRead(out result) && result.Name == "Santuario de fuego", "A time rift uses the game's text");
        Localization.Texts.Clear();
        Check(LandmarkProbe.TryRead(out result) && result.Name == "fisura temporal", "A time rift falls back to its mod name");
        altar.gameObject.Components.Clear();
        Add(altar.gameObject, new NemesisEncounterInteractableObject());
        Check(LandmarkProbe.TryRead(out result) && result.Name == "Némesis", "The Nemesis altar falls back to its mod name");
        altar.Allowed = false;
        Check(!LandmarkProbe.TryRead(out _), "A role never bypasses native availability");
        return checks;
    }
}
