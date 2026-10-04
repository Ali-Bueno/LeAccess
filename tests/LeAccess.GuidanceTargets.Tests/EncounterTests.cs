using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Navigation.Guidance;
using LE.UI.Minimap;
using UnityEngine;

internal static class EncounterTests
{
    private static GameObject Child(GameObject parent = null)
    { var value = new GameObject(); value.transform.parent = parent?.transform; return value; }

    internal static void Run(Action<bool, string> check)
    {
        var targets = new List<GuidanceTarget>();
        MapSectionWorldSnapshot world = default;
        RageOfMorditasGauntletWarningPointerVisualController pointer = null;
        RageOfMorditasGauntletVisualController gauntlet = null;
        ActorSync local = null;
        MeshRenderer renderer = null;
        void Setup(bool online = false)
        {
            EncounterGuidanceTargets.Reset(); targets.Clear(); MinimapIconManager.Instance = null;
            EHG.Multiplayer.GameplayEnvironment.IsOnlineClient = online;
            PlayerFinder.player = new GameObject();
            world = new(1) { Session = ((long)1 << 32) | (uint)PlayerFinder.player.GetInstanceID() };
            local = new GameObject().Add<ActorSync>(); PlayerFinder.Local = local;
            PlayerFinder.playerActorComponent = Child(PlayerFinder.player).Add<Actor>();
            PlayerFinder.playerActorComponent.actorSync = local;
            local.gameplayActor = online ? null : PlayerFinder.playerActorComponent;
            PlayerFinder.playerActorVisuals = Child(PlayerFinder.player).Add<ActorVisuals>();
            local.actorVisuals = PlayerFinder.playerActorVisuals;
            local.actorVisuals.actorSync = local;
            gauntlet = new GameObject().Add<RageOfMorditasGauntletVisualController>();
            gauntlet.transform.position = new(10, 2, 20);
            gauntlet._sync = new GameObject().Add<RageOfMorditasSync>();
            gauntlet._sync._gauntletVisuals = gauntlet;
            pointer = new GameObject().Add<RageOfMorditasGauntletWarningPointerVisualController>();
            pointer.pointerObject = Child(pointer.gameObject);
            renderer = pointer.pointerObject.Add<MeshRenderer>();
            pointer._lookAtTransform = gauntlet.transform;
            pointer._playerTransform = local.transform;
            gauntlet._gauntletWarningPointerVisualControllerInstance = pointer;
            EncounterGuidanceTargets.Track(pointer);
        }
        bool Read() { GuidanceTargetReader.TryRead(world, targets); return targets.Count == 1; }

        Setup();
        check(GuidanceTargetReader.TryRead(world, targets) && targets.Count == 1, "Active owned encounter arrow works without a minimap registry");
        var result = targets.Single();
        check(result.Kind == GuidanceTargetKind.EncounterReturn && result.Priority < 0 && result.ArrivalRadius == 0,
            "Return arrow is a distinct prioritized encounter target, not a quest, exit or completion radius");
        check(result.Position == gauntlet.transform.position && result.Id == gauntlet.GetInstanceID() &&
            result.Name == LeAccess.I18n.ModText.Get("guidance.encounter_return"), "Orientation uses the exact native gauntlet target and localized return instruction");
        EncounterGuidanceTargets.Track(pointer); check(Read(), "Repeated native update callbacks do not duplicate the target");
        gauntlet.transform.position = new(-20, 2, 30);
        check(Read() && targets[0].Position == gauntlet.transform.position, "Current target position is refreshed rather than cached");
        Setup(true); check(Read(), "Online client needs its current owned visuals without a server gameplay Actor");
        local.gameObject.scene = new() { handle = 8, Valid = true, isLoaded = true };
        gauntlet._sync.gameObject.scene = new() { handle = 9, Valid = true, isLoaded = true };
        check(Read(), "Verified local and encounter syncs may live in loaded additive networking scenes");

        var invalid = new (string Name, Action Change)[]
        {
            ("disabled pointer owner", () => pointer.enabled = false),
            ("hidden pointer object", () => pointer.pointerObject.activeInHierarchy = false),
            ("foreign pointer scene", () => pointer.pointerObject.scene.handle = 2),
            ("foreign pointer object", () => pointer.pointerObject.transform.parent = null),
            ("disabled renderer", () => renderer.enabled = false),
            ("suppressed renderer", () => renderer.forceRenderingOff = true),
            ("collected renderer", () => renderer.WasCollected = true),
            ("absent renderer", () => pointer.pointerObject.Components.Clear()),
            ("missing target", () => pointer._lookAtTransform = null),
            ("collected target", () => pointer._lookAtTransform.WasCollected = true),
            ("inactive target", () => gauntlet.gameObject.activeInHierarchy = false),
            ("foreign target scene", () => gauntlet.gameObject.scene.handle = 2),
            ("unloaded target scene", () => gauntlet.gameObject.scene.isLoaded = false),
            ("invalid target scene", () => gauntlet.gameObject.scene.Valid = false),
            ("disabled gauntlet", () => gauntlet.enabled = false),
            ("foreign gauntlet pointer", () => gauntlet._gauntletWarningPointerVisualControllerInstance = null),
            ("finished gauntlet", () => gauntlet._currentState = RageOfMorditasLogic.State.Finished),
            ("missing encounter sync", () => gauntlet._sync = null),
            ("disabled encounter sync", () => gauntlet._sync.enabled = false),
            ("foreign encounter owner", () => gauntlet._sync._gauntletVisuals = new GameObject().Add<RageOfMorditasGauntletVisualController>()),
            ("ended encounter", () => gauntlet._sync._state = RageOfMorditasLogic.State.ReadyForReward),
            ("unloaded encounter sync", () => gauntlet._sync.gameObject.scene.isLoaded = false),
            ("missing local sync", () => PlayerFinder.Local = null),
            ("collected local sync", () => local.WasCollected = true),
            ("inactive local sync", () => local.enabled = false),
            ("unloaded local sync", () => local.gameObject.scene.isLoaded = false),
            ("another player's transform", () => pointer._playerTransform = new GameObject().transform),
            ("foreign player scene", () => PlayerFinder.player.scene.handle = 2),
            ("inactive player", () => PlayerFinder.player.activeInHierarchy = false),
            ("changed snapshot player", () => world = world with { Session = world.Session + 1 }),
            ("stale offline actor", () => local.gameplayActor = new GameObject().Add<Actor>()),
            ("mismatched offline sync", () => local.gameplayActor.actorSync = null),
            ("foreign offline actor hierarchy", () => local.gameplayActor.transform.parent = null),
            ("dying actor", () => local.gameplayActor.Dying = true),
            ("invalid destination", () => gauntlet.transform.position = new(float.NaN, 0, 0))
        };
        foreach (var item in invalid) { Setup(); item.Change(); check(!Read(), "Return guidance rejects " + item.Name); }

        foreach (string failure in new[] { "stale", "sync", "dead", "hierarchy", "scene", "disabled", "collected" })
        {
            Setup(true);
            var visuals = local.actorVisuals;
            switch (failure)
            {
                case "stale": PlayerFinder.playerActorVisuals = new GameObject().Add<ActorVisuals>(); break;
                case "sync": visuals.actorSync = null; break;
                case "dead": visuals.dead = true; break;
                case "hierarchy": visuals.transform.parent = null; break;
                case "scene": visuals.gameObject.scene.handle = 2; break;
                case "disabled": visuals.enabled = false; break;
                case "collected": visuals.WasCollected = true; break;
            }
            check(!Read(), "Online return guidance rejects " + failure + " visuals");
        }
        Setup(); pointer.pointerObject.activeInHierarchy = false; check(!Read(), "Hidden arrow releases orientation immediately");
        pointer.pointerObject.activeInHierarchy = true; check(Read(), "Returning native arrow can be read without another initialization hook");
        pointer.Destroyed = true; check(!Read(), "Destroyed indicator expires from the registry");
        pointer.Destroyed = false; check(!Read(), "Expired indicator is not resurrected from a retained wrapper");
        EncounterGuidanceTargets.Track(pointer); check(Read(), "New live native callback rediscovers an indicator");
        Setup(); var duplicate = new GameObject().Add<RageOfMorditasGauntletWarningPointerVisualController>();
        EncounterGuidanceTargets.Track(duplicate); check(Read(), "Unready additional arrow cannot suppress an independently valid one");
        var manager = new GameObject().Add<MinimapIconManager>();
        manager.minimap = manager.gameObject.Add<Minimap>(); manager.icons.WasCollected = true;
        MinimapIconManager.Instance = manager; check(Read(), "Unavailable minimap model does not erase a verified encounter return target");
        EncounterGuidanceTargets.Reset(); check(!Read(), "Plugin lifecycle reset removes tracked encounter arrows");
        MinimapIconManager.Instance = null;
    }
}

