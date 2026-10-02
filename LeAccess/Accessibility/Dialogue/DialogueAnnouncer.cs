using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.TextMeshPro;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Dialogue;

internal static class DialogueAnnouncer
{
    // Changed diagnostic snapshots are bounded independently of game frame rate.
    private const double DiagnosticIntervalSeconds = 5;
    private static readonly ScreenContentTracker Tracker = new();
    private static TextMeshProDialogueUI _ui;
    private static TextMeshProSubtitleControls _subtitle;
    private static long _subtitleId;
    private static string _fullLine;
    private static bool _responsesShown;
    private static bool _usingReminder;
    private static int _shownFrame;
    private static string _lastFailure;
    private static string _lastStatus;
    private static double _nextStatusSeconds;

    internal static bool HasPendingContent { get; private set; }
    internal static bool HasActiveContent
    {
        get
        {
            try { return Available() && (VisibleSubtitle() || IsAlive(ResponseRoot())); }
            catch (Exception) { return false; }
        }
    }

    internal static bool OwnsFocus(GameObject element)
    {
        try
        {
            if (!HasActiveContent || !IsAlive(element)) return false;
            var dialogue = _ui.dialogue;
            var panel = dialogue is not null && !dialogue.WasCollected ? dialogue.panel : null;
            if (IsAlive(panel) && element.transform.IsChildOf(panel.transform)) return true;
            var responses = ResponseRoot();
            if (IsAlive(responses) && element.transform.IsChildOf(responses.transform)) return true;
            return VisibleSubtitle() && IsAlive(_subtitle.panel) &&
                element.transform.IsChildOf(_subtitle.panel.transform);
        }
        catch (Exception) { return false; }
    }

