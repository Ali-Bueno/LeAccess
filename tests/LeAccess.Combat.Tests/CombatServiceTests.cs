using BepInEx.Configuration;
using EHG.Multiplayer;
using LeAccess.Accessibility;
using LeAccess.Accessibility.Combat;
using LeAccess.Accessibility.Navigation;
using UnityEngine;

internal static class CombatServiceTests
{
    internal static void Run(Action<bool, string> check)
    {
        var friendly = new Alignment { Id = 100 };
        var hostile = new Alignment { Id = 200 };
        friendly.Foe = hostile;
        Actor ActorAt(int id, string name, float distance, Alignment alignment = null) => new()
        {
            Id = id, Alignment = alignment ?? hostile,
            transform = new Transform { position = new Vector3(distance, 0, 0) },
            Display = new BaseDisplayInformation { Name = name }
        };
        void Actors(params Actor[] entries) => ActorManager.instance = new ActorManager
        {
            actors = new NativeList<ActorManager.ActorList>(new ActorManager.ActorList
            { alignment = hostile, actors = new NativeList<Actor>(entries) })
        };
        ConfigFile Reset()
        {
            CombatAccessibility.Cleanup();
            var config = new ConfigFile(); CombatAccessibility.Initialize(config);
            ScreenReader.Messages.Clear(); LeAccess.Plugin.Log.Messages.Clear();
            ScreenReader.Available = true; ScreenReader.SpokeThisFrame = ScreenReader.Speaking = false;
            GameplayEnvironment.IsOnlineClient = false;
            PlayerFinder.playerActorComponent = ActorAt(1, "Player", 0, friendly);
            PlayerFinder.playerActorVisuals = null;
            ControllerTargetingParadigm._SkillTargetMaxDistance = 10;
            Time.unscaledTimeAsDouble = 0; SceneList.NonCombat = false;
            NavigationAudioState.SuspensionReason = null; GameplayInputState.Owns = true;
            LeAccess.Accessibility.ScreenContent.ScreenContentAnnouncer.HasActiveContent = false;
            LeAccess.Accessibility.Dialogue.DialogueAnnouncer.HasActiveContent = false;
            LeAccess.Accessibility.Inventory.InventoryAccessibility.HasActiveContent = false;
            LeAccess.Accessibility.Skills.SkillAccessibility.HasActiveContent = false;
            LeAccess.Accessibility.ControllerMenus.ControllerMenuAccessibility.HasActiveContent = false;
            LeAccess.Accessibility.Items.GroundLootAccessibility.HasPendingContent = false;
            LeAccess.Accessibility.Zones.ZoneAnnouncer.HasPendingContent = false;
            LeAccess.Accessibility.Npcs.NpcSounds.HasPendingName = false;
            Actors(ActorAt(2, "Esqueleto", 2));
            return config;
        }
        void Tick(double now) { Time.unscaledTimeAsDouble = now; ScreenReader.SpokeThisFrame = false; CombatAccessibility.Update(); }

        var config = Reset(); CombatAccessibility.Update();
        check(ScreenReader.Messages.Single() == ("Esqueleto", false), "Offline registry and player need no online visuals, aim or damage");
        check(LeAccess.Plugin.Log.Messages.Any(x => x == "[NearestEnemy] Esqueleto"), "Delivered combat speech leaves diagnostic evidence");
        check(LeAccess.Plugin.Log.Messages.Any(x => x.Contains("registry=offline actors") && x.Contains("in_range=1")), "Status distinguishes actual registry and nearby count");
        Tick(3); Tick(6);
        check(ScreenReader.Messages.Count == 1, "Polling does not repeat unchanged nearest enemy");

        var near = ActorAt(2, "Lobo", 1); var far = ActorAt(3, "Oso", 3);
        Reset(); Actors(near, far); near.Dying = true; CombatAccessibility.Update();
        check(ScreenReader.Messages.Single().Text == "Oso", "Dead nearest offline actor cannot hide a living hostile");
        near.Dying = false;
        near.gameObject.scene = new UnityEngine.SceneManagement.Scene { handle = 2, Unloaded = true };
        Tick(.21);
        check(ScreenReader.Messages.Count == 1, "Unloaded offline enemy cannot hide a living current enemy");
        near.gameObject.scene = far.gameObject.scene;
        near.gameObject.activeInHierarchy = false;
        Tick(.42);
        check(ScreenReader.Messages.Count == 1, "Inactive offline enemy owner cannot supply a name");
        near.gameObject.activeInHierarchy = true;
        near.Dying = false; near.InvalidTarget = true; Tick(3);
        check(ScreenReader.Messages.Count == 1, "Invalid offline target is excluded");
        near.InvalidTarget = false; near.Alignment = friendly; Tick(6);
        check(ScreenReader.Messages.Count == 1, "Changed alignment in stale hostile offline group is filtered");
        near.Alignment = hostile; near.ThrowOnAlignment = true; Tick(9);
        check(ScreenReader.Messages.Count == 1 && NearestEnemyProbe.LastFailure.Contains("broken actor"), "Bad offline entry is diagnosed without masking valid neighbor");
        near.ThrowOnAlignment = false; near.WasCollected = true; Tick(12);
        check(ScreenReader.Messages.Count == 1 && NearestEnemyProbe.LastFailure is null, "Collected offline entry is guarded and previous error clears");
        near.WasCollected = false; near.Display.Name = null; Tick(15);
        check(ScreenReader.Messages.Count == 1 && NearestEnemyProbe.LastStatus.Contains("nearest_name=unavailable"), "Unnamed nearest offline actor never borrows farther name");
        Reset();
        var staleGroup = new ActorManager.ActorList
        { WasCollected = true, alignment = hostile, actors = new NativeList<Actor>(near) };
        var validGroup = new ActorManager.ActorList { alignment = hostile, actors = new NativeList<Actor>(far) };
        ActorManager.instance.actors = new NativeList<ActorManager.ActorList>(null, staleGroup, validGroup);
        CombatAccessibility.Update();
        check(ScreenReader.Messages.Single().Text == "Oso", "Collected offline group does not hide valid neighboring group");
        staleGroup.WasCollected = false; staleGroup.actors.WasCollected = true; Tick(3);
        check(ScreenReader.Messages.Count == 1, "Collected offline collection is never indexed");
        ActorManager.instance.actors.WasCollected = true; Tick(6);
        check(ScreenReader.Messages.Count == 1, "Collected offline registry is never enumerated");

        Reset(); ScreenReader.Speaking = true; CombatAccessibility.Update();
        check(ScreenReader.Messages.Count == 1, "Stuck backend speaking state cannot permanently silence nearest enemy");
        Reset(); ScreenReader.SpokeThisFrame = true; CombatAccessibility.Update();
        check(ScreenReader.Messages.Count == 0, "Higher priority announcement owns its frame");
        Tick(1); check(ScreenReader.Messages.Count == 1, "Current candidate speaks after frame ownership clears");

        Reset();
        CombatAccessibility.Update(otherContentActive: true);
        check(ScreenReader.Messages.Count == 0, "Passive tree or quest journal ownership suppresses retained combat candidates");
        Actors(ActorAt(4, "Enemigo actual", 1));
        Tick(1);
        check(ScreenReader.Messages.Single().Text == "Enemigo actual", "Closing a new reader resumes only the current enemy");

        var gates = new (string Label, Action<bool> Set)[]
        {
            ("modal", v => LeAccess.Accessibility.ScreenContent.ScreenContentAnnouncer.HasActiveContent = v),
            ("dialogue", v => LeAccess.Accessibility.Dialogue.DialogueAnnouncer.HasActiveContent = v),
            ("inventory", v => LeAccess.Accessibility.Inventory.InventoryAccessibility.HasActiveContent = v),
            ("skills", v => LeAccess.Accessibility.Skills.SkillAccessibility.HasActiveContent = v),
            ("radial", v => LeAccess.Accessibility.ControllerMenus.ControllerMenuAccessibility.HasActiveContent = v),
            ("zone", v => LeAccess.Accessibility.Zones.ZoneAnnouncer.HasPendingContent = v),
            ("ground", v => LeAccess.Accessibility.Items.GroundLootAccessibility.HasPendingContent = v),
            ("NPC", v => LeAccess.Accessibility.Npcs.NpcSounds.HasPendingName = v),
            ("gameplay input", v => GameplayInputState.Owns = !v),
            ("loading", v => NavigationAudioState.SuspensionReason = v ? "scene loading" : null),
            ("reader", v => ScreenReader.Available = !v)
        };
        foreach (var gate in gates)
        {
            Reset(); gate.Set(true); CombatAccessibility.Update();
            check(ScreenReader.Messages.Count == 0, "Speech correctly yields to " + gate.Label);
            Actors(ActorAt(4, "Último enemigo", 1)); gate.Set(false); Tick(3);
            check(ScreenReader.Messages.Single().Text == "Último enemigo", "Only latest candidate speaks after " + gate.Label);
        }

        foreach (string mode in new[] { "disabled", "noncombat", "invalid range", "missing player", "dead player", "out of range" })
        {
            config = Reset();
            switch (mode)
            {
                case "disabled": config.Set("AnnounceNearestEnemy", false); break;
                case "noncombat": SceneList.NonCombat = true; break;
                case "invalid range": ControllerTargetingParadigm._SkillTargetMaxDistance = float.NaN; break;
                case "missing player": PlayerFinder.playerActorComponent = null; break;
                case "dead player": PlayerFinder.playerActorComponent.Dying = true; break;
                case "out of range": Actors(ActorAt(5, "Far", 11)); break;
            }
            CombatAccessibility.Update();
            check(ScreenReader.Messages.Count == 0, "No combat speech for " + mode);
            check(LeAccess.Plugin.Log.Messages.Count > 0, "Unavailable path leaves bounded diagnosis: " + mode);
        }

        foreach (bool online in new[] { false, true })
        {
            foreach (string unavailable in new[] { "unloaded player scene", "invalid player scene", "inactive owner", "collected owner", "unloaded scene", "invalid scene" })
            {
                Reset();
                GameplayEnvironment.IsOnlineClient = online;
                PlayerFinder.playerActorVisuals = new ActorVisuals { Id = 1, Alignment = friendly };
                var owner = online ? PlayerFinder.playerActorVisuals.gameObject : PlayerFinder.playerActorComponent.gameObject;
                var originalScene = UnityEngine.SceneManagement.SceneManager.Current;
                switch (unavailable)
                {
                    case "unloaded player scene": owner.scene = new() { handle = 2, Unloaded = true }; break;
                    case "invalid player scene": owner.scene = default; break;
                    case "inactive owner": owner.activeInHierarchy = false; break;
                    case "collected owner": owner.WasCollected = true; break;
                    case "unloaded scene": UnityEngine.SceneManagement.SceneManager.Current = new() { handle = 1, Unloaded = true }; break;
                    case "invalid scene": UnityEngine.SceneManagement.SceneManager.Current = default; break;
                }
                CombatAccessibility.Update();
                check(ScreenReader.Messages.Count == 0 && !LeAccess.Plugin.Log.Messages.Any(x => x.Contains("registry=")),
                    $"{(online ? "Online" : "Offline")} combat stops before probing with {unavailable}");
                UnityEngine.SceneManagement.SceneManager.Current = originalScene;
                owner.WasCollected = false;
            }
        }

        Reset();
        PlayerFinder.playerActorComponent.gameObject.scene = new() { handle = 2 };
        var additive = ActorAt(20, "Enemigo persistente", 2);
        additive.gameObject.scene = new() { handle = 3 };
        Actors(additive);
        CombatAccessibility.Update();
        check(ScreenReader.Messages.Single().Text == "Enemigo persistente",
            "Loaded persistent player and offline registry remain eligible across scene ownership");

        Reset();
        var separated = ActorAt(20,null,2);
        separated.Display=null;
        separated.data=new ActorData{actorName="Skeleton",LocalizedName="Esqueleto"};
        Actors(separated); CombatAccessibility.Update();
        check(ScreenReader.Messages.Single().Text=="Esqueleto" && separated.data.Reads==1,
            "Actual offline shape without actor display uses selected ActorData localization");
        check(LeAccess.Plugin.Log.Messages.Any(m=>m.Contains("name_source=actor_data")),"ActorData name source leaves actionable diagnosis");
        separated.data.LocalizedName="Skeleton";Tick(3);
        check(ScreenReader.Messages.Last().Text=="Skeleton" && ScreenReader.Messages.Count==2,"Same selected actor reads current game localization after language changes");

        Reset();
        var ownedVisuals=new ActorVisuals{Id=21,BaseDisplayInformation=new(){Name="Capitán esqueleto"}};
        separated.actorSync=new ActorSync{Id=22,gameplayActor=separated,actorVisuals=ownedVisuals};
        Actors(separated); CombatAccessibility.Update();
        check(ScreenReader.Messages.Single().Text=="Capitán esqueleto", "Exact actor-sync visual display preserves custom enemy name");
        check(LeAccess.Plugin.Log.Messages.Any(m=>m.Contains("name_source=owned_visuals")),"Owned visual source diagnosed independently of simulation actor");
        int dataReads=separated.data.Reads;
        check(dataReads==2,"Owned custom display avoids unnecessary base-name localization");

        Reset(); ownedVisuals.gameObject.scene = new() { handle = 2, Unloaded = true };
        Actors(separated); CombatAccessibility.Update();
        check(ScreenReader.Messages.Single().Text == "Skeleton", "Unloaded linked visuals fall back to current actor data");
        ownedVisuals.gameObject.scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        Reset(); separated.actorSync.gameplayActor=far; Actors(separated); CombatAccessibility.Update();
        check(ScreenReader.Messages.Single().Text=="Skeleton","Mismatched pooled sync cannot borrow another actor's visual name");
        Reset(); separated.actorSync.gameplayActor=separated; separated.actorSync.WasCollected=true;
        Actors(separated); CombatAccessibility.Update();
        check(ScreenReader.Messages.Single().Text=="Skeleton","Collected sync is guarded before fields and falls back to own data");
        separated.actorSync.WasCollected=false;ownedVisuals.WasCollected=true;
        Reset(); Actors(separated);CombatAccessibility.Update();
        check(ScreenReader.Messages.Single().Text=="Skeleton","Collected linked visuals cannot hide valid own ActorData name");
        ownedVisuals.WasCollected=false;ownedVisuals.BaseDisplayInformation.Name=" ";
        Reset();Actors(separated);CombatAccessibility.Update();
        check(ScreenReader.Messages.Single().Text=="Skeleton","Empty current visual display falls back to localized ActorData");
        Reset();separated.actorSync=null;separated.data.WasCollected=true;Actors(separated,far);CombatAccessibility.Update();
        check(ScreenReader.Messages.Count==0,"Collected nearest ActorData never substitutes a farther hostile");
        separated.data.WasCollected=false;separated.data.actorName=" ";
        Reset();Actors(separated);dataReads=separated.data.Reads;CombatAccessibility.Update();
        check(ScreenReader.Messages.Count==0 && separated.data.Reads==dataReads,
            "Unnamed ActorData does not invoke native technical-object-name fallback");
        separated.data.actorName="Skeleton";separated.data.LocalizedName=null;
        Reset();Actors(separated,far);CombatAccessibility.Update();
        check(ScreenReader.Messages.Count==0 && NearestEnemyProbe.LastStatus.Contains("nearest_name=unavailable"),
            "Missing selected ActorData translation leaves current-source diagnosis without farther substitution");

        Reset(); GameplayEnvironment.IsOnlineClient = true; PlayerFinder.playerActorComponent = null;
        PlayerFinder.playerActorVisuals = new ActorVisuals { Id = 1, Alignment = friendly };
        ActorManager.instance.actors = null;
        ActorManager.instance.visuals = new NativeList<ActorManager.ActorVisualsList>(new ActorManager.ActorVisualsList
        {
            alignment = hostile,
            visuals = new NativeList<ActorVisuals>(new ActorVisuals
            {
                Id = 9, Alignment = hostile, transform = new Transform { position = new Vector3(2,0,0) },
                BaseDisplayInformation = new BaseDisplayInformation { Name = "En línea" }
            })
        });
        CombatAccessibility.Update();
        check(ScreenReader.Messages.Single().Text == "En línea", "Online mode keeps visual registry without requiring offline actor");
        check(LeAccess.Plugin.Log.Messages.Any(x => x.Contains("registry=online visuals")), "Online registry choice is visible in diagnostics");
        check(UnityEngine.Object.InvalidAccesses == 0, "All collected-object guards run before native access, even inside caught failures");
        CombatAccessibility.Cleanup();
    }
}
