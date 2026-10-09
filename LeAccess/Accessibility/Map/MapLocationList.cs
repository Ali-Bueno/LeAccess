using System;
using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Map;

/// <summary>With the quests hidden, D-pad up and down walk the era's destinations region by region instead of the quests.</summary>
/// <remarks>Snapping mirrors the game's own recentre; see reference/waypoint-map.md.</remarks>
internal static class MapLocationList
{
    internal readonly record struct Entry(UIWaypoint Waypoint, UIWaypointRegion Region, int Index, int Count);

    private static UIWaypoint _snapped;
    private static int _pollsSinceSnap;
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
            Snap(map, entries[next].Waypoint);
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

    /// <summary>The current era's focusable waypoints, by region in the map's order; waypoints in no region come last.</summary>
    internal static List<Entry> Build(MapPanel map)
    {
        var entries = new List<Entry>();
        var controller = CurrentController(map);
        if (controller is null) return entries;
        var remaining = new List<UIWaypoint>();
        for (int i = 0; i < controller.waypointsInMenu.Count; i++)
            if (IsAlive(controller.waypointsInMenu[i])) remaining.Add(controller.waypointsInMenu[i]);
        for (int r = 0; r < controller.regionsInMenu.Count; r++)
        {
            var region = controller.regionsInMenu[r];
            if (!IsAlive(region) || region.waypointsInRegion is null) continue;
            var members = new List<UIWaypoint>();
            for (int i = 0; i < region.waypointsInRegion.Count; i++)
            {
                int index = remaining.FindIndex(w => Same(w, region.waypointsInRegion[i]));
                if (index < 0) continue;
                members.Add(remaining[index]);
                remaining.RemoveAt(index);
            }
            Add(entries, members, region);
        }
        Add(entries, remaining, null);
        return entries;
    }

    /// <summary>Logs when a snapped selection did not survive the game's next per-frame focus check.</summary>
    internal static void CheckSnap(MapPanel map)
    {
        if (_snapped is null || ++_pollsSinceSnap < 2) return;
        var snapped = _snapped;
        _snapped = null;
        if (!IsAlive(map) || !IsAlive(snapped) || snapped.representsSceneInEra(map.GetFocusedSceneID(), map.CurrentEra)) return;
        Plugin.Log.LogWarning($"[World map] snapped selection {snapped.sceneName} was replaced by '{map.GetFocusedSceneID()}'");
    }

    private static void Add(List<Entry> entries, List<UIWaypoint> members, UIWaypointRegion region)
    {
        for (int i = 0; i < members.Count; i++) entries.Add(new Entry(members[i], region, i, members.Count));
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

    private static int IndexOf(List<Entry> entries, MapPanel map)
    {
        string scene = map.GetFocusedSceneID();
        if (string.IsNullOrEmpty(scene)) return -1;
        return entries.FindIndex(e => e.Waypoint.representsSceneInEra(scene, map.CurrentEra));
    }

    // Waypoint icons are inactive in the region zoom band, where the game cannot focus them.
    private static void Snap(MapPanel map, UIWaypoint waypoint)
    {
        var zoom = map.mapZoom;
        if (IsAlive(zoom) && zoom.currentZoom <= zoom.labelZoomRegionBreakpoint) zoom.ZoomTo(zoom.regionZoomLevel);
        if (IsAlive(map.worldMapArrow)) map.worldMapArrow.CenterOnGameobject(waypoint.gameObject);
        waypoint.SelectSceneLocation();
        _snapped = waypoint;
        _pollsSinceSnap = 0;
    }

    private static void Speak(MapPanel map, Entry entry, bool withRegion)
    {
        var parts = new List<string>();
        if (withRegion && IsAlive(entry.Region) && IsAlive(entry.Region.regionLabel))
            parts.Add(TextUtils.CleanText(entry.Region.regionLabel.text));
        string label = WaypointReader.ReadLabel(map.selectedLocationLabel) ?? WaypointReader.ReadName(entry.Waypoint);
        parts.AddRange(MapSelectionAnnouncer.Describe(map, map.GetFocusedSceneID(), map.GetFocusedRegionID(), label));
        if (entry.Count > 1) parts.Add(ModText.Format("list.position", entry.Index + 1, entry.Count));
        ScreenReader.Speak(ScreenContentTracker.Combine(parts), interrupt: true);
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
