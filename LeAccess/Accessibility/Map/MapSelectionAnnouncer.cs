using System.Collections.Generic;
using System.Linq;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Map;

/// <summary>Speaks world-map selection changes; the map navigable moves them without focusing a control.</summary>
internal static class MapSelectionAnnouncer
{
    private static string _selectionKey;
    private static SceneList.Era? _era;

    internal static void Reset() { _selectionKey = null; _era = null; }

    /// <summary>A quest change moves the location too; its brief speaks first, so the location queues after it.</summary>
    internal static void Update(MapPanel map, bool questMoved)
    {
        if (!IsAlive(map) || !map.isActiveAndEnabled || !UiVisibility.IsVisible(map.gameObject)) { Reset(); return; }
        string scene = map.GetFocusedSceneID(), region = map.GetFocusedRegionID();
        string label = WaypointReader.ReadLabel(map.selectedLocationLabel);
        string key = $"{scene}|{region}|{label}";
        bool eraChanged = _era != map.CurrentEra;
        if (key == _selectionKey && !eraChanged) return;
        string title = eraChanged ? WaypointReader.ReadLabel(map.mapTitle) : null;
        bool first = _selectionKey is null;
        _selectionKey = key; _era = map.CurrentEra;
        if (!questMoved && ScreenReader.SpokeThisFrame && !first) return;
        string text = ScreenContentTracker.Combine(new List<string> { title }
            .Concat(Describe(map, scene, region, label)));
        if (!string.IsNullOrWhiteSpace(text)) ScreenReader.Speak(text, !questMoved);
    }

    private static IEnumerable<string> Describe(MapPanel map, string scene, string region, string label)
    {
        yield return label;
        yield return WaypointReader.ReadLabel(map.selectedLocationLevelLabel);
        var waypoint = FindWaypoint(map, scene);
        if (waypoint is not null)
        {
            foreach (string state in WaypointReader.WaypointStates(waypoint)) yield return state;
            yield break;
        }
        var focusedRegion = FindRegion(map, region);
        if (focusedRegion is null) yield break;
        foreach (string state in WaypointReader.RegionStates(focusedRegion)) yield return state;
    }

    private static UIWaypoint FindWaypoint(MapPanel map, string scene)
    {
        if (string.IsNullOrEmpty(scene)) return null;
        for (int era = 0; era < map.eras.Count; era++)
        {
            var controller = map.eras[era].waypointController;
            if (!IsAlive(controller)) continue;
            for (int i = 0; i < controller.waypointsInMenu.Count; i++)
            {
                var waypoint = controller.waypointsInMenu[i];
                if (IsAlive(waypoint) && waypoint.representsSceneInEra(scene, map.CurrentEra)) return waypoint;
            }
        }
        return null;
    }

    private static UIWaypointRegion FindRegion(MapPanel map, string region)
    {
        if (string.IsNullOrEmpty(region)) return null;
        for (int era = 0; era < map.eras.Count; era++)
        {
            var controller = map.eras[era].waypointController;
            if (!IsAlive(controller)) continue;
            for (int i = 0; i < controller.regionsInMenu.Count; i++)
            {
                var candidate = controller.regionsInMenu[i];
                if (IsAlive(candidate) && candidate.regioniName == region) return candidate;
            }
        }
        return null;
    }
}
