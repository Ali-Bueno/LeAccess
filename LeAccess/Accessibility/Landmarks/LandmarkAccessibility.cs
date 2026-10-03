using System;
using BepInEx.Configuration;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using UnityEngine;

namespace LeAccess.Accessibility.Landmarks;

internal static class LandmarkAccessibility
{
    // Match the existing nearest-enemy registry's five observations per second.
    private const double PollSeconds = 0.2;
    private static readonly ScreenContentTracker Tracker = new();
    private static ConfigEntry<bool> _enabled;
    private static double _nextPoll;
    private static long _session;
    private static int _target;
    private static string _lastFailure;
    private static string _lastStatus;
    private static double _nextStatus;
    internal static bool HasPendingContent { get; private set; }

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind("Landmarks", "AnnounceNearbyNames", true,
            "Read named signposts and zone/dungeon destinations within the game's interaction range.");
        Reset();
    }

    internal static void Update(bool menuActive = false, bool otherSpeechPending = false)
    {
        if (_enabled == null) return;
        try
        {
            if (!_enabled.Value || LE.UI.LoadingScreen.IsVisible) { Reset(); return; }
            // Losing focus keeps delivered memory so alt-tab does not repeat names.
            if (menuActive || !Application.isFocused || NavigationAudioState.IsSuspended || !GameplayInputState.OwnsInput())
            {
                // Revalidate the target on return, retaining delivered text so
                // closing a menu beside the same sign does not repeat its name.
                HasPendingContent = false;
                _nextPoll = 0;
                return;
            }
            double now = Time.unscaledTimeAsDouble;
            if (now < _nextPoll) return;
            _nextPoll = now + PollSeconds;
            bool found = LandmarkProbe.TryRead(out var target);
            if (now >= _nextStatus && LandmarkProbe.LastStatus != null && _lastStatus != LandmarkProbe.LastStatus)
            {
                _lastStatus = LandmarkProbe.LastStatus;
                _nextStatus = now + 5;
                Plugin.Log.LogInfo($"[LandmarkStatus] {_lastStatus}");
            }
            if (LandmarkProbe.LastFailure != null) ReportFailure(LandmarkProbe.LastFailure);
            if (!found)
            {
                ClearTarget();
                return;
            }
            if (_session != target.Session || _target != target.InstanceId)
            {
                ClearTarget();
                _session = target.Session;
                _target = target.InstanceId;
            }
            string text = ModText.Format("landmark.nearby", target.Name);
            HasPendingContent = Tracker.Observe(_target, text,
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame && !otherSpeechPending,
                out string announcement);
            if (announcement == null) return;
            HasPendingContent = false;
            ScreenReader.Speak(announcement, interrupt: false);
            Plugin.Log.LogInfo($"[Landmark] target={_target}: {announcement}");
        }
        catch (Exception ex)
        {
            Reset();
            ReportFailure(ex.ToString());
        }
    }

    private static void ReportFailure(string failure)
    {
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Nearby landmark feedback unavailable: {failure}");
    }

    private static void ClearTarget()
    {
        Tracker.Reset();
        _target = 0;
        _session = 0;
        HasPendingContent = false;
    }

    private static void Reset() { ClearTarget(); _nextPoll = 0; }

    internal static void Cleanup()
    {
        Reset();
        _enabled = null;
        _lastFailure = null;
        _lastStatus = null;
        _nextStatus = 0;
    }
}
