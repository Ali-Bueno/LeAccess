using System;
using System.Collections.Generic;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Map;

/// <summary>Speaks the outcome of Cross on the world map, which travels without a prompt or does nothing.</summary>
internal static class MapTravelAnnouncer
{
    private static bool _travelHandled;
    private static string _lastFailure;

    internal static void BeginSubmit() => _travelHandled = false;

    /// <summary>The waypoint the travel button targets; mirrors native UIWaypointStandard.LoadWaypointScene's guard.</summary>
    internal static void AnnounceTravel(UIWaypoint waypoint)
    {
        try
        {
            if (!IsAlive(waypoint)) return;
            _travelHandled = true;
            string name = WaypointReader.ReadName(waypoint);
            string text = waypoint.noWaypointInScene ? ModText.Get("waypoint.no_travel_point") :
                waypoint.playerIsHere ? ModText.Get("waypoint.here") :
                !waypoint.isActive ? ModText.Get("waypoint.locked") :
                string.IsNullOrWhiteSpace(name) ? ModText.Get("waypoint.traveling") : ModText.Format("waypoint.traveling_to", name);
            ScreenReader.Speak(text, interrupt: true);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    /// <summary>Cross on a location whose travel button did not respond says so instead of staying silent.</summary>
    internal static void EndSubmit(MapPanel map, SubmitType type)
    {
        try
        {
            if (type != SubmitType.A_EX || _travelHandled || !IsAlive(map) || !map.isActiveAndEnabled) return;
            string scene = map.GetFocusedSceneID();
            if (string.IsNullOrEmpty(scene)) return;
            var parts = new List<string> { ModText.Get("map.travel_unavailable") };
            parts.AddRange(MapSelectionAnnouncer.Describe(map, scene, map.GetFocusedRegionID(),
                WaypointReader.ReadLabel(map.selectedLocationLabel)));
            ScreenReader.Speak(ScreenContentTracker.Combine(parts), interrupt: true);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"World map travel feedback unavailable: {ex}");
    }
}
