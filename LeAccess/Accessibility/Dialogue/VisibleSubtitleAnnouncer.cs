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
    private static readonly ScreenContentTracker VideoTracker = new();
    private static readonly SubmittedSubtitleTracker Submitted = new();
    private static readonly object SubmissionLock = new();
    private static readonly List<int> Expired = new();
    private static readonly SourceStatus VideoStatus = new("VideoSubtitleStatus");
    private static readonly SourceStatus CinematicStatus = new("CinematicSubtitleStatus");
    private static readonly SourceStatus BarkStatus = new("BarkSubtitleStatus");
    private static int _cinematicId;
    private static int _videoId, _videoCinematicId, _videoSubtitleIndex;
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
            if (!Application.isFocused) { Reset(); return; }
            HasPendingContent = false;
            bool canSpeak = !otherSpeechActive && !DialogueAnnouncer.HasActiveContent &&
                !ScreenContentAnnouncer.HasActiveContent && ScreenReader.IsAvailable();
            bool videoVisible = ReadVideo(canSpeak);
            if (LE.UI.LoadingScreen.IsVisible) { ResetAmbient(); return; }
            if (videoVisible) return;
            bool cinematicVisible = ReadCinematic(canSpeak);
            Expired.Clear();
            int currentBarks = 0, onScreenBarks = 0, nearbyBarks = 0, ownBarks = 0, eligibleBarks = 0;
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
                    bool eligible = BarkAudibility.Allows(bark, ui, out bool onScreen, out bool nearby,
                        out bool own, out bool ownsLabel);
                    if (!ownsLabel) text = null;
                    currentBarks++;
                    if (text != null && onScreen) onScreenBarks++;
                    if (nearby) nearbyBarks++;
                    if (own) ownBarks++;
                    if (text != null && eligible) eligibleBarks++;
                    if (text != null && !eligible)
                        lock (SubmissionLock) Submitted.ObserveRendered(pair.Key, text, Now);
                    bool pending = BarkTracker.Observe(pair.Key, text,
                        eligible && canSpeak && !cinematicVisible, out string announcement);
                    HasPendingContent |= eligible && pending && announcement == null;
                    if (announcement == null) continue;
                    if (SpeakRendered(pair.Key, announcement)) canSpeak = false;
                }
                catch (Exception ex) { Expired.Add(pair.Key); ReportFailure(ex); }
            }
            BarkStatus.Report(currentBarks > 0 ? $"current={currentBarks} onscreen={onScreenBarks} " +
                $"in_range={nearbyBarks} own={ownBarks} eligible={eligibleBarks} allowed={canSpeak && !cinematicVisible}" : null);
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

    private static bool ReadVideo(bool canSpeak)
    {
        var video = CharacterSelectionVideoManager.instance;
        bool playing = IsAlive(video) && video.isActiveAndEnabled && video.IsPlaying;
        var cinematic = playing ? video.currentCinematic : null;
        bool visible = playing && IsAlive(cinematic) && video.currentSubtitleIndex > 0 &&
            IsAlive(video.cv) && video.cv.alpha > 0 && IsAlive(video.holder) &&
            UiVisibility.IsVisible(video.holder);
        string text = visible ? Read(video.subtitleText) : null;
        int id = text != null ? video.GetInstanceID() : 0;
        int cinematicId = id != 0 ? cinematic.GetInstanceID() : 0;
        int subtitleIndex = id != 0 ? video.currentSubtitleIndex : 0;
        if (_videoId != id || _videoCinematicId != cinematicId || _videoSubtitleIndex != subtitleIndex)
        {
            lock (SubmissionLock) Submitted.ForgetRendered(_videoId);
            VideoTracker.Reset();
            _videoId = id; _videoCinematicId = cinematicId; _videoSubtitleIndex = subtitleIndex;
        }
        VideoStatus.Report(playing ? $"playing=True visible={visible} line={text != null} allowed={canSpeak}" : null);
        if (id == 0) return false;
        bool pending = VideoTracker.Observe(id, text, canSpeak, out string announcement);
        HasPendingContent |= pending && announcement == null;
        if (announcement != null)
        {
            lock (SubmissionLock) Submitted.ObserveRendered(id, announcement, Now);
            Speak(announcement, "VideoSubtitle");
        }
        return true;
    }

    private static bool ReadCinematic(bool canSpeak)
    {
        var panel = CinematicSubtitlePanel.Instance;
        string text = IsAlive(panel) && panel.isActiveAndEnabled && panel.IsActive &&
            IsAlive(panel._canvasGroup) && panel._canvasGroup.alpha > 0 &&
            UiVisibility.IsVisible(panel.gameObject) ? Read(panel._subtitleText) : null;
        CinematicStatus.Report(IsAlive(panel) && panel.IsActive
            ? $"active=True visible={text != null} allowed={canSpeak}" : null);
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
        if (!TextUtils.IsVisibleLabel(label)) return null;
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
        ResetAmbient();
        VideoTracker.Reset();
        _videoId = _videoCinematicId = _videoSubtitleIndex = 0;
        HasPendingContent = false;
    }

    private static void ResetAmbient()
    {
        Barks.Clear(); Expired.Clear(); BarkTracker.Reset(); CinematicTracker.Reset();
        lock (SubmissionLock) Submitted.Reset();
        _cinematicId = 0;
    }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Visible subtitle feedback unavailable: {ex.Message}");
    }

    private sealed class SourceStatus
    {
        // Bound changed diagnostic snapshots independently of frame rate or subtitle fade animation.
        private const double DiagnosticIntervalSeconds = 5;
        private readonly string _source;
        private string _lastStatus;
        private double _nextReportAt;
        internal SourceStatus(string source) => _source = source;
        internal void Report(string status)
        {
            double now = Now;
            if (status == _lastStatus || now < _nextReportAt) return;
            Plugin.Log.LogInfo($"[{_source}] {status ?? "inactive"}");
            _lastStatus = status;
            _nextReportAt = now + DiagnosticIntervalSeconds;
        }
    }
}
