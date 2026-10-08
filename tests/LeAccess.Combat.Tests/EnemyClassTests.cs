using BepInEx.Configuration;
using EHG.Multiplayer;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility;
using LeAccess.Accessibility.Combat;
using LeAccess.I18n;
using UnityEngine;

// Class words in the nearest-enemy speech, online and offline.
internal static class EnemyClassTests
{
    internal static void Run(Action<bool, string> check)
    {
        var friendly = new Alignment { Id = 100 };
        var hostile = new Alignment { Id = 200 };
        friendly.Foe = hostile;
        string Named(string name, string word) => ModText.Format("enemy.named_class", name, ModText.Format(word));

        string Offline(Action<Actor> shape)
        {
            CombatAccessibility.Cleanup();
            CombatAccessibility.Initialize(new ConfigFile());
            ScreenReader.Messages.Clear(); ScreenReader.Available = true; ScreenReader.SpokeThisFrame = ScreenReader.Speaking = false;
            GameplayEnvironment.IsOnlineClient = false;
            PlayerFinder.playerActorComponent = new Actor { Id = 1, Alignment = friendly };
            PlayerFinder.playerActorVisuals = null;
            ControllerTargetingParadigm._SkillTargetMaxDistance = 10;
            Time.unscaledTimeAsDouble = 0; SceneList.NonCombat = false;
            NavigationAudioState.SuspensionReason = null; GameplayInputState.Owns = true;
            var enemy = new Actor
            {
                Id = 2, Alignment = hostile, transform = new Transform { position = new Vector3(2, 0, 0) },
                Display = new BaseDisplayInformation { Name = "Gilgamesh el Corrupto" }
            };
            shape(enemy);
            ActorManager.instance = new ActorManager
            {
                actors = new NativeList<ActorManager.ActorList>(new ActorManager.ActorList
                { alignment = hostile, actors = new NativeList<Actor>(enemy) })
            };
            CombatAccessibility.Update();
            return ScreenReader.Messages.Count == 0 ? null : ScreenReader.Messages[0].Text;
        }

        check(Offline(e => { }) == "Gilgamesh el Corrupto", "A normal enemy is spoken by name alone");
        check(Offline(e => e.Display.actorClass = DisplayActorClass.Rare) == Named("Gilgamesh el Corrupto", "enemy.class.rare"),
            "A rare enemy is spoken with its class word");
        check(Offline(e => e.Display.actorClass = DisplayActorClass.Magic) == Named("Gilgamesh el Corrupto", "enemy.class.magic"),
            "A magic enemy is spoken with its class word");
        check(Offline(e => { e.Display.actorClass = DisplayActorClass.Rare; e.Display.Champion = true; }) ==
            Named("Gilgamesh el Corrupto", "enemy.class.champion"), "A champion outranks its rare display class");
        check(Offline(e => e.Boss = true) == "Gilgamesh el Corrupto", "A boss keeps its plain wording");
        check(Offline(e => e.Display.actorClass = DisplayActorClass.Boss) == "Gilgamesh el Corrupto",
            "A boss display class keeps the plain wording too");
        check(Offline(e => { e.data = new ActorData { rareStatus = ActorData.RareStatus.Always }; e.Display = null; }) == null,
            "An enemy without a name stays unspoken whatever its class");

        CombatAccessibility.Cleanup();
        CombatAccessibility.Initialize(new ConfigFile());
        ScreenReader.Messages.Clear(); ScreenReader.SpokeThisFrame = ScreenReader.Speaking = false;
        GameplayEnvironment.IsOnlineClient = true; PlayerFinder.playerActorComponent = null;
        PlayerFinder.playerActorVisuals = new ActorVisuals { Id = 1, Alignment = friendly };
        Time.unscaledTimeAsDouble = 0;
        ActorManager.instance = new ActorManager
        {
            visuals = new NativeList<ActorManager.ActorVisualsList>(new ActorManager.ActorVisualsList
            {
                alignment = hostile,
                visuals = new NativeList<ActorVisuals>(new ActorVisuals
                {
                    Id = 9, Alignment = hostile, transform = new Transform { position = new Vector3(2, 0, 0) },
                    BaseDisplayInformation = new BaseDisplayInformation { Name = "Gilgamesh el Corrupto", actorClass = DisplayActorClass.Rare }
                })
            })
        };
        CombatAccessibility.Update();
        check(ScreenReader.Messages.Count == 1 && ScreenReader.Messages[0].Text == Named("Gilgamesh el Corrupto", "enemy.class.rare"),
            "An online rare enemy is spoken with its class word");
        CombatAccessibility.Cleanup();
        GameplayEnvironment.IsOnlineClient = false;
    }
}
