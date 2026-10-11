using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Zones.Transitions;

/// <summary>Reads the "Entering ..." and "Portaling to ..." pop-ups the game shows before a zone loads.</summary>
internal static class ZoneTransitionAnnouncer
{
    // The additive (pinnacle and boss) loader has no singleton; looking it up is retried at this pace.
    private const double ResolveIntervalSeconds = 2;
    private static readonly ZoneTransitionState Zone = new();
    private static readonly ZoneTransitionState Additive = new();
    private static AdditiveLoadingStatusUI _additive;
    private static double _nextResolveSeconds;
    private static string _lastFailure;

    internal static void Update()
    {
        try
        {
            if (!Application.isFocused) { Reset(); return; }
            Speak(Zone, ReadZone());
            Speak(Additive, ReadAdditive());
        }
        catch (Exception ex)
        {
            Reset();
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"Zone transition feedback unavailable: {ex.Message}");
        }
    }

    internal static void Reset()
    {
        Zone.Reset();
        Additive.Reset();
        _additive = null;
    }

    private static void Speak(ZoneTransitionState state, string visibleText)
    {
        if (!state.Observe(visibleText, out string announcement) || !ScreenReader.TrySpeak(announcement, interrupt: false)) return;
        state.Acknowledge(announcement);
        Plugin.Log.LogInfo($"[Zone] transition: {announcement}");
    }

    private static string ReadZone()
    {
        var ui = ZoneLoadingStatusUI.instance;
        return IsAlive(ui) && UiVisibility.IsVisible(ui.Panel) ? Read(ui.zoneNameTMP) : null;
    }

    private static string ReadAdditive()
    {
        var ui = ResolveAdditive();
        if (!IsAlive(ui)) return null;
        bool shown = UiVisibility.IsVisible(ui.overlayPanel) || UiVisibility.IsVisible(ui.fullPanel);
        return shown ? ScreenContentTracker.Combine(new[] { Read(ui.zoneNameTMP), Read(ui.zoneDetails) }) : null;
    }

    private static AdditiveLoadingStatusUI ResolveAdditive()
    {
        if (IsAlive(_additive)) return _additive;
        double now = (double)System.Diagnostics.Stopwatch.GetTimestamp() / System.Diagnostics.Stopwatch.Frequency;
        if (now < _nextResolveSeconds) return null;
        _nextResolveSeconds = now + ResolveIntervalSeconds;
        _additive = UnityEngine.Object.FindObjectOfType<AdditiveLoadingStatusUI>(true);
        return _additive;
    }

    private static string Read(TMP_Text label) =>
        TextUtils.IsVisibleLabel(label) ? TextUtils.CleanText(label.text) : null;
}
