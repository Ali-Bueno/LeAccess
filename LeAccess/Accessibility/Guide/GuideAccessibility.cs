using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LE.UI.PanelSystem;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Guide;

/// <summary>Speaks the game-guide article the game shows for the chosen entry, and rereads it on request.</summary>
/// <remarks>See reference/guide-reading.md.</remarks>
internal static class GuideAccessibility
{
    private const int ArticleId = 1;
    private const int HintId = 1;
    private static readonly ScreenContentTracker HintTracker = new();
    private static readonly ScreenContentTracker Tracker = new();
    private static GuideManager _manager;
    private static string _lastFailure;

    internal static bool HasActiveContent { get; private set; }
    internal static bool HasPendingContent => HasActiveContent && Tracker.HasStableContent(ArticleId);

    internal static void Update()
    {
        HasActiveContent = false;
        try
        {
            ObserveTooltip();
            if (!TryGetManager(out var manager)) { Tracker.Reset(); return; }
            HasActiveContent = true;
            TryReadArticle(manager, out string text);
            bool pending = Tracker.Observe(ArticleId, text, ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame,
                out string announcement, acknowledge: false);
            if (!pending || announcement == null) return;
            // Queued behind the focus label of the entry that was just chosen.
            if (!ScreenReader.TrySpeak(announcement, interrupt: false)) return;
            Tracker.Acknowledge(ArticleId, text);
            Plugin.Log.LogInfo($"[Guide] {announcement}");
        }
        catch (Exception ex) { Tracker.Reset(); _manager = null; HasActiveContent = false; ReportFailure(ex); }
    }

    // The hint the game pops up for a guide topic (e.g. in the forge); clicking it opens the guide.
    private static void ObserveTooltip()
    {
        var tooltip = GameGuideTooltip.instance;
        string text = IsAlive(tooltip) && tooltip.isGameGuideActive && UiVisibility.IsVisible(tooltip.gameObject) &&
            IsAlive(tooltip.guideText) ? TextUtils.CleanText(tooltip.guideText.text) : null;
        bool pending = HintTracker.Observe(HintId, text, ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame,
            out string announcement, acknowledge: false);
        if (!pending || announcement == null || !ScreenReader.TrySpeak(announcement, interrupt: false)) return;
        HintTracker.Acknowledge(HintId, text);
        Plugin.Log.LogInfo($"[Guide hint] {announcement}");
    }

    /// <summary>Rereads the whole open article (the manual-details gesture).</summary>
    internal static bool ReadCurrentDetails()
    {
        try
        {
            if (!TryGetManager(out var manager)) return false;
            if (!TryReadArticle(manager, out string text)) return false;
            ScreenReader.Speak(text, true);
            Plugin.Log.LogInfo($"[Guide details] {text}");
            return true;
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    /// <summary>A category header says its name and whether its entries are listed.</summary>
    internal static bool TryReadCategory(GameObject element, out string text)
    {
        text = null;
        var category = element.GetComponentInParent<GuideCategory>();
        if (!IsAlive(category) || !IsAlive(category.headerButton)) return false;
        var header = category.headerButton.transform;
        if (!element.transform.IsChildOf(header)) return false;
        var label = header.GetComponentInChildren<TMP_Text>();
        string name = IsAlive(label) ? TextUtils.CleanText(label.text) : null;
        if (string.IsNullOrWhiteSpace(name)) return false;
        text = GuideArticleText.Category(name, category.expanded);
        return true;
    }

    // The panel the focused control belongs to; kept while that panel stays visible (e.g. focus on the search box).
    private static bool TryGetManager(out GuideManager manager)
    {
        manager = null;
        if (UiFocusOwnership.TryGetSelected(out var selected))
        {
            var panel = selected.GetComponentInParent<GameGuidePanel>();
            var found = IsAlive(panel) ? panel._guideManager : selected.GetComponentInParent<GuideManager>();
            if (IsAlive(found)) _manager = found;
        }
        if (IsAlive(_manager) && _manager.isActiveAndEnabled && UiVisibility.IsVisible(_manager.gameObject))
        { manager = _manager; return true; }
        _manager = null;
        return false;
    }

    private static bool TryReadArticle(GuideManager manager, out string text)
    {
        var paragraphs = new List<string>();
        var rendered = manager._activeEntryParagraphs;
        for (int i = 0; rendered is not null && !rendered.WasCollected && i < rendered.Count; i++)
        {
            var paragraph = rendered[i];
            if (IsAlive(paragraph) && paragraph.gameObject.activeInHierarchy) paragraphs.Add(paragraph.text);
        }
        var heading = manager.activeEntryTitle;
        string title = IsAlive(heading) && IsAlive(heading.title) ? heading.title.text : null;
        text = paragraphs.Count == 0 ? null : GuideArticleText.Compose(title, paragraphs);
        return !string.IsNullOrWhiteSpace(text);
    }

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Guide reading unavailable: {ex}");
    }
}
