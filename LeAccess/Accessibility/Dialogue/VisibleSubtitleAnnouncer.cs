using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Dialogue;

internal static class VisibleSubtitleAnnouncer
{
    private static readonly Dictionary<int, FloatingBark> Barks = new();
    private static readonly ScreenContentTracker BarkTracker = new();
    private static readonly ScreenContentTracker CinematicTracker = new();
    private static readonly SubmittedSubtitleTracker Submitted = new();
    private static readonly object SubmissionLock = new();
    private static readonly List<int> Expired = new();
    private static int _cinematicId;
    private static string _lastFailure;
    internal static bool HasPendingContent { get; private set; }

    internal static void NpcMessageSubmitted(string content, string sender)
    {
        try
        {
            lock (SubmissionLock)
                Submitted.Submit(TextUtils.CleanText(content), TextUtils.CleanText(sender), Now);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void BarkShown(FloatingBark bark)
    {
        try
        {
            if (!IsAlive(bark)) return;
            int id = bark.GetInstanceID();
            Barks[id] = bark;
            BarkTracker.Hide(id);
            lock (SubmissionLock) Submitted.ForgetRendered(id);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void BarkHidden(FloatingBark bark)
    {
        try
        {
            if (!IsAlive(bark)) return;
            int id = bark.GetInstanceID();
            Barks.Remove(id);
            BarkTracker.Hide(id);
            lock (SubmissionLock) Submitted.ForgetRendered(id);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    // These subtitles do not own navigation or gameplay input.
    internal static void Update(bool otherSpeechActive = false)
    {
        try
        {
            if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible) { Reset(); return; }
            HasPendingContent = false;
            bool canSpeak = !otherSpeechActive && !DialogueAnnouncer.HasActiveContent &&
                !ScreenContentAnnouncer.HasActiveContent && ScreenReader.IsAvailable();
            bool cinematicVisible = ReadCinematic(canSpeak);
            Expired.Clear();
            foreach (var pair in Barks)
            {
                try
                {
                    var bark = pair.Value;
                    if (!IsAlive(bark) || !bark.isActiveAndEnabled ||
                        !float.IsFinite(bark.secondsLeft) || bark.secondsLeft <= 0)
                    { Expired.Add(pair.Key); continue; }
                    var ui = bark.FloatingUI;
                    string text = IsAlive(ui) && ui.isActiveAndEnabled ? Read(ui.textComponent) : null;
                    bool pending = BarkTracker.Observe(pair.Key, text,
                        canSpeak && !cinematicVisible, out string announcement);
                    HasPendingContent |= pending && announcement == null;
                    if (announcement == null) continue;
                    if (SpeakRendered(pair.Key, announcement)) canSpeak = false;
                }
                catch (Exception ex) { Expired.Add(pair.Key); ReportFailure(ex); }
            }
            foreach (int id in Expired)
            {
                Barks.Remove(id); BarkTracker.Hide(id);
                lock (SubmissionLock) Submitted.ForgetRendered(id);
            }
            string submitted;
            lock (SubmissionLock)
            {
                if (DialogueAnnouncer.HasActiveContent || ScreenContentAnnouncer.HasActiveContent)
                    Submitted.DiscardPending();
                HasPendingContent |= Submitted.TryTake(canSpeak && !cinematicVisible, Now, out submitted);
            }
            if (submitted != null) Speak(submitted, "DialogueNpcMessage");
        }
        catch (Exception ex) { Reset(); ReportFailure(ex); }
    }

    private static bool ReadCinematic(bool canSpeak)
    {
        var panel = CinematicSubtitlePanel.Instance;
        string text = IsAlive(panel) && panel.isActiveAndEnabled && panel.IsActive &&
            IsAlive(panel._canvasGroup) && panel._canvasGroup.alpha > 0 &&
            UiVisibility.IsVisible(panel.gameObject) ? Read(panel._subtitleText) : null;
        int id = text != null ? panel.GetInstanceID() : 0;
        if (_cinematicId != id)
        {
            lock (SubmissionLock) Submitted.ForgetRendered(_cinematicId);
            CinematicTracker.Reset(); _cinematicId = id;
        }
        if (id == 0) return false;
        bool pending = CinematicTracker.Observe(id, text, canSpeak, out string announcement);
        HasPendingContent |= pending && announcement == null;
        if (announcement != null) SpeakRendered(id, announcement);
        return true;
    }

    private static string Read(TMP_Text label)
    {
        if (!IsAlive(label) || !label.isActiveAndEnabled || label.alpha <= 0 ||
            !UiVisibility.IsVisible(label.gameObject) ||
            IsAlive(label.canvasRenderer) && label.canvasRenderer.cull) return null;
        string text = TextUtils.CleanText(label.text);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static void Speak(string text, string source = "Dialogue")
    {
        ScreenReader.Speak(text, interrupt: false);
        Plugin.Log.LogInfo($"[{source}] {text}");
    }

    private static bool SpeakRendered(int id, string text)
    {
        lock (SubmissionLock)
            if (Submitted.ObserveRendered(id, text, Now)) return false;
        Speak(text);
        return true;
    }

    private static double Now => (double)System.Diagnostics.Stopwatch.GetTimestamp() /
        System.Diagnostics.Stopwatch.Frequency;

    internal static void Reset()
    {
        Barks.Clear(); Expired.Clear(); BarkTracker.Reset(); CinematicTracker.Reset();
        lock (SubmissionLock) Submitted.Reset();
        _cinematicId = 0; HasPendingContent = false;
    }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Visible subtitle feedback unavailable: {ex.Message}");
    }
}
