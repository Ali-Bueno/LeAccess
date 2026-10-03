using System;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Npcs;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;

namespace LeAccess.Accessibility.Players;

internal static class NearbyPlayerAccessibility
{
    private static readonly NpcNameAnnouncementState Names = new();
    private static double _nextPoll;
    private static string _lastFailure;
    internal static bool HasPendingContent => Names.HasPendingContent;

    internal static void Update(bool otherSpeechPending = false)
    {
        try
        {
            if (NavigationAudioState.IsSuspended || !GameplayInputState.OwnsInput() ||
                ScreenContentAnnouncer.HasActiveContent || DialogueAnnouncer.HasActiveContent)
            { Reset(); return; }
            double now = Time.unscaledTimeAsDouble;
            if (!double.IsFinite(now)) { Reset(); return; }
            if (now < _nextPoll) return;
            _nextPoll = now + NpcCueTiming.PollSeconds;
            bool found = NearbyPlayerProbe.TryRead(out var target);
            ReportFailure(NearbyPlayerProbe.LastFailure);
            if (!found) { Names.Reset(); return; }
            if (!Names.Observe(target.Session, target.InstanceId, target.Name,
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame && !otherSpeechPending,
                now, out string announcement)) return;
            ScreenReader.Speak(announcement, interrupt: false);
            Plugin.Log.LogInfo($"[Nearby player] {announcement}");
        }
        catch (Exception ex) { Reset(); ReportFailure(ex.Message); }
    }

    private static void ReportFailure(string failure)
    {
        if (failure == null || failure == _lastFailure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Nearby player names: {failure}");
    }

    internal static void Reset() { Names.Reset(); _nextPoll = 0; }
}