    internal static void ShowSubtitle(TextMeshProDialogueUI ui, Subtitle subtitle)
    {
        try
        {
            if (!Attach(ui)) return;
            _subtitle = ui.currentSubtitleControls;
            _usingReminder = false;
            _subtitleId = subtitle is not null && !subtitle.WasCollected ? subtitle.Pointer.ToInt64() : 0;
            var formatted = _subtitleId != 0 ? subtitle.formattedText : null;
            _fullLine = formatted is not null && !formatted.WasCollected ? TextUtils.CleanText(formatted.text) : null;
            _shownFrame = Time.frameCount;
            HasPendingContent = true;
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void HideSubtitle(TextMeshProDialogueUI ui, Subtitle subtitle)
    {
        try
        {
            if (!Matches(ui)) return;
            if (subtitle is not null && !subtitle.WasCollected && _subtitleId != subtitle.Pointer.ToInt64()) return;
            if (NativeConversationOwns(ui) && VisibleSubtitle()) return;
            _subtitle = null;
            _subtitleId = 0;
            _fullLine = null;
            HasPendingContent = false;
            Tracker.Reset();
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void ShowResponses(TextMeshProDialogueUI ui)
    {
        try
        {
            if (!Attach(ui)) return;
            _responsesShown = true;
            SelectReminder();
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void HideResponses(TextMeshProDialogueUI ui)
    {
        try
        {
            if (!Matches(ui)) return;
            _responsesShown = false;
            if (!_usingReminder) return;
            SelectControls(null, false);
            HasPendingContent = false;
            Tracker.Reset();
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void Update()
    {
        try
        {
            RefreshNativeUi();
            ReportStatus();
            if (!Available()) { Reset(); return; }
            if (!VisibleSubtitle())
            {
                HasPendingContent = false;
                Tracker.Reset();
                return;
            }
            if (Time.frameCount <= _shownFrame) return;
            string line = Read(_subtitle.line);
            // Typewriter effects may expose a prefix; read the current subtitle's full localized line once.
            if (!string.IsNullOrEmpty(line) && !string.IsNullOrEmpty(_fullLine) &&
                _fullLine.StartsWith(line, StringComparison.Ordinal)) line = _fullLine;
            string speaker = Read(_subtitle.portraitName);
            string text = string.IsNullOrEmpty(line) ? null :
                string.IsNullOrEmpty(speaker) || speaker == line ? line : $"{speaker}: {line}";
            HasPendingContent = Tracker.Observe(_ui.GetInstanceID(), text,
                !ScreenContentAnnouncer.HasActiveContent && ScreenReader.IsAvailable(), out string announcement);
            if (announcement == null) return;
            HasPendingContent = false;
            ScreenReader.Speak(announcement, interrupt: true);
            Plugin.Log.LogInfo($"[Dialogue] {announcement}");
        }
        catch (Exception ex)
        {
            Reset();
            ReportFailure(ex);
        }
    }

    internal static void Reset()
    {
        _ui = null;
        _subtitle = null;
        _subtitleId = 0;
        _fullLine = null;
        _responsesShown = false;
        _usingReminder = false;
        HasPendingContent = false;
        Tracker.Reset();
    }

    private static bool Attach(TextMeshProDialogueUI ui)
    {
        if (!IsAlive(ui)) return false;
        if (!Matches(ui)) { Reset(); _ui = ui; }
        return true;
    }

    private static bool Matches(TextMeshProDialogueUI ui) =>
        IsAlive(ui) && IsAlive(_ui) && ui.Pointer == _ui.Pointer;

    private static bool NativeConversationOwns(TextMeshProDialogueUI ui)
    {
        var manager = PixelCrushers.DialogueSystem.DialogueManager.instance;
        if (!IsAlive(manager)) return false;
        var conversation = manager.conversationController;
        var source = manager.currentDialogueUI;
        if (conversation is null || conversation.WasCollected || !conversation.IsActive ||
            source is null || source.WasCollected) return false;
        var active = source.TryCast<TextMeshProDialogueUI>();
        return IsAlive(active) && IsAlive(ui) && active.Pointer == ui.Pointer;
    }

    private static void RefreshNativeUi()
    {
        // The lowercase field does not instantiate a missing dialogue manager.
        var manager = PixelCrushers.DialogueSystem.DialogueManager.instance;
        if (!IsAlive(manager)) { if (manager is not null) Reset(); return; }
        var source = manager.currentDialogueUI;
        var active = source is not null && !source.WasCollected ? source.TryCast<TextMeshProDialogueUI>() : null;
        if (!NativeConversationOwns(active)) { Reset(); return; }
        if (!Attach(active)) return;
        var controls = active.currentSubtitleControls;
        var current = controls;
        var dialogue = active.dialogue;
        if (dialogue is null || dialogue.WasCollected) return;
        var responses = dialogue.responseMenu;
        _responsesShown = responses is not null && !responses.WasCollected && responses.isVisible;
        if (SelectReminder()) { RefreshNativeLine(manager, _subtitle, requireRenderedMatch: true); return; }
        if (!VisibleControls(controls)) controls = dialogue.npcSubtitle;
        SelectControls(controls, false);
        if (current is not null && !current.WasCollected && controls is not null &&
            current.Pointer == controls.Pointer) RefreshNativeLine(manager, current);
    }

    private static void SelectControls(TextMeshProSubtitleControls controls, bool reminder)
    {
        if (controls is not null && controls.WasCollected) controls = null;
        if (!ReferenceEquals(_subtitle, controls) &&
            (_subtitle is null || _subtitle.WasCollected || controls is null || _subtitle.Pointer != controls.Pointer))
        {
            _subtitle = controls;
            // Moving the same line between subtitle and reminder controls is not a new subtitle.
            if (controls is null) _subtitleId = 0;
            _fullLine = null;
        }
        _usingReminder = reminder;
    }

    private static bool SelectReminder()
    {
        var dialogue = _ui.dialogue;
        if (!_responsesShown || dialogue is null || dialogue.WasCollected) return false;
        var responses = dialogue.responseMenu;
        if (responses is null || responses.WasCollected || !responses.isVisible ||
            !IsAlive(ResponseRoot()) || !VisibleControls(responses.subtitleReminder)) return false;
        SelectControls(responses.subtitleReminder, true);
        return true;
    }

    private static void ReportStatus()
    {
        double now = (double)System.Diagnostics.Stopwatch.GetTimestamp() / System.Diagnostics.Stopwatch.Frequency;
        if (now < _nextStatusSeconds) return;
        _nextStatusSeconds = now + DiagnosticIntervalSeconds;
        string status = $"attached={IsAlive(_ui)} available={Available()} visible={Available() && VisibleSubtitle()} " +
            $"responses={_responsesShown} reminder={_usingReminder} line={LineStatus()} modal={ScreenContentAnnouncer.HasActiveContent}";
        if (status == _lastStatus) return;
        _lastStatus = status;
        Plugin.Log.LogInfo($"[DialogueStatus] {status}");
    }

    private static string LineStatus()
    {
        if (_subtitle is null || _subtitle.WasCollected || !IsAlive(_subtitle.line)) return "missing";
        var line = _subtitle.line;
        if (!line.isActiveAndEnabled) return "disabled";
        if (line.alpha <= 0) return "transparent";
        if (IsAlive(line.canvasRenderer) && line.canvasRenderer.cull) return "culled";
        if (!UiVisibility.IsVisible(line.gameObject)) return "hidden";
        return string.IsNullOrWhiteSpace(line.text) ? "empty" : "ready";
    }

    private static void RefreshNativeLine(DialogueSystemController manager, TextMeshProSubtitleControls controls,
        bool requireRenderedMatch = false)
    {
        if (controls is null || controls.WasCollected || !Visible(controls.line)) return;
        var state = manager.conversationController.state;
        var subtitle = state is not null && !state.WasCollected ? state.subtitle : null;
        if (subtitle is null || subtitle.WasCollected) return;
        var formatted = subtitle.formattedText;
        string full = formatted is not null && !formatted.WasCollected ? TextUtils.CleanText(formatted.text) : null;
        string rendered = Read(controls.line);
        bool matches = !string.IsNullOrEmpty(rendered) && !string.IsNullOrEmpty(full) &&
            full.StartsWith(rendered, StringComparison.Ordinal);
        if (requireRenderedMatch && !matches)
        {
            // A retained reminder must not borrow a later response's model text or identity.
            if (string.IsNullOrEmpty(rendered) || _fullLine?.StartsWith(rendered, StringComparison.Ordinal) != true)
                _fullLine = null;
            return;
        }
        long id = subtitle.Pointer.ToInt64();
        if (_subtitleId != id)
        {
            _subtitleId = id;
            _fullLine = null;
            Tracker.Reset();
        }
        // Native subtitle rendering localizes formattedText first. Pair only
        // with a visible prefix to avoid speaking unrelated or untranslated text.
        _fullLine = matches ? full : null;
    }

    private static bool Available() => Application.isFocused && !LE.UI.LoadingScreen.IsVisible &&
        IsAlive(_ui) && _ui.isActiveAndEnabled;

    private static bool VisibleSubtitle() => (!_usingReminder || IsAlive(ResponseRoot())) &&
        (_ui.dialogue is null || _ui.dialogue.WasCollected || !IsAlive(_ui.dialogue.panel) ||
            UiVisibility.IsVisible(_ui.dialogue.panel.gameObject)) &&
        VisibleControls(_subtitle);

    private static bool VisibleControls(TextMeshProSubtitleControls controls) =>
        controls is not null && !controls.WasCollected && Visible(controls.line) &&
        !string.IsNullOrWhiteSpace(controls.line.text) &&
        (!IsAlive(controls.panel) || UiVisibility.IsVisible(controls.panel.gameObject));

    private static GameObject ResponseRoot()
    {
        if (!_responsesShown) return null;
        var dialogue = _ui.dialogue;
        if (dialogue is null || dialogue.WasCollected) return null;
        var responses = dialogue.responseMenu;
        if (responses is null || responses.WasCollected || !responses.isVisible) return null;
        // The installed response menu has no panel of its own; its controls belong to the dialogue panel.
        var panel = IsAlive(responses.panel) ? responses.panel : dialogue.panel;
        return IsAlive(panel) && UiVisibility.IsVisible(panel.gameObject) ? panel.gameObject : null;
    }

    private static bool Visible(TMP_Text label) => IsAlive(label) && label.isActiveAndEnabled && label.alpha > 0 &&
        UiVisibility.IsVisible(label.gameObject) && (!IsAlive(label.canvasRenderer) || !label.canvasRenderer.cull);

    private static string Read(TMP_Text label) => Visible(label) ? TextUtils.CleanText(label.text) : null;

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Dialogue feedback unavailable: {ex.Message}");
    }
}
