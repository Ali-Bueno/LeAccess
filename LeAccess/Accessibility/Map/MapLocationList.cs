using System;
using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Map;

/// <summary>With the quests hidden, D-pad up and down walk everything on the era's map, region by region, instead of the quests.</summary>
/// <remarks>Snapping mirrors the game's own recentre; see reference/waypoint-map.md.</remarks>
internal static class MapLocationList
{
    /// <summary>A waypoint (markers without a waypoint included) or a standalone icon, with its place in its region.</summary>
    internal readonly record struct Entry(UIWaypoint Waypoint, GeneralTooltop Icon, UIWaypointRegion Region, int Index, int Count);

    private static UIWaypoint _currentWaypoint;
    private static GeneralTooltop _currentIcon;
    private static string _settledScene;
    private static int _pollsSinceSnap = -1;
    private static string _lastFailure;

    /// <returns>True when the move was handled here; false leaves it to the game.</returns>
    internal static bool TryNavigate(MapPanel map, Direction direction)
    {
        if (direction != Direction.UP && direction != Direction.DOWN) return false;
        try
        {
            if (!IsAlive(map) || !QuestsHidden(map)) return false;
            if (map.mapTransitionBlocked || IsAlive(map.mapZoom) && map.mapZoom.manualZoomScrollBlocked) return true;
            var entries = Build(map);
            if (entries.Count == 0) return true;
            int current = IndexOf(entries, map);
            int next = Step(entries.Count, current, direction == Direction.DOWN);
            if (next < 0)
            {
                Speak(map, entries[current], withRegion: false);
                return true;
            }
            Snap(map, entries[next]);
            Speak(map, entries[next], withRegion: current < 0 || !Same(entries[current].Region, entries[next].Region));
            return true;
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    internal static bool QuestsHidden(MapPanel map) => IsAlive(map.questPanel) && map.questPanel.questsHidden;

    /// <summary>Nothing focused starts from either end; the list never wraps (-1 at an edge).</summary>
    internal static int Step(int count, int current, bool forward)
    {
        if (current < 0) return forward ? 0 : count - 1;
        int next = current + (forward ? 1 : -1);
        return next >= 0 && next < count ? next : -1;
    }

    /// <summary>Everything the current era's map shows, by region in the map's order (waypoints, then icons); the rest last.</summary>
    internal static List<Entry> Build(MapPanel map)
    {
        var entries = new List<Entry>();
        var controller = CurrentController(map);
        if (controller is null) return entries;
        var waypoints = new List<UIWaypoint>();
        for (int i = 0; i < controller.waypointsInMenu.Count; i++)
            if (IsAlive(controller.waypointsInMenu[i])) waypoints.Add(controller.waypointsInMenu[i]);
        var icons = new List<GeneralTooltop>();
        var iconList = controller.IconsInMenu;
        for (int i = 0; iconList is not null && i < iconList.Count; i++)
            if (MapIconReader.IsStandalone(iconList[i])) icons.Add(iconList[i]);
        for (int r = 0; r < controller.regionsInMenu.Count; r++)
        {
            var region = controller.regionsInMenu[r];
            if (!IsAlive(region)) continue;
            var members = new List<UIWaypoint>();
            for (int i = 0; region.waypointsInRegion is not null && i < region.waypointsInRegion.Count; i++)
            {
                int index = waypoints.FindIndex(w => Same(w, region.waypointsInRegion[i]));
                if (index < 0) continue;
                members.Add(waypoints[index]);
                waypoints.RemoveAt(index);
            }
            var regionIcons = icons.FindAll(icon => Same(icon.GetComponentInParent<UIWaypointRegion>(true), region));
            icons.RemoveAll(regionIcons.Contains);
            Add(entries, members, regionIcons, region);
        }
        Add(entries, waypoints, icons, null);
        return entries;
    }

    /// <summary>Notes where the map settled after a snap; logs when the game's focus check replaced a snapped waypoint.</summary>
    internal static void CheckSnap(MapPanel map)
    {
        if (_pollsSinceSnap < 0 || ++_pollsSinceSnap < 2) return;
        _pollsSinceSnap = -1;
        if (!IsAlive(map)) return;
        _settledScene = map.GetFocusedSceneID();
        if (IsAlive(_currentWaypoint) && !_currentWaypoint.representsSceneInEra(_settledScene, map.CurrentEra))
            Plugin.Log.LogWarning($"[World map] snapped selection {_currentWaypoint.sceneName} was replaced by '{_settledScene}'");
    }

    private static void Add(List<Entry> entries, List<UIWaypoint> waypoints, List<GeneralTooltop> icons, UIWaypointRegion region)
    {
        int count = waypoints.Count + icons.Count;
        for (int i = 0; i < waypoints.Count; i++) entries.Add(new Entry(waypoints[i], null, region, i, count));
        for (int i = 0; i < icons.Count; i++) entries.Add(new Entry(null, icons[i], region, waypoints.Count + i, count));
    }

    private static UIWaypointController CurrentController(MapPanel map)
    {
        for (int i = 0; i < map.eras.Count; i++)
        {
            var era = map.eras[i];
            if (era is not null && era.era == map.CurrentEra && IsAlive(era.waypointController)) return era.waypointController;
        }
        return null;
    }

    // While the map stays where it settled after a snap, the snapped entry is current: islands share one scene, and an
    // icon focuses no location (or the waypoint beside it).
    private static int IndexOf(List<Entry> entries, MapPanel map)
    {
        string scene = map.GetFocusedSceneID();
        if (scene == _settledScene && (IsAlive(_currentIcon) || IsAlive(_currentWaypoint)))
        {
            int index = entries.FindIndex(e => IsAlive(_currentIcon) ? Same(e.Icon, _currentIcon) : Same(e.Waypoint, _currentWaypoint));
            if (index >= 0) return index;
        }
        if (string.IsNullOrEmpty(scene)) return -1;
        return entries.FindIndex(e => IsAlive(e.Waypoint) && e.Waypoint.representsSceneInEra(scene, map.CurrentEra));
    }

    // Waypoint icons are inactive in the region zoom band, where the game cannot focus them.
    private static void Snap(MapPanel map, Entry entry)
    {
        var zoom = map.mapZoom;
        var target = IsAlive(entry.Waypoint) ? entry.Waypoint.gameObject : entry.Icon.gameObject;
        if (IsAlive(entry.Waypoint) && IsAlive(zoom) && zoom.currentZoom <= zoom.labelZoomRegionBreakpoint) zoom.ZoomTo(zoom.regionZoomLevel);
        if (IsAlive(map.worldMapArrow)) map.worldMapArrow.CenterOnGameobject(target);
        if (IsAlive(entry.Waypoint)) entry.Waypoint.SelectSceneLocation();
        else map.DeselectCurrentLocation();
        _currentWaypoint = entry.Waypoint;
        _currentIcon = entry.Icon;
        _settledScene = map.GetFocusedSceneID();
        _pollsSinceSnap = 0;
    }

    private static void Speak(MapPanel map, Entry entry, bool withRegion)
    {
        var parts = new List<string>();
        if (withRegion) parts.Add(MapRegionName.Get(entry.Region));
        if (IsAlive(entry.Waypoint))
        {
            string label = WaypointReader.ReadLabel(map.selectedLocationLabel) ?? WaypointReader.ReadName(entry.Waypoint);
            parts.AddRange(MapSelectionAnnouncer.Describe(map, map.GetFocusedSceneID(), map.GetFocusedRegionID(), label, entry.Waypoint));
        }
        else
        {
            var (title, body) = MapIconReader.Read(entry.Icon);
            parts.Add(title);
            parts.Add(body);
        }
        if (entry.Count > 1) parts.Add(ModText.Format("list.position", entry.Index + 1, entry.Count));
        string text = ScreenContentTracker.Combine(parts);
        ScreenReader.Speak(text, interrupt: true);
        Plugin.Log.LogInfo($"[World map list] {text} ({(IsAlive(entry.Waypoint) ? entry.Waypoint.sceneName : "icon")})");
    }

    private static bool Same(UnityEngine.Object a, UnityEngine.Object b) =>
        IsAlive(a) ? IsAlive(b) && a.Pointer == b.Pointer : !IsAlive(b);

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"World map list navigation unavailable: {ex}");
    }
}
