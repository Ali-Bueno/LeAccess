using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Map;

/// <summary>Speaks world-map selection changes; the map navigable moves them without focusing a control.</summary>
internal static class MapSelectionAnnouncer
{
    private static string _selectionKey;
    private static string _title;
    private static bool _questsHidden;

    internal static void Reset() { _selectionKey = null; _title = null; _questsHidden = false; }

    /// <summary>A quest change moves the location too; its brief speaks first, so the location queues after it.</summary>
    /// <returns>True when the selected location itself changed.</returns>
    internal static bool Update(MapPanel map, bool questMoved)
    {
        if (!IsAlive(map) || !map.isActiveAndEnabled || !UiVisibility.IsVisible(map.gameObject)) { Reset(); return false; }
        string scene = map.GetFocusedSceneID(), region = map.GetFocusedRegionID();
        string label = WaypointReader.ReadLabel(map.selectedLocationLabel);
        string key = $"{scene}|{region}|{label}";
        // OpenEra sets the title only after its crossfade, so the displayed text marks the era change.
        string title = WaypointReader.ReadLabel(map.mapTitle);
        bool questsHidden = MapLocationList.QuestsHidden(map);
        bool first = _selectionKey is null;
        bool moved = key != _selectionKey, titleChanged = title != _title, questsToggled = !first && questsHidden != _questsHidden;
        if (!moved && !titleChanged && !questsToggled) return false;
        _selectionKey = key; _title = title; _questsHidden = questsHidden;
        if (!questMoved && ScreenReader.SpokeThisFrame && !first) return moved;
        var parts = new List<string>();
        if (titleChanged) { parts.Add(title); parts.Add(EraPosition(map)); }
        if (questsToggled || first && questsHidden) parts.Add(ModText.Get(questsHidden ? "map.quests_hidden" : "map.quests_shown"));
        if (moved || titleChanged) parts.AddRange(Describe(map, scene, region, label));
        string text = ScreenContentTracker.Combine(parts);
        if (!string.IsNullOrWhiteSpace(text)) ScreenReader.Speak(text, !questMoved);
        return moved;
    }

    private static string EraPosition(MapPanel map)
    {
        for (int i = 0; i < map.eras.Count; i++)
            if (map.eras[i] is { } era && era.era == map.CurrentEra)
                return map.eras.Count > 1 ? ModText.Format("list.position", i + 1, map.eras.Count) : null;
        return null;
    }

    internal static IEnumerable<string> Describe(MapPanel map, string scene, string region, string label)
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
