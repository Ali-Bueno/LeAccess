using LeAccess.Accessibility;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Navigation.Guidance;
using LeAccess.Accessibility.Audio;
using UnityEngine;
using UnityEngine.AI;

internal static partial class Program
{
    private static void AcceptanceTests()
    {
        ResetService(); AddTarget(); ScreenReader.OutputAccepted = false; Settle();
        Check(ScreenReader.Spoken.Count == 0 && RadarAudioOutput.Plays == 0 && LeAccess.Plugin.Log.Info.Count == 0,
            "Rejected target speech cannot acknowledge identity or unlock the guidance pulse");
        GuidanceTargetReader.Targets.Clear();
        GuidanceTargetReader.Targets.Add(new(1, "Current", new(12, 0, 0), 0, GuidanceTargetKind.Quest));
        MapSectionWorld.World = MapSectionWorld.World with { X = 2 };
        Tick(1.5); ScreenReader.OutputAccepted = true; Tick(2.5);
        Check(ScreenReader.Spoken.Single() == "guidance.automatic.target[Current|guidance.direction.right|10]" && RadarAudioOutput.Plays == 1,
            "Recovery names current target, distance and direction before unlocking a verified pulse");
        Tick(3); Check(ScreenReader.Spoken.Count == 1, "Accepted recovered target is acknowledged once");

        ResetService(); AddTarget(); Settle();
        Camera.main.transform.forward = new(1, 0, 0); Camera.main.transform.right = new(0, 0, -1);
        Tick(7); ScreenReader.OutputAccepted = false; Tick(8);
        Check(ScreenReader.Spoken.Count == 1 && LeAccess.Plugin.Log.Info.Count == 1, "Rejected major turn consumes no delivery or narration cadence");
        Camera.main.transform.forward = new(-1, 0, 0); Camera.main.transform.right = new(0, 0, 1);
        ScreenReader.OutputAccepted = true; Tick(8.5); Tick(9.5);
        Check(ScreenReader.Spoken.Count == 2 && ScreenReader.Spoken.Last() == "guidance.automatic.turn[guidance.direction.right]",
            "Turn recovery uses the fresh settled direction without an unheard six-second gap");
        ResetService(); AddTarget(); Settle();
        MapSectionWorld.World = MapSectionWorld.World with { Z = 9.5 }; ScreenReader.OutputAccepted = false; Tick(7);
        MapSectionWorld.World = MapSectionWorld.World with { Z = 9.7 }; ScreenReader.OutputAccepted = true; Tick(7.5);
        Check(ScreenReader.Spoken.Count == 2 && ScreenReader.Spoken.Last() == "guidance.automatic.near_marker[Objective]" && RadarAudioOutput.Plays == 1,
            "Rejected proximity output retries current near state without consuming cadence or claiming completion");

        foreach (string gate in new[] { "hidden", "ownership", "session" })
        {
            ResetService(); AddTarget(); ScreenReader.OutputAccepted = false; Settle();
            if (gate == "hidden") GuidanceTargetReader.Targets.Clear();
            if (gate == "ownership") MapSectionWorld.Available = false;
            if (gate == "session") MapSectionWorld.World = MapSectionWorld.World with { Session = 2 };
            Tick(1.5); ScreenReader.OutputAccepted = true; Tick(2);
            if (gate == "session")
                Check(ScreenReader.Spoken.Count == 0 && RadarAudioOutput.Plays == 0, "New session settles independently before any recovered output");
            else
            {
                Check(ScreenReader.Spoken.Count == 0 && RadarAudioOutput.Plays == 0, $"Rejected target cannot replay while {gate} invalidates its source");
                MapSectionWorld.Available = true; GuidanceTargetReader.Targets.Clear(); AddTarget(2, "Replacement"); Tick(2.5);
            }
            Tick(3.5);
            Check(ScreenReader.Spoken.Count == 1 && (gate == "session" || ScreenReader.Spoken.Single().Contains("Replacement")),
                $"Recovered {gate} state announces only its current validated target");
        }
        ResetService(); AddTarget(1, "Quest", 3); Set("GuideToNamedExits", false); Set("LocalExitRangeInBodyWidths", 4f);
        GuidanceTargetReader.Targets.Add(new(2, "Return to event", new(0, 0, 100), 0, GuidanceTargetKind.EncounterReturn) { Priority = -1 });
        Settle();
        Check(ScreenReader.Spoken.Single().StartsWith("guidance.automatic.target[Return to event|") &&
            !ScreenReader.Spoken.Single().Contains("guidance.exit_unconfirmed") && RadarAudioOutput.Plays == 1,
            "Verified encounter return bypasses disabled exits and exit range, outranks quests and unlocks its own route pulse");
        ResetService(); Set("GuideToNamedExits", false);
        GuidanceTargetReader.Targets.Add(new(2, "Return to event", new(0, 0, 100), 0, GuidanceTargetKind.EncounterReturn) { Priority = -1 });
        NavMesh.Status = NavMeshPathStatus.PathPartial; Settle();
        Check(ScreenReader.Spoken.Count == 0 && RadarAudioOutput.Plays == 0, "Encounter priority cannot bypass route completeness verification");
        ResetService(); Set("GuideToNamedExits", false);
        GuidanceTargetReader.Targets.Add(new(2, "Return to event", new(0, 0, 0.5f), 0, GuidanceTargetKind.EncounterReturn) { Priority = -1 });
        Settle();
        Check(ScreenReader.Spoken.Single() == "guidance.automatic.near_marker[Return to event]" && RadarAudioOutput.Plays == 0,
            "Near encounter region remains proximity feedback without exit, damage or completion claims");
        AcknowledgementStateTests();
        UrgentEncounterTests();
        PassiveGuidanceAccessibility.Cleanup();
    }

