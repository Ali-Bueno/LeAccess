using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ControllerMenus;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Zones;

internal static class ZoneAnnouncer
{
    private static readonly ZoneAnnouncementState State = new();
    private static LocationNotificationManager _notification;
    private static int _notificationScene;
    // The era last spoken; it is added to an area's name only when travel changes it, and survives resets.
    private static SceneList.Era? _announcedEra;
    private static string _lastFailure;
    internal static bool HasPendingContent => State.HasPendingContent;

    internal static void TrackLocation(LocationNotificationManager notification)
    {
        try
        {
            if (!IsAlive(notification)) return;
            _notification = notification;
            _notificationScene = SceneManager.GetActiveScene().handle;
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    internal static void Update()
    {
        try
        {
            var scene = SceneManager.GetActiveScene();
            State.EnterScene(scene.handle);
            if (State.HasDelivered) return;
            if (!Application.isFocused || LE.UI.LoadingScreen.IsVisible || !scene.isLoaded ||
                !IsAlive(PlayerFinder.player) || !PlayerFinder.player.activeInHierarchy)
            { State.Suspend(); return; }
            if (!SceneList.TryGetNonDefaultSceneDetails(scene.name, out var details) ||
                details is null || details.WasCollected) { State.Suspend(); return; }
            string name = TextUtils.CleanText(details.LocalizedName);
            if (string.IsNullOrWhiteSpace(name)) { State.Suspend(); return; }
            string text = name;
            // A notification may outlive its scene; accept optional year/chapter only for the current named area.
            if (_notificationScene == scene.handle && IsAlive(_notification) && _notification.showLocation &&
                UiVisibility.IsVisible(_notification.holder) && IsAlive(_notification.nameText) &&
                TextUtils.CleanText(_notification.nameText.text) == name)
            {
                string visible = ScreenContentReader.Read(_notification.holder, _notification.nameText, null);
                if (!string.IsNullOrWhiteSpace(visible) && visible.StartsWith(name, StringComparison.Ordinal)) text = visible;
            }
            if (details.era != _announcedEra && ZoneEra.Name(details.era) is { } era)
                text = ScreenContentTracker.Combine(new[] { text, era });
            bool available = ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame &&
                !NavigationAudioState.IsSuspended &&
                !ScreenContentAnnouncer.HasActiveContent && !DialogueAnnouncer.HasActiveContent &&
                !ControllerMenuAccessibility.HasActiveContent && GameplayInputState.OwnsInput();
            if (!State.Observe(text, available, out string announcement)) return;
            _announcedEra = details.era;
            ScreenReader.Speak(announcement, interrupt: true);
            Plugin.Log.LogInfo($"[Zone] {announcement}");
        }
        catch (Exception ex) { State.Suspend(); ReportFailure(ex); }
    }

    internal static void Reset()
    {
        State.Reset();
        _notification = null;
        _notificationScene = 0;
        _lastFailure = null;
    }

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Zone feedback unavailable: {failure}");
    }
}