internal class RageOfMorditasGauntletWarningPointerVisualController : Behaviour
{ internal GameObject pointerObject; internal Transform _lookAtTransform, _playerTransform; }
internal class RageOfMorditasGauntletVisualController : Behaviour
{
    internal RageOfMorditasGauntletWarningPointerVisualController _gauntletWarningPointerVisualControllerInstance;
    internal RageOfMorditasLogic.State _currentState = RageOfMorditasLogic.State.Gauntlet;
    internal RageOfMorditasSync _sync;
}
internal class RageOfMorditasSync : Behaviour
{
    internal RageOfMorditasGauntletVisualController _gauntletVisuals;
    internal RageOfMorditasLogic.State _state = RageOfMorditasLogic.State.Gauntlet;
}
internal static class RageOfMorditasLogic { internal enum State { Gauntlet, ReadyForReward, Finished } }
internal class ActorSync : Behaviour { internal Actor gameplayActor; internal ActorVisuals actorVisuals; }
internal class Actor : Behaviour { internal ActorSync actorSync; internal bool Dying; internal bool isDying() => Dying; }
internal class ActorVisuals : Behaviour { internal ActorSync actorSync; internal bool dead; }
internal static class PlayerFinder
{
    internal static GameObject player;
    internal static Actor playerActorComponent;
    internal static ActorVisuals playerActorVisuals;
    internal static ActorSync Local;
    internal static ActorSync getLocalActorSync() => Local;
}
namespace EHG.Multiplayer { internal static class GameplayEnvironment { internal static bool IsOnlineClient; } }
