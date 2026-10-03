using System;
using System.Collections.Generic;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

internal static class NotificationAnnouncer
{
    private static readonly Dictionary<int, Source> Sources = new();
    private static readonly ScreenContentTracker Tracker = new();
    private static readonly HashSet<int> Visible = new();
    private static readonly HashSet<int> PreviousVisible = new();
    private static readonly List<int> Expired = new();
    private static readonly List<string> Announcements = new();
    private static readonly Dictionary<int, string> Attempts = new();
    private static readonly List<GameObject> Roots = new();
    private static readonly Dictionary<int, int> Deliveries = new();
    private static readonly EncounterCountdownReader Countdowns = new();
    private static string _lastFailure;

    internal static void TrackController(ControllerNotification notification) =>
        Track(notification, () => IsAlive(notification.notification) ? notification.notification.gameObject : null,
            false, true);

    internal static void TrackQuick(UIQuickNotificationText notification) =>
        Track(notification, () => IsAlive(notification._label) ? notification._label.gameObject : null, false, true);

    internal static void TrackChat(OverChatNotification notification) =>
        Track(notification, () => IsAlive(notification.notification) ? notification.notification.gameObject : null,
            false, true);

    internal static void TrackLocation(LocationNotificationManager notification) =>
        Zones.ZoneAnnouncer.TrackLocation(notification);

    internal static void TrackGameplay(Notifications notification) =>
        Track(notification, () => notification.holder, true, false);

    internal static void GameplayDelivered(LE.UI.NotificationSystem.NotificationBase notification) =>
        Track(notification, () => notification.gameObject, false, true);

    private static void Track(Component owner, Func<GameObject> getRoot, bool children, bool delivery)
    {
        try
        {
            if (!IsAlive(owner)) return;
            int id = owner.GetInstanceID();
            if (!Sources.TryGetValue(id, out var source))
                Sources[id] = source = new Source { Owner = owner, GetRoot = getRoot, Children = children };
            // Reused notification widgets can deliver the same text again without disabling their object.
            if (delivery) Rearm(getRoot());
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void Update(bool otherContentActive = false) => UpdateCore(otherContentActive, false);

    internal static void UpdateUrgent(bool foregroundContentActive = false) =>
        UpdateCore(foregroundContentActive || ScreenReader.SpokeThisFrame || !UrgentOwnsInput(), true);

    private static bool UrgentOwnsInput()
    {
        try
        {
            if (NavigationAudioState.IsSuspended || !GameplayInputState.OwnsInput()) return false;
            var input = EpochInputManager.instance;
            var keyboard = ScreenKeyboardManager.Instance;
            return !(IsAlive(input) && input.isInputFieldActive || IsAlive(keyboard) && keyboard._enabled);
        }
        catch (Exception ex) { ReportFailure(ex); return false; }
    }

    private static void UpdateCore(bool otherContentActive, bool countdownOnly)
    {
        if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible)
        {
            Tracker.Reset();
            Countdowns.Reset();
            PreviousVisible.Clear();
            return;
        }
        Visible.Clear();
        Expired.Clear();
        Announcements.Clear();
        Attempts.Clear();
        Countdowns.BeginPass();
        Roots.Clear();
        bool canSpeak = ScreenReader.IsAvailable() && !ScreenContentAnnouncer.HasActiveContent && !otherContentActive;
        foreach (var entry in Sources)
        {
            try
            {
                var source = entry.Value;
                if (!IsAlive(source.Owner)) { Expired.Add(entry.Key); continue; }
                var root = source.GetRoot();
                if (!UiVisibility.IsVisible(root)) continue;
                if (!source.Children) Collect(root);
                else
                {
                    var container = root.transform;
                    for (int i = 0; i < container.childCount; i++)
                        Collect(container.GetChild(i).gameObject);
                }
            }
            catch (Exception ex)
            {
                Expired.Add(entry.Key);
                ReportFailure(ex);
            }
        }
        foreach (int id in Expired) Sources.Remove(id);
        foreach (var root in Roots)
        {
            try { Observe(root, canSpeak, countdownOnly); }
            catch (Exception ex) { ReportFailure(ex); }
        }
        foreach (int id in PreviousVisible)
            if (!Visible.Contains(id)) { Tracker.Hide(id); Countdowns.Hide(id); }
        PreviousVisible.Clear();
        foreach (int id in Visible) PreviousVisible.Add(id);
        Expired.Clear();
        foreach (var delivery in Deliveries)
            if (!Visible.Contains(delivery.Key)) Expired.Add(delivery.Key);
        foreach (int id in Expired) Deliveries.Remove(id);
        string announcement = ScreenContentTracker.Combine(Announcements);
        if (string.IsNullOrWhiteSpace(announcement)) return;
        if (!ScreenReader.TrySpeak(announcement, interrupt: countdownOnly)) return;
        foreach (var attempt in Attempts) Tracker.Acknowledge(attempt.Key, attempt.Value);
        Countdowns.Acknowledge();
        Plugin.Log.LogInfo($"[VisibleNotification] {announcement}");
    }

    private static void Rearm(GameObject root)
    {
        if (!IsAlive(root)) return;
        RearmId(root.GetInstanceID());
        foreach (var ancestor in Roots)
            if (IsAlive(ancestor) && root.transform.IsChildOf(ancestor.transform))
                RearmId(ancestor.GetInstanceID());
    }

    private static void RearmId(int id)
    {
        if (Deliveries.TryGetValue(id, out int frame) && frame == Time.frameCount) return;
        Deliveries[id] = Time.frameCount;
        Tracker.Hide(id);
        Countdowns.Hide(id);
    }

    private static void Collect(GameObject root)
    {
        if (!UiVisibility.IsVisible(root)) return;
        for (int i = Roots.Count - 1; i >= 0; i--)
        {
            if (root.transform.IsChildOf(Roots[i].transform)) return;
            if (Roots[i].transform.IsChildOf(root.transform)) Roots.RemoveAt(i);
        }
        Roots.Add(root);
    }

    private static void Observe(GameObject root, bool canSpeak, bool countdownOnly)
    {
        if (!UiVisibility.IsVisible(root)) return;
        int id = root.GetInstanceID();
        if (!Visible.Add(id)) return;
        string text = Countdowns.Read(root, canSpeak, Announcements);
        if (countdownOnly) return;
        Tracker.Observe(id, text, canSpeak, out string announcement, acknowledge: false);
        if (announcement != null) { Announcements.Add(announcement); Attempts[id] = announcement; }
    }

    internal static void Reset()
    {
        Sources.Clear();
        Tracker.Reset();
        Visible.Clear();
        PreviousVisible.Clear();
        Expired.Clear();
        Announcements.Clear();
        Attempts.Clear();
        Roots.Clear();
        Deliveries.Clear();
        Countdowns.Reset();
        _lastFailure = null;
    }

    private static void ReportFailure(Exception ex)
    {
        if (_lastFailure == ex.Message) return;
        _lastFailure = ex.Message;
        Plugin.Log.LogWarning($"Visible notification feedback unavailable: {ex.Message}");
    }

    private sealed class Source
    {
        internal Component Owner;
        internal Func<GameObject> GetRoot;
        internal bool Children;
    }
}