    private static void UrgentEncounterTests()
    {
        void ReturnTarget()
        {
            EncounterGuidanceTargets.HasTrackedPointers = true;
            GuidanceTargetReader.Targets.Add(new(20, "Return to event", new(0, 0, 10), 0, GuidanceTargetKind.EncounterReturn) { Priority = -1 });
        }
        void Urgent(double time, bool content = false)
        { Time.unscaledTimeAsDouble = time; PassiveGuidanceAccessibility.UpdateUrgent(content); }

        ResetService(); AddTarget(); Urgent(0);
        Check(EncounterGuidanceTargets.Reads == 0 && GuidanceTargetReader.Reads == 0 && NavMesh.Calculations == 0,
            "No tracked encounter pointer makes the urgent pass cheap and leaves normal target scanning untouched");
        Settle();
        Check(ScreenReader.Spoken.Single().Contains("[Objective|") && GuidanceTargetReader.Reads == 3,
            "An empty urgent pass cannot consume normal guidance's initial probe or target settle");
        ResetService(); ReturnTarget(); MapSectionWorld.Available = false; Urgent(0);
        Check(EncounterGuidanceTargets.Reads == 0 && GuidanceTargetReader.Reads == 0 && NavMesh.Calculations == 0,
            "Missing player registry fails urgent guidance before collecting pointers or probing a route");
        ResetService(); ReturnTarget(); Urgent(0); Urgent(0.1); Urgent(0.49);
        Check(EncounterGuidanceTargets.Reads == 1 && NavMesh.Calculations == 1,
            "Initial urgent session validation preserves its one-read-per500ms clock");
        Urgent(0.5); Tick(0.5);
        Check(EncounterGuidanceTargets.Reads == 2 && GuidanceTargetReader.Reads == 0 && NavMesh.Calculations == 2,
            "A valid urgent route claims the same tick's normal probe without a second path calculation");
        ResetService(); AddTarget(); Tick(0); ReturnTarget(); Urgent(0.1); Urgent(0.49);
        Check(NavMesh.Calculations == 1 && EncounterGuidanceTargets.Reads == 0,
            "A pointer appearing after an ordinary probe cannot collect or calculate a second path inside the same half-second interval");
        Urgent(0.5);
        Check(NavMesh.Calculations == 2 && EncounterGuidanceTargets.Reads == 1 && ScreenReader.Spoken.Count == 0,
            "The first permitted urgent boundary begins return-target verification and its one-second settle");
        Urgent(1);
        Check(ScreenReader.Spoken.Count == 0 && NavMesh.Calculations == 3,
            "Urgent cadence preserves target settling after discovery without naming provisional return state");
        Urgent(1.5);
        Check(ScreenReader.Spoken.Single().StartsWith("guidance.automatic.target[Return to event|") &&
            RadarAudioOutput.Plays == 1 && NavMesh.Calculations == 4 && EncounterGuidanceTargets.Reads == 3,
            "Return guidance is not starved by the shared route clock and names the verified target at1.5seconds");
        ResetService(); AddTarget(); Settle(); ReturnTarget();
        Tick(1.5, pending: true); Tick(2.5, pending: true);
        Check(ScreenReader.Spoken.Count == 2 && ScreenReader.Spoken.Last().StartsWith("guidance.automatic.target[Return to event|"),
            "A stable encounter return bypasses recent quest cadence and lower-priority pending speech");
        ResetService(); ReturnTarget(); Urgent(0); Urgent(0.5);
        ScreenReader.SpokeThisFrame = true; Urgent(1);
        Check(ScreenReader.Spoken.Count == 0 && RadarAudioOutput.Plays == 0,
            "A same-frame countdown or manual utterance retains priority over urgent encounter naming and pulses");
        ScreenReader.SpokeThisFrame = false; Urgent(1.5);
        Check(ScreenReader.Spoken.Single().StartsWith("guidance.automatic.target[Return to event|") && RadarAudioOutput.Plays == 1,
            "Urgent return names the current verified target at the next clear frame");
        foreach (string gate in new[] { "foreground", "typing", "menu" })
        {
            ResetService(); ReturnTarget();
            EpochInputManager.instance.isInputFieldActive = gate == "typing";
            GameplayInputState.Owned = gate != "menu";
            Urgent(0, gate == "foreground"); Urgent(1, gate == "foreground");
            Check(ScreenReader.Spoken.Count == 0 && RadarAudioOutput.Plays == 0 && EncounterGuidanceTargets.Reads == 0,
                $"Urgent encounter retains {gate} input/content ownership gates before route queries");
        }
        ResetService(); EncounterGuidanceTargets.HasTrackedPointers = true; AddTarget(); Urgent(0); Urgent(1);
        Check(ScreenReader.Spoken.Count == 0 && RadarAudioOutput.Plays == 0 && GuidanceTargetReader.Reads == 0 && NavMesh.Calculations == 0,
            "Tracked pointers with no valid return target cannot produce ordinary quest speech through the urgent path");
    }

