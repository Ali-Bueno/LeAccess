using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Vitals;

/// <summary>The character's gold, from the same tracker the game's gold counters display.</summary>
/// <remarks>See reference/vitals-api.md#gold-2026-10-05.</remarks>
internal static class GoldReport
{
    internal static bool TryRead(out int gold)
    {
        gold = 0;
        // The local inventory's tracker first; online the local player's, offline the actor's.
        var inventory = ItemContainersManager.Instance;
        var tracker = IsAlive(inventory) ? inventory.goldTracker : null;
        if (!IsAlive(tracker))
        {
            var local = PlayerFinder.getLocalPlayerInMultiplayer();
            tracker = IsAlive(local) ? local.goldTracker : null;
        }
        if (!IsAlive(tracker))
        {
            var actor = PlayerFinder.playerActorComponent;
            tracker = IsAlive(actor) ? actor.goldTracker : null;
        }
        if (!IsAlive(tracker)) return false;
        gold = tracker.value;
        return true;
    }
}
