using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LE.UI.PanelSystem;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Endgame;

/// <summary>Speaks the moving parts of the endgame panels: the focused option or echo, and who voted.</summary>
/// <remarks>Polled; the window text itself comes from the screen-content reader. See reference/endgame-panels.md.</remarks>
internal static class EndgameAccessibility
{
    private const int StopId = 1;
    private static readonly ScreenContentTracker Tracker = new();
    private static List<VoteEntry> _votes;
    private static int _panelId;
    private static bool _spokeStop;
    private static string _key;
    private static string _notice;
    private static int _rejectedPanelId;
    private static string _lastFailure;

    internal static bool HasActiveContent { get; private set; }

    /// <summary>The generic announcer stays silent for controls whose panel reads them here.</summary>
    internal static bool TryHandleFocus(GameObject element)
    {
        try
        {
            var panel = FocusedPanel();
            return IsAlive(panel) && IsAlive(element) && EndgameStops.Owns(panel, element);
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    internal static void Update(bool readDetails = false)
    {
        HasActiveContent = false;
        try
        {
            var panel = FocusedPanel();
            if (!IsAlive(panel) || !Application.isFocused || LE.UI.LoadingScreen.IsVisible) { Reset(); return; }
            if (panel.GetInstanceID() != _panelId) { Reset(); _panelId = panel.GetInstanceID(); }
            // The window's opening text goes first; a stop or a vote never talks over it.
            bool free = ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame && !ScreenContentAnnouncer.HasPendingContent;
            UpdateVotes(panel, free);
            UpdateNotice(panel, free);
            HasActiveContent = UpdateStop(panel, free, readDetails);
        }
        catch (Exception ex) { Reset(); ReportFailure(ex); }
    }

    internal static void Reset()
    {
        Tracker.Reset();
        _votes = null;
        _panelId = 0;
        _spokeStop = false;
        _key = null;
        _notice = null;
        HasActiveContent = false;
    }

    private static bool UpdateStop(Panel panel, bool free, bool readDetails)
    {
        EndgameStop stop = default;
        bool found = false;
        var monolith = panel.TryCast<MonolithPanel>();
        if (IsAlive(monolith)) found = MonolithReader.TryRead(monolith, out stop);
        else
        {
            // The Observatory reads its slot even when no control has focus.
            UiFocusOwnership.TryGetSelected(out var selected);
            found = EndgameStops.TryRead(panel, selected, out stop);
        }
        if (!found) { Tracker.Reset(); _key = null; return false; }
        if (_key != stop.Key) { Tracker.Reset(); _key = stop.Key; }
        // Stable text only: tooltips fill in over a frame or two.
        Tracker.Observe(StopId, stop.Text, free, out string announcement, acknowledge: false);
        if (readDetails) announcement = stop.Details?.Invoke() ?? stop.Text;
        if (string.IsNullOrWhiteSpace(announcement)) return true;
        if (!ScreenReader.TrySpeak(announcement, interrupt: _spokeStop)) return true;
        Tracker.Acknowledge(StopId, stop.Text);
        _spokeStop = true;
        Plugin.Log.LogInfo($"[Endgame] {announcement}");
        return true;
    }

    private static void UpdateNotice(Panel panel, bool free)
    {
        var ladder = panel.TryCast<LadderPanel>();
        string notice = IsAlive(ladder) ? Market.LadderReader.EmptyNotice(ladder) : null;
        if (notice == null) { _notice = null; return; }
        if (notice == _notice || !free || !ScreenReader.TrySpeak(notice, interrupt: false)) return;
        _notice = notice;
        Plugin.Log.LogInfo($"[Endgame] {notice}");
    }

    private static void UpdateVotes(Panel panel, bool free)
    {
        if (!VoteRosterReader.TryRead(panel, out var current)) { _votes = null; return; }
        string text = _votes == null ? EndgameText.Roster(current) : EndgameText.VoteChanges(_votes, current);
        if (string.IsNullOrWhiteSpace(text)) { _votes ??= current; return; }
        if (!free || !ScreenReader.TrySpeak(text, interrupt: false)) return;
        _votes = current;
        Plugin.Log.LogInfo($"[Endgame votes] {text}");
    }

    // The panel that owns controller focus, whether the game hands focus to the panel or to one of its controls.
    private static Panel FocusedPanel()
    {
        var navigation = NavigationManager.currentlySelected;
        if (navigation is null || navigation.WasCollected) return null;
        var mediator = navigation.TryCast<NavigableMediator>();
        Panel panel;
        if (IsAlive(mediator)) panel = mediator.FocusedPanel;
        else
        {
            var owner = navigation.TryCast<Component>();
            panel = IsAlive(owner) ? owner.GetComponentInParent<Panel>() : null;
        }
        if (!IsAlive(panel) || panel.GetInstanceID() == _rejectedPanelId) return null;
        if (!panel.isActiveAndEnabled || !UiVisibility.IsVisible(panel.gameObject)) return null;
        if (EndgameWindows.IsFamily(panel)) return panel;
        _rejectedPanelId = panel.GetInstanceID();
        return null;
    }

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Endgame panel feedback unavailable: {ex}");
    }
}
