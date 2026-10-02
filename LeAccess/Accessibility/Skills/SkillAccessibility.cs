using System;
using System.Collections.Generic;
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
    private static double _nextBindingRefresh;
    private const double BindingRefreshSeconds = 0.2;
    private static string _spokenName;
    private static string _spokenBinding;
    private static string _spokenText;
    private static SkillDetailsSource _spokenDetailsSource;
    private static bool _observedTooltip;
    private static SkillDetailsSource _initialTooltipSource;
    private static string _initialTooltipBase;
    private static readonly Dictionary<SkillDetailsSource, string> ReducedDetails = new();
    private static readonly Dictionary<SkillDetailsSource, (string Basis, string Text)> LiveSnapshots = new();

    internal static bool HasActiveContent { get; private set; }
    internal static bool HasPendingContent { get; private set; }

    internal static bool TryHandleFocus(GameObject element)
    {
        try
        {
            return IsAlive(element) && SkillSelectionReader.TryRead(out var selection) &&
                IsAlive(selection.Root) && element.transform.IsChildOf(selection.Root.transform);
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    internal static void Update()
    {
        try
        {
            if (!SkillSelectionReader.TryRead(out var selection)) { ClearSelection(); return; }
            HasActiveContent = true;
            if (_selectionKey != selection.Key)
            {
                ClearSelection();
                HasActiveContent = true;
                _selectionKey = selection.Key;
            }

            // Refresh remaps while focused without traversing hotbar glyphs every frame.
            double now = Time.unscaledTimeAsDouble;
            if (now >= _nextBindingRefresh)
            {
                _nextBindingRefresh = now + BindingRefreshSeconds;
                _binding = null;
                try
                {
                    _binding = IsAlive(selection.BarIcon)
                        ? SkillBindingReader.ReadAssignment(selection.BarIcon) : SkillBindingReader.ReadAssignment(selection.Ability);
                }
                catch (Exception ex) { ReportFailure(ex); }
            }

            // Keep fluctuating DPS/cost readouts from continuously interrupting menu speech.
            if (!string.IsNullOrWhiteSpace(selection.LiveDetails) &&
                (!LiveSnapshots.TryGetValue(selection.DetailsSource, out var snapshot) || snapshot.Basis != selection.Details))
                LiveSnapshots[selection.DetailsSource] = (selection.Details, selection.LiveDetails);
            string liveDetails = !string.IsNullOrWhiteSpace(selection.LiveDetails) &&
                LiveSnapshots.TryGetValue(selection.DetailsSource, out var currentSnapshot) && currentSnapshot.Basis == selection.Details
                ? currentSnapshot.Text : null;
            string text = string.IsNullOrWhiteSpace(selection.Name) ? null :
                ScreenContentTracker.Combine(new[] { selection.Name, _binding, selection.Details, liveDetails });
            if (selection.DetailsSource != SkillDetailsSource.Description)
            {
                if (!_observedTooltip)
                {
                    _observedTooltip = true;
                    if (_spokenText != null && selection.Name == _spokenName && _binding == _spokenBinding)
                    {
                        _initialTooltipSource = selection.DetailsSource;
                        _initialTooltipBase = _spokenText;
                    }
                }
                else if (selection.DetailsSource != _initialTooltipSource) _initialTooltipBase = null;
            }
            // Hiding details is not a new selection. Keep the already-delivered text
            // acknowledged, without reusing it for a changed name or assigned control.
            bool reducedDetails = (int)selection.DetailsSource < (int)_spokenDetailsSource &&
                selection.Name == _spokenName && _binding == _spokenBinding;
            if (reducedDetails)
            {
                if (!ReducedDetails.TryGetValue(selection.DetailsSource, out string baseline))
                    ReducedDetails[selection.DetailsSource] = baseline = text;
                if (text == baseline) text = _spokenText;
            }
            HasPendingContent = Tracker.Observe(1, text,
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame, out string announcement);
            if (announcement == null) return;
            HasPendingContent = false;
            bool lateTooltip = _initialTooltipBase != null && selection.Name == _spokenName && _binding == _spokenBinding;
            string spoken = lateTooltip ? SkillAnnouncementDelta.NewFragments(_initialTooltipBase, announcement) : announcement;
            _initialTooltipBase = null;
            if (!string.IsNullOrWhiteSpace(spoken)) ScreenReader.Speak(spoken, interrupt: !lateTooltip);
            _spokenName = selection.Name;
            _spokenBinding = _binding;
            _spokenText = announcement;
            _spokenDetailsSource = selection.DetailsSource;
            ReducedDetails.Clear();
            if (!string.IsNullOrWhiteSpace(spoken)) Plugin.Log.LogInfo($"[Skill] {spoken}");
        }
        catch (Exception ex) { ClearSelection(); ReportFailure(ex); }
    }

    private static void ClearSelection()
    {
        Tracker.Reset();
        _selectionKey = null;
        _binding = null;
        _nextBindingRefresh = 0;
        _spokenName = null;
        _spokenBinding = null;
        _spokenText = null;
        _spokenDetailsSource = SkillDetailsSource.Description;
        _observedTooltip = false;
        _initialTooltipBase = null;
        _initialTooltipSource = SkillDetailsSource.Description;
        ReducedDetails.Clear();
        HasActiveContent = false;
        LiveSnapshots.Clear();
        HasPendingContent = false;
    }

    internal static void Reset() { ClearSelection(); _lastFailure = null; }

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Skill feedback unavailable: {ex}");
    }
}
