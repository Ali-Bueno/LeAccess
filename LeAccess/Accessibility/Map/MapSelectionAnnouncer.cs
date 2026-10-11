using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.Accessibility.Zones;
using LeAccess.I18n;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Map;

/// <summary>Speaks world-map selection changes; the map navigable moves them without focusing a control.</summary>
internal static class MapSelectionAnnouncer
{
    private static string _selectionKey;
    private static SceneList.Era? _era;
    private static bool _questsHidden, _afterEra;

    internal static void Reset() { _selectionKey = null; _era = null; _questsHidden = _afterEra = false; }

    /// <summary>A quest change moves the location too; its brief speaks first, so the location queues after it.</summary>
    /// <returns>True when the selected location itself changed.</returns>
    internal static bool Update(MapPanel map, bool questMoved)
    {
        if (!IsAlive(map) || !map.isActiveAndEnabled || !UiVisibility.IsVisible(map.gameObject)) { Reset(); return false; }
        string scene = map.GetFocusedSceneID(), region = map.GetFocusedRegionID();
        string label = WaypointReader.ReadLabel(map.selectedLocationLabel);
        string key = $"{scene}|{region}|{label}";
        bool questsHidden = MapLocationList.QuestsHidden(map);
        bool first = _selectionKey is null;
        bool moved = key != _selectionKey, eraChanged = _era != map.CurrentEra, questsToggled = !first && questsHidden != _questsHidden;
        if (!moved && !eraChanged && !questsToggled) return false;
        _selectionKey = key; _era = map.CurrentEra; _questsHidden = questsHidden;
        if (!questMoved && !eraChanged && ScreenReader.SpokeThisFrame && !first) { _afterEra = false; return moved; }
        var parts = new List<string>();
        if (eraChanged) parts.AddRange(Era(map));
        if (questsToggled || first && questsHidden) parts.Add(ModText.Get(questsHidden ? "map.quests_hidden" : "map.quests_shown"));
        if (moved || eraChanged) parts.AddRange(Describe(map, scene, region, label));
        string text = ScreenContentTracker.Combine(parts);
        if (string.IsNullOrWhiteSpace(text)) return moved;
        // The place the map settles on after an era switch queues behind the era's name instead of cutting it.
        bool queue = questMoved || _afterEra && !eraChanged;
        _afterEra = eraChanged;
        ScreenReader.Speak(text, !queue);
        Plugin.Log.LogInfo($"[World map] {text}");
        return moved;
    }

    // The tab's title is an English prefab string copied into mapTitle; the game's localized era name comes first.
    private static IEnumerable<string> Era(MapPanel map)
    {
        for (int i = 0; i < map.eras.Count; i++)
        {
            if (map.eras[i] is not { } era || era.era != map.CurrentEra) continue;
            string title = ZoneEra.Name(era.era) ?? TextUtils.CleanText(era.mapTitle);
            yield return string.IsNullOrWhiteSpace(title) && IsAlive(map.mapTitle) ? TextUtils.CleanText(map.mapTitle.text) : title;
            if (ListPosition.Text(i + 1, map.eras.Count) is { } position) yield return position;
            yield break;
        }
    }

    /// <param name="known">The exact waypoint when the caller knows it; End of Time islands share one scene.</param>
    internal static IEnumerable<string> Describe(MapPanel map, string scene, string region, string label, UIWaypoint known = null)
    {
        yield return label;
        var waypoint = IsAlive(known) ? known : FocusTarget(map, scene) ?? FindWaypoint(map, scene);
        if (IsAlive(waypoint))
            foreach (string fragment in MapIslandReader.Read(waypoint.monolithIslandInfo)) yield return fragment;
        yield return WaypointReader.ReadLabel(map.selectedLocationLevelLabel);
        if (waypoint is not null)
        {
            foreach (string state in WaypointReader.WaypointStates(waypoint)) yield return state;
            foreach (string title in MapIconReader.AttachedTitles(waypoint)) yield return title;
            yield break;
        }
        var focusedRegion = FindRegion(map, region);
        if (focusedRegion is null) yield break;
        foreach (string state in WaypointReader.RegionStates(focusedRegion)) yield return state;
    }

    private static UIWaypoint FocusTarget(MapPanel map, string scene)
    {
        var target = IsAlive(map.worldMapArrow) ? map.worldMapArrow.FocusTarget : null;
        return IsAlive(target) && !string.IsNullOrEmpty(scene) && target.representsSceneInEra(scene, map.CurrentEra) ? target : null;
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