    private static void AcknowledgementStateTests()
    {
        var state = new PassiveGuidanceState();
        var sample = Sample(); state.Observe(sample, 0);
        Check(state.TryTakeSpeech(1, out var rejected, acknowledge: false) && !state.HasAnnouncedTarget,
            "Pending observation cannot acknowledge target identity before accepted output");
        state.Observe(sample with { Next = new(10, 0, 0), Distance = 9 }, 1.5);
        Check(!state.Acknowledge(rejected, 1.5) && !state.HasAnnouncedTarget, "Stale direction or distance cannot acknowledge the fresh pending target");
        state.TryTakeSpeech(1.5, out var current, acknowledge: false);
        Check(state.Acknowledge(current, 1.5) && state.HasAnnouncedTarget, "Exact current target can be explicitly acknowledged");
        state.Reset(); state.Observe(sample, 0); state.TryTakeSpeech(1, out rejected, acknowledge: false);
        state.Observe(sample with { TargetId = 11, Name = "Replacement" }, 2);
        Check(!state.Acknowledge(rejected, 3) && !state.HasAnnouncedTarget, "Old target ID cannot acknowledge a replacement target");
        state.TryTakeSpeech(3, out current, acknowledge: false); state.Acknowledge(current, 3);
        state.Observe(sample with { Session = 2, TargetId = 11, Name = "Replacement" }, 4);
        Check(!state.Acknowledge(current, 5) && !state.HasAnnouncedTarget, "Identical target in a new session cannot accept old-session acknowledgement");
        state.TryTakeSpeech(5, out current, acknowledge: false); state.Suspend();
        Check(!state.Acknowledge(current, 6) && !state.PulseDue(6), "Suspended ownership rejects stale acknowledgement and pulse eligibility");
        state.Reset(); state.Observe(sample, 0); state.TryTakeSpeech(1, out _, acknowledge: true);
        var right = sample with { Next = new(10, 0, 0) }; state.Observe(right, 7);
        state.TryTakeSpeech(8, out rejected, acknowledge: false);
        Check(state.TryTakeSpeech(8.5, out current, acknowledge: false) && current.Kind == PassiveGuidanceSpeechKind.Turn,
            "Unaccepted turn does not advance the six-second speech gap");
        Check(state.Acknowledge(current, 8.5) && !state.TryTakeSpeech(9, out _), "Accepted turn consumes cadence once");
        state.Observe(sample with { Distance = 0.5f, Next = System.Numerics.Vector3.Zero }, 15);
        state.TryTakeSpeech(15, out rejected, acknowledge: false);
        Check(state.TryTakeSpeech(15.5, out current, acknowledge: false) && current.Kind == PassiveGuidanceSpeechKind.NearMarker,
            "Unaccepted near state remains eligible without consuming cadence");
        Check(state.Acknowledge(current, 15.5) && !state.TryTakeSpeech(16, out _), "Accepted near state is acknowledged once");
        state.Reset(); state.Observe(sample, 0); state.TryTakeSpeech(1, out _, acknowledge: true);
        state.Observe(sample with { TargetId = 20, Name = "Return to event", TargetKind = GuidanceTargetKind.EncounterReturn }, 1);
        Check(!state.TryTakeSpeech(1.5, out _, acknowledge: false), "Encounter return retains the one-second target settle");
        Check(state.TryTakeSpeech(2, out current, acknowledge: false) && current.TargetKind == GuidanceTargetKind.EncounterReturn,
            "Stable encounter return bypasses a recent quest's six-second speech gap before the native four-second deadline");
        Check(state.Acknowledge(current, 2), "Accepted early encounter target is acknowledged normally");
        state.Observe(sample with { TargetId = 21, Name = "Next quest" }, 2.5);
        Check(!state.TryTakeSpeech(3.5, out _, acknowledge: false), "Ordinary replacement quests retain the speech gap after encounter output");
    }
}
