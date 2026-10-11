using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Skills;

internal static class SkillAccessibility
{
    private static readonly ScreenContentTracker Tracker = new();
    private static string _selectionKey;
    private static string _lastFailure;
    private static string _binding;
    private static string _bindingStatus;
    private static double _nextBindingRefresh;
    private const double BindingRefreshSeconds = 0.2;
    private static bool _detailsRequested;
    private static string _moreInfoSpoken;
    private static double _nextMoreInfoPoll;

    internal static bool HasActiveContent { get; private set; }
    internal static bool HasPendingContent { get; private set; }

    internal static bool TryHandleFocus(GameObject element)
    {
        try
        {
            return IsAlive(element) && SkillSelectionReader.TryRead(out var selection, includeDetails: false) &&
                IsAlive(selection.Root) && element.transform.IsChildOf(selection.Root.transform);
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    internal static void Update(bool readDetails = false)
    {
        try
        {
            SkillSwapCarry.Update();
            if (!SkillSelectionReader.TryRead(out var selection, includeDetails: false))
            {
                if (SkillSelectionReader.TryGetPanel(out var panel)) SkillTreeNavigation.RecoverHiddenButton(panel);
                ClearSelection();
                ForgetGroups();
                return;
            }
            if (_selectionKey != selection.Key)
            {
                ClearSelection();
                _selectionKey = selection.Key;
            }
            HasActiveContent = true;
            _detailsRequested |= readDetails;

            double now = Time.unscaledTimeAsDouble;
            if (readDetails || now >= _nextBindingRefresh)
            {
                _nextBindingRefresh = now + BindingRefreshSeconds;
                _binding = null;
                try
                {
                    if (!selection.IsNode)
                    {
                        _binding = IsAlive(selection.BarIcon)
                            ? SkillBindingReader.ReadAssignment(selection.BarIcon) : SkillBindingReader.ReadAssignment(selection.Ability);
                        string status = SkillBindingReader.LastStatus;
                        if (_bindingStatus != status)
                        {
                            _bindingStatus = status;
                            Plugin.Log.LogInfo($"[Skill binding] {status}");
                        }
                    }
                }
                catch (Exception ex) { ReportFailure(ex); }
            }

            if (now >= _nextMoreInfoPoll && !selection.IsNode) { _nextMoreInfoPoll = now + BindingRefreshSeconds; SpeakExpandedInfo(selection.Key); }

            string identity = string.IsNullOrWhiteSpace(selection.Name) ? null :
                ScreenContentTracker.Combine(new[] { selection.Name, _binding, selection.Status });
            string brief = identity == null ? null : ScreenContentTracker.Combine(new[] { selection.Context, identity, selection.Position });
            if (brief == null) _detailsRequested = false;
            HasPendingContent = Tracker.Observe(1, brief,
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame, out string announcement, acknowledge: false);
            HasPendingContent |= _detailsRequested && !string.IsNullOrWhiteSpace(brief);
            bool requestDetails = _detailsRequested && ScreenReader.IsAvailable() &&
                (readDetails || !ScreenReader.SpokeThisFrame) && !string.IsNullOrWhiteSpace(brief);
            if (requestDetails)
            {
                if (!SkillSelectionReader.TryRead(out var details) || details.Key != selection.Key)
                { ClearSelection(); return; }
                Tracker.Observe(1, brief, false, out _, acknowledge: false);
                if (details.MoreInfo != null) _moreInfoSpoken = $"{details.Key}|{details.MoreInfo}";
                // A details request names the skill without its row or place.
                announcement = ScreenContentTracker.Combine(new[] { identity, details.Details, details.LiveDetails });
            }
            if (announcement == null) return;
            if (!SkillSelectionReader.TryRead(out var current, includeDetails: false) || current.Key != selection.Key)
            { ClearSelection(); return; }
            if (!ScreenReader.TrySpeak(announcement, interrupt: true))
            { HasPendingContent = true; return; }
            Tracker.Acknowledge(1, brief);
            _detailsRequested = false;
            HasPendingContent = false;
            Plugin.Log.LogInfo($"[{(requestDetails ? "Skill details" : "Skill")}] {announcement}");
        }
        catch (Exception ex) { ClearSelection(); ReportFailure(ex); }
    }

    // The extra tooltip stays only while Right Stick is held, so its text is spoken once as it appears.
    private static void SpeakExpandedInfo(string key)
    {
        if (!KeywordExplanation.Enabled || ScreenReader.SpokeThisFrame || !ScreenReader.IsAvailable()) return;
        if (!SkillSelectionReader.TryRead(out var full) || full.Key != key) return;
        if (full.MoreInfo == null) { _moreInfoSpoken = null; return; }
        string spoken = $"{key}|{full.MoreInfo}";
        if (spoken == _moreInfoSpoken || !ScreenReader.TrySpeak(full.MoreInfo, interrupt: true)) return;
        _moreInfoSpoken = spoken;
        Plugin.Log.LogInfo($"[Skill keywords] {full.MoreInfo}");
    }

    private static void ClearSelection()
    {
        _moreInfoSpoken = null;
        Tracker.Reset();
        _selectionKey = null;
        _binding = null;
        _nextBindingRefresh = 0;
        HasActiveContent = false;
        HasPendingContent = false;
        _detailsRequested = false;
    }

    /// <summary>Speaks the current selection again, as when a move cannot go further.</summary>
    internal static void RepeatCurrent() => _selectionKey = null;

    // Rows and branches announce their name again the next time a selection enters them.
    private static void ForgetGroups()
    {
        SkillPanelGrid.Forget();
        SkillTreeNavigation.ForgetBranch();
    }

    internal static void Reset()
    {
        ClearSelection(); ForgetGroups(); SkillTreeNavigation.Reset(); SkillTreeState.Reset(); SkillSwapCarry.Clear(); _lastFailure = null; _bindingStatus = null;
    }

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Skill feedback unavailable: {ex}");
    }
}
