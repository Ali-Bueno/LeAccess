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
    private static string _lastStatus;
    private static double _nextStatusLog;
    private const double StatusLogIntervalSeconds = 2;
    internal static bool HasActiveContent { get; private set; }
    internal static bool HasPendingContent { get; private set; }

    internal static bool TryHandleFocus(GameObject element)
    {
        try
        {
            if (!IsAlive(element)) return false;
            bool read = PassiveSelectionReader.TryRead(out var selection);
            var focusedNode = element.GetComponentInParent<SkillTreeNode>();
            ReportStatus(IsAlive(focusedNode) && IsAlive(focusedNode.tree) && focusedNode.inPassiveTree());
            return read && element.transform.IsChildOf(selection.Root.transform);
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    internal static void Update()
    {
        try
        {
            bool read = PassiveSelectionReader.TryRead(out var selection);
            ReportStatus();
            if (!read) { ResetSelection(PassiveSelectionReader.HasPendingModel); return; }
            HasActiveContent = true;
            if (_key != selection.Key)
            {
                Tracker.Reset();
                _key = selection.Key;
            }
            HasPendingContent = Tracker.Observe(1, selection.Summary,
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame, out string announcement);
            if (announcement is null) return;
            if (!PassiveSelectionReader.TryRead(out var current) || current.Key != selection.Key)
            { ResetSelection(PassiveSelectionReader.HasPendingModel); return; }
            HasPendingContent = false;
            ScreenReader.Speak(announcement, interrupt: true);
            Plugin.Log.LogInfo($"[Passive] {announcement}");
        }
        catch (Exception ex) { ResetSelection(); ReportFailure(ex); }
    }

    internal static bool ReadCurrentDetails()
    {
        try
        {
            bool read = PassiveSelectionReader.TryRead(out var selection);
            ReportStatus();
            if (!read) { ResetSelection(PassiveSelectionReader.HasPendingModel); return false; }
            if (!PassiveSelectionReader.TryRead(out var current) || current.Key != selection.Key)
            { ResetSelection(PassiveSelectionReader.HasPendingModel); return false; }
            HasActiveContent = true;
            if (!ScreenReader.IsAvailable()) return true;
            if (_key != selection.Key) { Tracker.Reset(); _key = selection.Key; }
            // Acknowledge the brief so the next polling frame cannot interrupt the details.
            Tracker.Observe(1, selection.Summary, false, out _);
            Tracker.Observe(1, selection.Summary, true, out _);
            HasPendingContent = false;
            ScreenReader.Speak(selection.Text, interrupt: true);
            Plugin.Log.LogInfo($"[Passive details] {selection.Text}");
            return true;
        }
        catch (Exception ex) { ResetSelection(); ReportFailure(ex); return false; }
    }

    private static void ResetSelection(bool retainPendingModel = false)
    {
        if (!retainPendingModel) PassiveSelectionReader.Reset();
        Tracker.Reset();
        _key = null;
        HasActiveContent = false;
        HasPendingContent = false;
    }

    internal static void Reset()
    {
        ResetSelection();
        _lastFailure = null;
        _lastStatus = null;
        _nextStatusLog = 0;
    }

    private static void ReportStatus(bool hasPassiveFocus = false)
    {
        if (!hasPassiveFocus && !PassiveSelectionReader.HasPassiveContext) return;
        string status = PassiveSelectionReader.LastStatus;
        if (string.IsNullOrWhiteSpace(status) || status == _lastStatus || Time.unscaledTimeAsDouble < _nextStatusLog) return;
        _lastStatus = status;
        _nextStatusLog = Time.unscaledTimeAsDouble + StatusLogIntervalSeconds;
        Plugin.Log.LogInfo($"[PassiveStatus] {status}");
    }

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Passive feedback unavailable: {ex}");
    }
}
