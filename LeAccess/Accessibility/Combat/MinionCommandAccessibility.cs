using System;
using LeAccess.Accessibility.Exploration;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Players;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Combat;

/// <summary>Speaks the minion command the right stick just issued; see reference/minion-commands.md.</summary>
internal static class MinionCommandAccessibility
{
    private static readonly MinionCommandState State = new();
    private static string _lastFailure;

    internal static void Update()
    {
        try
        {
            var manager = EpochInputManager.instance;
            if (!IsAlive(manager) || !EpochInputManager.IsControllerActive || !GameplayInputState.OwnsInput())
            { State.Reset(); return; }
            if (!State.Observe(manager.canControllerMinionCommand, ExplorationInput.MenuHeld)) return;
            int minions = CountMinions();
            string target = TargetName();
            Plugin.Log.LogInfo($"[Minion command] minions={minions} target={target ?? "none"}");
            string text = MinionCommandState.Describe(minions, target);
            if (text != null) ScreenReader.Speak(text, interrupt: false);
        }
        catch (Exception ex)
        {
            State.Reset();
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"Minion command feedback unavailable: {ex.Message}");
        }
    }

    // The enemy the game aimed at: ControllerTargeting.RightStickTarget, the same one it passes to the command.
    private static string TargetName()
    {
        var controller = ControllerManager.instance;
        var targeting = IsAlive(controller) ? controller.Targeting : null;
        var hit = targeting is null || targeting.WasCollected ? null : targeting.RightStickTarget;
        var visuals = hit is null || hit.WasCollected ? null : hit.ActorVisuals;
        return IsAlive(visuals) ? NearestEnemyProbe.ReadName(visuals.BaseDisplayInformation) : null;
    }

    // Online each minion registers its creation references with the local ClientSummonTracker; offline the actor's SummonTracker lists them.
    private static int CountMinions()
    {
        var local = LocalPlayerSource.OnlineLocal();
        if (local is not null)
        {
            var tracker = local.clientSummonTracker;
            var references = IsAlive(tracker) ? tracker.clientMinionCreationReferences : null;
            if (references is null || references.WasCollected) return 0;
            int alive = 0;
            for (int i = 0; i < references.Count; i++)
                if (IsAlive(references[i]) && references[i].gameObject.activeInHierarchy) alive++;
            return alive;
        }
        var actor = PlayerFinder.getPlayerActor();
        if (!IsAlive(actor) || !actor.TryGetExistingSummonTracker(out var summons) || !IsAlive(summons)) return 0;
        var list = summons.summons;
        return list is null || list.WasCollected ? 0 : list.Count;
    }
}
