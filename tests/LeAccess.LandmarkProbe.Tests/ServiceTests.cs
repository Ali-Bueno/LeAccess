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
        Check(LandmarkProbe.TryRead(out result) && result.Name == LeAccess.I18n.ModText.Get("service.mtx_shop"), "Cosmetic shop is named as such, not as an equipment merchant");
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
        altar.gameObject.Components.Clear();
        var shrineRoot = new GameObject();
        altar.gameObject.transform.parent = shrineRoot.transform;
        ShrinesManager.Scenes["Z10"] = new ShrinesManager();
        ShrinesManager.Scenes["Z10"].Objects.Items.Add(shrineRoot);
        var named = SceneManager.Active; named.name = "Z10"; SceneManager.Active = named;
        altar.gameObject.scene = named;
        LeAccess.Accessibility.WorldCues.ShrineObjects.Reset();
        Check(LandmarkProbe.TryRead(out result) && result.Name == "santuario" && LandmarkProbe.LastStatus.Contains("role:Shrine"),
            "A shrine object its manager placed is named without a shrine component on the listener");
        ShrinesManager.Scenes.Clear(); LeAccess.Accessibility.WorldCues.ShrineObjects.Reset();
        Check(!LandmarkProbe.TryRead(out _), "Without the manager's shrine the same listener stays unnamed");
        Add(shrineRoot, new ShrineVisualsCreator());
        Check(LandmarkProbe.TryRead(out result) && result.Name == "santuario", "The shrine's visuals creator names its listener");
        altar.Allowed = false;
        Check(!LandmarkProbe.TryRead(out _), "A role never bypasses native availability");
        // A note shares the NPC interaction but has no character model: the landmark names it from its dialogue.
        WorldObjectClickListener.allListener = new();
        var noteGo = new GameObject();
        var note = Add(noteGo, new WorldObjectClickListener { interactionRange = 2, DialogueName = "Nota ajada" });
        note._NPCinteraction = Add(noteGo, new RelayNPCInteraction());
        note.transform.position = new Vector3 { x = 1 };
        WorldObjectClickListener.allListener.Items.Add(note);
        Check(LandmarkProbe.TryRead(out var noteResult) && noteResult.Name == note.DialogueName &&
            LandmarkProbe.LastStatus.Contains("readable"), "A note is announced by its dialogue name");
        var model = new GameObject(); model.transform.parent = noteGo.transform; noteGo.Children.Add(model);
        Add(model, new UnityEngine.SkinnedMeshRenderer());
        Check(!LandmarkProbe.TryRead(out _), "An NPC with a model is left to the NPC announcements");

        // Generated caves: the entrance and exits carry no hover tag, only the scene they lead to.
        WorldObjectClickListener.allListener = new();
        var entranceGo = new GameObject();
        var entrance = Add(entranceGo, new WorldObjectClickListener { interactionRange = 4 });
        entrance.transform.position = new Vector3 { x = 1 };
        WorldObjectClickListener.allListener.Items.Add(entrance);
        var spawn = Add(entranceGo, new UsePlayerSpawnInteraction { ToScene = "PCG_Cave" });
        Check(!LandmarkProbe.TryRead(out _), "A cave entrance to a scene without a game name stays unnamed");
        SceneList.Scenes["PCG_Cave"] = new SceneDetails { LocalizedName = "Cueva" };
        Check(LandmarkProbe.TryRead(out result) && result.Name == "Cueva" &&
            LandmarkProbe.LastStatus.Contains("untagged_destination:PCG_Cave"), "A cave entrance is named after the cave");
        spawn.ToScene = null; spawn.sceneName = "PCG_Cave";
        Check(LandmarkProbe.TryRead(out result) && result.Name == "Cueva", "The base transition scene is the fallback");
        spawn.enabled = false;
        Check(!LandmarkProbe.TryRead(out _), "A disabled transition names nothing");
        entranceGo.Components.Remove(spawn);
        var lobbyExit = Add(entranceGo, new CaveLobbyExitInteraction());
        Check(!LandmarkProbe.TryRead(out _), "A lobby exit outside a generated level stays unnamed");
        LE.PCG.LevelUtility.Levels[SceneManager.Active.handle] = new LE.PCG.Level
            { ParentScene = new Scene { handle = 30, isLoaded = true, name = "B1S30" } };
        SceneList.Scenes["B1S30"] = new SceneDetails { LocalizedName = "Túnel abandonado" };
        Check(LandmarkProbe.TryRead(out result) && result.Name == "Túnel abandonado",
            "The cave lobby exit is named after the scene the cave was generated from");
        lobbyExit.enabled = false;
        Check(!LandmarkProbe.TryRead(out _), "A disabled lobby exit names nothing");
        LE.PCG.LevelUtility.Levels.Clear();
        return checks;
    }
}
