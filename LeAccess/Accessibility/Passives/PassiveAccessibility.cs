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
    private static bool _detailsRequested;
    // Tree context speaks with the first node after opening the panel or switching trees.
    private static string _announcedTree;
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
            // Shoulder buttons focus two diamonds before the new tree loads; Update speaks the result once.
            return read && element.transform.IsChildOf(selection.Root.transform) ||
                PassivePanelReader.TryGetOpen(out var manager) && PassivePanelReader.DiamondIndex(manager, element) >= 0;
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    internal static void Update()
    {
        try
        {
            bool read = PassiveSelectionReader.TryRead(out var selection);
            ReportStatus();
            if (!read)
            {
                if (!PassiveSelectionReader.Blocked && !PassiveSelectionReader.HasPassiveContext) _announcedTree = null;
                ResetSelection(PassiveSelectionReader.HasPendingModel);
                return;
            }
            HasActiveContent = true;
            if (_key != selection.Key)
            {
                Tracker.Reset();
                _detailsRequested = false;
                _key = selection.Key;
            }
            HasPendingContent = Tracker.Observe(1, selection.Summary,
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame, out string announcement, acknowledge: false);
            HasPendingContent |= _detailsRequested;
            bool requestDetails = _detailsRequested && ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame;
            if (requestDetails)
            {
                Tracker.Observe(1, selection.Summary, false, out _, acknowledge: false);
                announcement = selection.Text;
            }
            if (announcement is null) return;
            if (!PassiveSelectionReader.TryRead(out var current) || current.Key != selection.Key)
            { ResetSelection(PassiveSelectionReader.HasPendingModel); return; }
            bool newTree = selection.TreeKey is not null && selection.TreeKey != _announcedTree;
            if (newTree) announcement = ScreenContentTracker.Combine(new[] {
                _announcedTree is null ? PassiveSelectionReader.ReadOverview(selection) : null,
                PassiveSelectionReader.ReadTree(selection), announcement });
            if (!ScreenReader.TrySpeak(announcement, interrupt: true))
            { HasPendingContent = true; return; }
            if (newTree) _announcedTree = selection.TreeKey;
            Tracker.Acknowledge(1, selection.Summary);
            HasPendingContent = false;
            _detailsRequested = false;
            Plugin.Log.LogInfo($"[{(requestDetails ? "Passive details" : "Passive")}] {announcement}");
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
            if (_key != selection.Key) { Tracker.Reset(); _key = selection.Key; }
            _detailsRequested = true;
            Tracker.Observe(1, selection.Summary, false, out _, acknowledge: false);
            Tracker.Observe(1, selection.Summary, false, out _, acknowledge: false);
            HasPendingContent = true;
            if (!ScreenReader.TrySpeak(selection.Text, interrupt: true)) return true;
            Tracker.Acknowledge(1, selection.Summary);
            _detailsRequested = false;
            HasPendingContent = false;
            Plugin.Log.LogInfo($"[Passive details] {selection.Text}");
            return true;
        }
        catch (Exception ex) { ResetSelection(); ReportFailure(ex); return false; }
    }

    /// <summary>Speaks the current selection again, as when a move cannot go further.</summary>
    internal static void RepeatCurrent() => _key = null;

    private static void ResetSelection(bool retainPendingModel = false)
    {
        if (!retainPendingModel) PassiveSelectionReader.Reset();
        Tracker.Reset();
        _key = null;
        HasActiveContent = false;
        HasPendingContent = false;
        _detailsRequested = false;
    }

    internal static void Reset()
    {
        ResetSelection();
        PassiveTreeNavigation.Reset();
        _announcedTree = null;
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
