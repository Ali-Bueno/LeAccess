using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Navigation;

internal static class GameplayInputState
{
    internal static bool OwnsInput()
    {
        var selected = NavigationManager.currentlySelected;
        if (selected is not null)
        {
            if (selected.WasCollected) return false;
            var id = selected.GetNavigableID();
            if (id != NavigableID.NONE && id != NavigableID.NAVIGATION_MANGER &&
                id != NavigableID.GROUND_ITEM_LABELS) return false;
        }

        // Gameplay targets can remain cached while a non-pausing menu owns the controller.
        var manager = ControllerManager.instance;
        if (!IsAlive(manager)) return !EpochInputManager.IsControllerActive;
        var map = manager._CurrentMap_k__BackingField;
        return map is null || !map.WasCollected && map.TryCast<DefaultMap>() is not null;
    }
}
