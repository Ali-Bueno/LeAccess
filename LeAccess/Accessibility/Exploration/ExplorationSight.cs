namespace LeAccess.Accessibility.Exploration;

// What a sighted player sees on screen is listed without first walking close to it.
internal static class ExplorationSight
{
    // Landmarks (shrines, portals, exits, the stash), loot and breakable props; NPCs, chests and objects keep the seen-range rule.
    internal static bool ListsOnScreen(ExplorationCategory category) => category is ExplorationCategory.Shrine or
        ExplorationCategory.Portal or ExplorationCategory.Exit or ExplorationCategory.Stash or ExplorationCategory.Item or
        ExplorationCategory.Breakable;
}
