using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Quests;

internal static class DynamicObjectiveAccessibility
{
    // Match the existing automatic quest and vitals readers' sampling budget.
    private const double PollSeconds = 0.1;
    private static readonly ScreenContentTracker Tracker = new();
    private static readonly List<DynamicObjectiveContent> Content = new();
    private static readonly HashSet<int> Visible = new(), PreviousVisible = new();
    private static readonly List<string> Announcements = new();
    private static readonly Dictionary<int, string> Attempts = new();
    private static double _nextPoll;
    private static int _scene;
    private static IntPtr _character;
    private static string _lastFailure;
    private static bool _canOwnAnnouncement;
    private static bool _hasReadyAnnouncement;
    internal static bool HasPendingContent { get; private set; }
    internal static bool HasPendingAnnouncement => _canOwnAnnouncement && _hasReadyAnnouncement;

    internal static void Update(bool otherContentActive = false, bool higherPriorityPending = false)
    {
        _canOwnAnnouncement = false;
        if (!QuestUpdateAccessibility.AutomaticUpdatesEnabled) { Reset(); return; }
        try
        {
            bool gameplayAccess = QuestAnnouncementEligibility.HasGameplayAccess(otherContentActive, _character);
            _canOwnAnnouncement = gameplayAccess;
            if (LE.UI.LoadingScreen.IsVisible) return;
            var scene = SceneManager.GetActiveScene();
            if (_character != IntPtr.Zero && _scene != scene.handle) _canOwnAnnouncement = false;
            if (!scene.isLoaded) return;
            if (!SceneList.TryGetNonDefaultSceneDetails(scene.name, out _)) { Reset(); return; }
            double now = Time.unscaledTimeAsDouble;
            if (now < _nextPoll) return;
            HasPendingContent = false;
            _nextPoll = now + PollSeconds;
            var player = PlayerFinder.player;
            if (!IsAlive(player) || !player.activeInHierarchy || player.scene.handle != scene.handle) return;
            var character = PlayerFinder.getPlayerData();
            if (character is null || character.WasCollected) return;
            if (_scene != scene.handle || _character != character.Pointer)
            {
                Reset(); _scene = scene.handle; _character = character.Pointer; _nextPoll = now + PollSeconds;
                gameplayAccess = QuestAnnouncementEligibility.HasGameplayAccess(otherContentActive, _character);
            }
            _canOwnAnnouncement = gameplayAccess;
            DynamicObjectiveProbe.Read(scene, Content);
            bool canSpeak = _canOwnAnnouncement && !higherPriorityPending && !ScreenReader.SpokeThisFrame &&
                ScreenReader.IsAvailable() && QuestUpdateAccessibility.CanAnnounceAt(now);
            Visible.Clear(); Announcements.Clear(); Attempts.Clear(); _hasReadyAnnouncement = false;
            bool unannouncedPending = false;
            foreach (var item in Content)
            {
                if (!Visible.Add(item.Id)) continue;
                bool pending = Tracker.Observe(item.Id, item.Text, canSpeak, out string announcement, acknowledge: false);
                HasPendingContent |= pending;
                if (announcement != null) { Announcements.Add(announcement); Attempts[item.Id] = announcement; }
                else unannouncedPending |= pending;
                _hasReadyAnnouncement |= Tracker.HasStableContent(item.Id);
            }
            foreach (int id in PreviousVisible)
                if (!Visible.Contains(id)) Tracker.Hide(id);
            PreviousVisible.Clear();
            foreach (int id in Visible) PreviousVisible.Add(id);
            string text = ScreenContentTracker.Combine(Announcements);
            if (string.IsNullOrWhiteSpace(text)) return;
            if (!ScreenReader.TrySpeak(text, interrupt: false)) return;
            foreach (var attempt in Attempts) Tracker.Acknowledge(attempt.Key, attempt.Value);
            HasPendingContent = unannouncedPending;
            _hasReadyAnnouncement = false;
            foreach (int id in Visible) _hasReadyAnnouncement |= Tracker.HasStableContent(id);
            QuestUpdateAccessibility.RecordAnnouncement(now, dynamic: true);
            Plugin.Log.LogInfo($"[Dynamic objective] {text}");
        }
        catch (Exception ex)
        {
            Reset();
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"Dynamic objective feedback unavailable: {ex.Message}");
        }
    }

    internal static void Reset()
    {
        Tracker.Reset(); Content.Clear(); Visible.Clear(); PreviousVisible.Clear(); Announcements.Clear();
        Attempts.Clear();
        _nextPoll = 0; _scene = 0; _character = IntPtr.Zero; HasPendingContent = false;
        _canOwnAnnouncement = false;
        _hasReadyAnnouncement = false;
    }
}
