using System;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Passives;

internal static class PassiveAccessibility
{
    private static readonly ScreenContentTracker Tracker = new();
    private static string _key;
    private static string _lastFailure;
    internal static bool HasActiveContent { get; private set; }
    internal static bool HasPendingContent { get; private set; }

    internal static bool TryHandleFocus(GameObject element)
    {
        try
        {
            return IsAlive(element) && PassiveSelectionReader.TryRead(out var selection) &&
                element.transform.IsChildOf(selection.Root.transform);
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    internal static void Update()
    {
        try
        {
            if (!PassiveSelectionReader.TryRead(out var selection)) { ResetSelection(); return; }
            HasActiveContent = true;
            if (_key != selection.Key)
            {
                Tracker.Reset();
                _key = selection.Key;
            }
            HasPendingContent = Tracker.Observe(1, selection.Text,
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame, out string announcement);
            if (announcement is null) return;
            HasPendingContent = false;
            ScreenReader.Speak(announcement, interrupt: true);
            Plugin.Log.LogInfo($"[Passive] {announcement}");
        }
        catch (Exception ex) { ResetSelection(); ReportFailure(ex); }
    }

    private static void ResetSelection()
    {
        PassiveSelectionReader.Reset();
        Tracker.Reset();
        _key = null;
        HasActiveContent = false;
        HasPendingContent = false;
    }

    internal static void Reset()
    {
        ResetSelection();
        _lastFailure = null;
    }

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Passive feedback unavailable: {ex}");
    }
}
