using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Vitals;

/// <summary>The character's gold, from the same tracker the game's gold counters display.</summary>
/// <remarks>See reference/vitals-api.md#gold-2026-10-05.</remarks>
internal static class GoldReport
{
    internal static bool TryRead(out int gold) => TryRead(out gold, out _);

    // tracker is the counter's native identity, so a change of character or player reads as a new counter.
    internal static bool TryRead(out int gold, out long tracker)
    {
        gold = 0;
        tracker = 0;
        // The local inventory's tracker first; online the local player's, offline the actor's.
        var inventory = ItemContainersManager.Instance;
        var counter = IsAlive(inventory) ? inventory.goldTracker : null;
        if (!IsAlive(counter))
        {
            var local = PlayerFinder.getLocalPlayerInMultiplayer();
            counter = IsAlive(local) ? local.goldTracker : null;
        }
        if (!IsAlive(counter))
        {
            var actor = PlayerFinder.playerActorComponent;
            counter = IsAlive(actor) ? actor.goldTracker : null;
        }
        if (!IsAlive(counter)) return false;
        gold = counter.value;
        tracker = counter.Pointer.ToInt64();
        return true;
    }
}
