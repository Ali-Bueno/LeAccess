using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LE.UI.PanelSystem;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Map;

/// <summary>Reads world-map waypoints and regions from their own typed fields.</summary>
internal static class WaypointReader
{
    internal static bool TryRead(GameObject go, bool includeDetails, out string text)
    {
        text = null;
        var fragments = TryReadWaypoint(go) ?? TryReadRegion(go);
        if (fragments is null) return false;
        if (includeDetails) fragments.Add(ReadDescription(go));
        text = ScreenContentTracker.Combine(fragments);
        return true;
    }

    private static List<string> TryReadWaypoint(GameObject go)
    {
        var waypoint = go.GetComponentInParent<UIWaypoint>();
        if (!IsAlive(waypoint) || !UiVisibility.IsVisible(waypoint.gameObject)) return null;
        string name = ReadName(waypoint);
        if (name is null) return null;
        var fragments = new List<string> { name };
        fragments.AddRange(WaypointStates(waypoint));
        return fragments;
    }

    /// <summary>Here, locked and quest markers of a waypoint, without its name.</summary>
    internal static List<string> WaypointStates(UIWaypoint waypoint)
    {
        var states = new List<string>();
        if (waypoint.GetIsPlayerHere()) states.Add(ModText.Get("waypoint.here"));
        if (!waypoint.GetIsActive()) states.Add(ModText.Get("waypoint.locked"));
        var indicators = waypoint.questIndicators;
        if (IsAlive(indicators))
            AddQuests(states, indicators.hasActiveMainQuest, indicators.hasActiveSideQuest,
                indicators.hasPotentialMainQuest, indicators.haPotentialSideQuest);
        return states;
    }

    /// <summary>Here and quest markers of a region, without its name or count.</summary>
    internal static List<string> RegionStates(UIWaypointRegion region)
    {
        var states = new List<string>();
        if (IsAlive(region.playerLocation) && UiVisibility.IsVisible(region.playerLocation))
            states.Add(ModText.Get("waypoint.here"));
        AddQuests(states, region.hasActiveMainQuest, region.hasActiveSideQuest,
            region.hasPotentialMainQuest, region.haPotentialSideQuest);
        return states;
    }

    private static List<string> TryReadRegion(GameObject go)
    {
        var region = go.GetComponentInParent<UIWaypointRegion>();
        if (!IsAlive(region) || !UiVisibility.IsVisible(region.gameObject)) return null;
        string name = ReadLabel(region.regionLabel);
        if (name is null) return null;
        var fragments = new List<string> { name, ReadLabel(region.locationCountLabel) };
        fragments.AddRange(RegionStates(region));
        return fragments;
    }

    private static string ReadName(UIWaypoint waypoint)
    {
        if (waypoint.TryCast<UIWaypointStandard>() is { } standard) return ReadLabel(standard.locationLabel);
        return IsAlive(waypoint.label) ? TextUtils.GetTextFromHierarchy(waypoint.label) : null;
    }

    private static void AddQuests(List<string> fragments, bool activeMain, bool activeSide, bool availableMain, bool availableSide)
    {
        if (activeMain) fragments.Add(ModText.Get("waypoint.quest_main_active"));
        if (activeSide) fragments.Add(ModText.Get("waypoint.quest_side_active"));
        if (availableMain) fragments.Add(ModText.Get("waypoint.quest_main_available"));
        if (availableSide) fragments.Add(ModText.Get("waypoint.quest_side_available"));
    }

    private static string ReadDescription(GameObject go)
    {
        var map = go.GetComponentInParent<MapPanel>() ?? go.GetComponentInParent<WorldMapPanel>()?._mapPanel;
        var popup = IsAlive(map) ? map.waypointDescriptionPopup : null;
        return IsAlive(popup) ? ReadLabel(popup.description) : null;
    }

    internal static string ReadLabel(TMP_Text label) =>
        TextUtils.IsVisibleLabel(label) ? TextUtils.CleanText(label.text) : null;
}
