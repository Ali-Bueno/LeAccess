using System;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Npcs;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;

namespace LeAccess.Accessibility.Players;

internal static class NearbyPlayerAccessibility
{
    // A party member walks with the player and keeps crossing the native radius; name them again only after a minute away.
    internal const double RepeatGapSeconds = 60;
    private static readonly NpcNameAnnouncementState Names = new(RepeatGapSeconds);
    private static double _nextPoll;
    private static string _lastFailure;
    private static long _knownSession;
    private static int _knownTarget, _knownLevel;
    private static string _knownCharacter;
    internal static bool HasPendingContent => Names.HasPendingContent;

    internal static void Update(bool otherSpeechPending = false)
    {
        try
        {
            double now = Time.unscaledTimeAsDouble;
            if (!double.IsFinite(now)) { Reset(); return; }
            // Menus and windows pause naming without forgetting who was already named.
            if (NavigationAudioState.IsSuspended || !GameplayInputState.OwnsInput() ||
                ScreenContentAnnouncer.HasActiveContent || DialogueAnnouncer.HasActiveContent)
            { Names.Lost(now); _nextPoll = 0; return; }
            if (now < _nextPoll) return;
            _nextPoll = now + NpcCueTiming.PollSeconds;
            bool found = NearbyPlayerProbe.TryRead(out var target);
            ReportFailure(NearbyPlayerProbe.LastFailure);
            if (!found) { Names.Lost(now); return; }
            if (!Names.Observe(target.Session, target.InstanceId, Describe(target),
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame && !otherSpeechPending,
                now, out string announcement)) return;
            ScreenReader.Speak(announcement, interrupt: false);
            Plugin.Log.LogInfo($"[Nearby player] {announcement}");
        }
        catch (Exception ex) { Reset(); ReportFailure(ex.Message); }
    }

    // Known character name and level are kept per target so a flickering read never re-announces.
    private static string Describe(NearbyPlayerTarget target)
    {
        if (target.Session != _knownSession || target.InstanceId != _knownTarget)
        { _knownSession = target.Session; _knownTarget = target.InstanceId; _knownCharacter = null; _knownLevel = 0; }
        if (!string.IsNullOrWhiteSpace(target.Character)) _knownCharacter = target.Character;
        if (target.Level > 0) _knownLevel = target.Level;
        return PlayerIdentityText.Compose(target.Name, _knownCharacter, _knownLevel);
    }

    private static void ReportFailure(string failure)
    {
        if (failure == null || failure == _lastFailure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Nearby player names: {failure}");
    }

    internal static void Reset() { Names.Reset(); _nextPoll = 0; _knownTarget = 0; _knownCharacter = null; _knownLevel = 0; }
}
