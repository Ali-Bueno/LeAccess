using System;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;

namespace LeAccess.Accessibility.Items;

internal static class GroundLootAccessibility
{
    private static readonly ScreenContentTracker Tracker = new();
    private static GroundLootSnapshot _current;
    private static string _lastFailure;
    private static int _lastFrame = -1;

    internal static bool HasActiveTarget { get; private set; }
    internal static bool HasPendingContent { get; private set; }

    internal static void Update(bool otherSpeechPending = false)
    {
        if (_lastFrame == Time.frameCount) return;
        _lastFrame = Time.frameCount;
        try
        {
            if (NavigationAudioState.IsSuspended || ScreenContentAnnouncer.HasActiveContent ||
                DialogueAnnouncer.HasActiveContent || InventoryAccessibility.HasActiveContent ||
                !GroundLootProbe.TryCapture(out var snapshot))
            {
                ResetTarget();
                return;
            }

            if (!HasActiveTarget || snapshot.LabelId != _current.LabelId || snapshot.DropId != _current.DropId ||
                snapshot.ItemPointer != _current.ItemPointer)
                ResetTarget();
            _current = snapshot;
            HasActiveTarget = true;

            bool pending = Tracker.Observe(snapshot.LabelId, snapshot.Name,
                ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame && !otherSpeechPending,
                out string announcement);
            HasPendingContent = pending && announcement is null;
            if (announcement is null) return;
            Plugin.Log.LogInfo($"[GroundItem] label={snapshot.LabelId} drop={snapshot.DropId}: {announcement}");
            ScreenReader.Speak(announcement, interrupt: false);
        }
        catch (Exception ex)
        {
            ResetTarget();
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"Ground item feedback unavailable: {ex.Message}");
        }
    }

    private static void ResetTarget()
    {
        Tracker.Reset();
        _current = default;
        HasActiveTarget = false;
        HasPendingContent = false;
    }

    internal static void Reset()
    {
        ResetTarget();
        _lastFrame = -1;
        _lastFailure = null;
    }
}
