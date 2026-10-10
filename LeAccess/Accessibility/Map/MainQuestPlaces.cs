using System.Collections.Generic;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Map;

/// <summary>Where the local player's active main quest currently is, read live from the game's quest state.</summary>
/// <remarks>The waypoint flags are only computed when a waypoint activates; see reference/waypoint-map.md.</remarks>
internal static class MainQuestPlaces
{
    private static string _loggedScenes;

    /// <summary>Host scenes of the active main quest's current step; empty when none, null when quest state is unavailable.</summary>
    internal static List<string> CurrentScenes()
    {
        var list = PlayerFinder.getLocalPlayerStatefulQuestList();
        if (list is null || list.WasCollected) return null;
        var scenes = new List<string>();
        if (!list.hasActiveMainQuest(out Quest main) || !IsAlive(main) || !main.playerVisible) return scenes;
        var state = list.getStatefulQuest(main);
        var step = state is null || state.WasCollected ? null : state.currentStep;
        if (!IsAlive(step) || step.questStateDuringThisStep != QuestState.Active) return scenes;
        var objectives = step.objectives;
        if (objectives is null || objectives.WasCollected) return scenes;
        // Like the game's own markers, every objective of the step counts, completed ones included.
        foreach (var objective in objectives)
            if (IsAlive(objective) && !string.IsNullOrEmpty(objective.hostScene) && !scenes.Contains(objective.hostScene))
                scenes.Add(objective.hostScene);
        LogScenes(main, scenes);
        return scenes;
    }

    internal static bool IsAt(UIWaypoint waypoint, List<string> scenes)
    {
        if (!IsAlive(waypoint)) return false;
        foreach (string scene in scenes)
            if (waypoint.representsScene(scene)) return true;
        return false;
    }

    internal static bool IsIn(UIWaypointRegion region, List<string> scenes)
    {
        var waypoints = region.waypointsInRegion;
        if (waypoints is null || waypoints.WasCollected) return false;
        foreach (var waypoint in waypoints)
            if (IsAt(waypoint, scenes)) return true;
        return false;
    }

    // Records which scenes the main quest points to, once per change, to diagnose places without a marker.
    private static void LogScenes(Quest main, List<string> scenes)
    {
        string joined = main.id + ":" + string.Join(", ", scenes);
        if (joined == _loggedScenes) return;
        _loggedScenes = joined;
        Plugin.Log.LogInfo($"[World map] Main quest {main.id} current step scenes: {string.Join(", ", scenes)}");
    }
}
