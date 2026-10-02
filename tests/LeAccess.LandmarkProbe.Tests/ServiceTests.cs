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
        Check(LandmarkProbe.TryRead(out var result) && result.Name == "Punto de viaje",
            "Z32 Map interaction with owned EnableWaypoint child and no HoverTag is announced");
        Check(LandmarkProbe.LastStatus.Contains("service.waypoint"), "Service source is available in bounded landmark diagnostics");
        var childListener = Add(activator, new WorldObjectClickListener());
        Check(!LandmarkProbe.TryRead(out _), "Nested listener's waypoint does not label the parent map");
        activator.Components.Remove(childListener);
        activator.activeInHierarchy = false;
        Check(LandmarkProbe.TryRead(out result) && result.Name == "Punto de viaje",
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
        Check(LandmarkProbe.TryRead(out result) && result.Name == "Alijo", "Native OpenStash.OnUse target announced without tag");
        stash.isActiveAndEnabled = false;
        Check(!LandmarkProbe.TryRead(out _), "Inactive direct stash action is ignored");
        go.Components.Remove(stash);
        menu.menu = OpenMenuInteraction.MenuToOpen.CraftingPanel;
        Check(LandmarkProbe.TryRead(out result) && result.Name == "Forja", "Typed crafting interaction announced without tag");
        menu.menu = OpenMenuInteraction.MenuToOpen.MtxShop;
        Check(!LandmarkProbe.TryRead(out _), "Cosmetic shop is not mistaken for an equipment merchant");
        var shop = Add(go, new OpenShop { enabled = false });
        Check(LandmarkProbe.TryRead(out result) && result.Name == "Mercader", "Typed shop without an NPC has one landmark announcement");
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
        return checks;
    }
}
