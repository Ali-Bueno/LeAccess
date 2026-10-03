using BepInEx.Configuration;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Items;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Npcs;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;

int checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}
const double NpcAnnouncementRepeatGap = NpcNameAnnouncementState.RepeatGapSeconds;
void Tick(double now, bool foregroundSpeech = false, bool otherPending = false)
{
    ScreenReader.SpokeThisFrame = foregroundSpeech;
    Time.unscaledTimeAsDouble = now;
    NpcSounds.Update(otherPending);
}
ConfigFile Reset(bool initialize = true)
{
    NpcSounds.Cleanup();
    WorldAudioReservation.Reset();
    NpcWorldProbe.Clear();
    RadarAudioOutput.Clear();
    NpcCueSynthesizer.Calls.Clear();
    LeAccess.Plugin.Log.Info.Clear();
    LeAccess.Plugin.Log.Warnings.Clear();
    NavigationAudioState.IsSuspended = false;
    GameplayInputState.Owns = true;
    ScreenContentAnnouncer.HasActiveContent = false;
    DialogueAnnouncer.HasActiveContent = false;
    InventoryAccessibility.HasActiveContent = false;
    GroundLootAccessibility.HasPendingContent = false;
    ScreenReader.Available = true;
    ScreenReader.SpokeThisFrame = false;
    ScreenReader.Messages.Clear();
    Time.unscaledTimeAsDouble = 0;
    var config = new ConfigFile();
    if (initialize)
    {
        NpcSounds.Initialize(config);
        config.Set("AnnounceNames", false);
    }
    return config;
}
void Target(int id = 1, long session = 1, float distance = 10, float range = 10, float pan = 0.25f, float forward = -0.5f)
{
    NpcWorldProbe.Available = true;
    NpcWorldProbe.Target = new NpcCueTarget(session, id, $"NPC {id}", distance, range, pan, forward);
}

Reset(initialize: false); Target(); Tick(0);
Check(NpcWorldProbe.Captures == 0 && RadarAudioOutput.ConstructorCalls == 0,
    "An uninitialized service does not probe or create an output");
Reset(); Tick(0); Tick(0.11);
Check(NpcWorldProbe.Captures == 2 && RadarAudioOutput.ConstructorCalls == 0,
    "No eligible NPC means no audio device is opened");
Check(LeAccess.Plugin.Log.Warnings.Count == 0, "Normal no-NPC observations remain quiet");
Target(); Tick(0.22);
var output = RadarAudioOutput.Instances.Single();
Check(output.Description == "NPC cues" && output.PlayCalls == 1,
    "First actual target lazily creates its dedicated output and plays one cue");
Check(NpcCueSynthesizer.Calls.Single() == (0.25f, -0.5f, 1f),
    "Current target bearing and configured level reach the synthesizer");
Tick(0.23);
Check(NpcWorldProbe.Captures == 3, "World polling honors the game's service interval");
Tick(0.33); Tick(0.44);
Check(output.PlayCalls == 1 && RadarAudioOutput.ConstructorCalls == 1,
    "An ongoing sample is neither overlapped nor given another output");
output.IsPlaying = false; Tick(0.77);
Check(output.PlayCalls == 2 && RadarAudioOutput.ConstructorCalls == 1,
    "Finished cues reuse the same stream");

Reset(); Target(); Tick(0);
output = RadarAudioOutput.Instances.Single(); output.IsPlaying = false;
Target(id: 2, distance: 0, pan: -1, forward: 1); Tick(0.11); Tick(0.22);
Check(output.PlayCalls == 1 && output.SilenceCalls == 0,
    "Nearest-target switch preserves previous cadence without clearing the output");
Tick(0.44);
Check(output.PlayCalls == 1, "A target switch cannot restart the cue timer early");
Tick(0.55);
Check(output.PlayCalls == 2 && NpcCueSynthesizer.Calls.Last() == (-1f, 1f, 1f),
    "Next scheduled cue uses the latest NPC, not the previous bearing");
output.IsPlaying = false; Tick(0.66);
Check(output.PlayCalls == 2, "Near target still honors the minimum pulse interval");
Tick(0.77);
Check(output.PlayCalls == 3, "Near cadence resumes after its interval");
Check(LeAccess.Plugin.Log.Info.Count(line => line.StartsWith("[NPC sound]")) == 2,
    "Target diagnostics are emitted on changes only");

Reset(); Target(); Tick(0); output = RadarAudioOutput.Instances.Single();
Target(id: 2, session: 2, pan: -1); Tick(0.11);
Check(output.SilenceCalls == 1 && output.PlayCalls == 1,
    "A scene/player change clears old samples and allows queued device audio to finish before another cue");
Tick(0.22);
Check(output.PlayCalls == 2, "The new session resumes once the previous device reservation ends");
Check(RadarAudioOutput.ConstructorCalls == 1, "Session changes reuse the audio stream");

foreach (string reason in new[] { "pause or loading", "menu ownership", "popup", "dialogue", "disabled", "zero volume" })
{
    var config = Reset(); Target(); Tick(0); output = RadarAudioOutput.Instances.Single();
    void SetGate(bool active)
    {
        switch (reason)
        {
            case "pause or loading": NavigationAudioState.IsSuspended = active; break;
            case "menu ownership": GameplayInputState.Owns = !active; break;
            case "popup": ScreenContentAnnouncer.HasActiveContent = active; break;
            case "dialogue": DialogueAnnouncer.HasActiveContent = active; break;
            case "disabled": config.Set("Enabled", !active); break;
            case "zero volume": config.Set("Volume", active ? 0 : 100); break;
        }
    }
    SetGate(true); Tick(0.01);
    Check(output.SilenceCalls == 1 && !output.IsPlaying,
        $"{reason} immediately discards current samples even before next probe");
    int captures = NpcWorldProbe.Captures;
    Tick(0.02); Tick(0.5);
    Check(output.SilenceCalls == 1 && NpcWorldProbe.Captures == captures,
        $"{reason} neither repeatedly cancels nor probes while suspended");
    SetGate(false); Target(id: 2, pan: -0.7f); Tick(0.61);
    Check(output.PlayCalls == 2 && NpcCueSynthesizer.Calls.Last().Pan == -0.7f,
        $"{reason} resumes with the current NPC, never stale queued samples");
    Check(output.DisposeCalls == 0 && RadarAudioOutput.ConstructorCalls == 1,
        $"{reason} keeps the existing output for resumption");
}

foreach (string configGate in new[] { "Enabled", "Volume" })
{
    var config = Reset(); Target();
    if (configGate == "Enabled") config.Set("Enabled", false); else config.Set("Volume", 0);
    Tick(0); Tick(10);
    Check(RadarAudioOutput.ConstructorCalls == 0 && NpcWorldProbe.Captures == 0,
        $"Initial {configGate} suppression does not open audio or query NPCs");
}
var levels = Reset(); levels.Set("Volume", 35); levels.Set("InteractionRangeMultiplier", 2.5f);
Target(); Tick(0);
Check(NpcCueSynthesizer.Calls.Single().Volume == 0.35f && NpcWorldProbe.LastMultiplier == 2.5f,
    "Configured volume and range multiplier are passed through independently");
foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
{
    levels = Reset(); levels.Set("InteractionRangeMultiplier", invalid); Target(); Tick(0);
    Check(NpcWorldProbe.LastMultiplier == 1f, "Invalid multiplier falls back to game interaction range");
}

Reset(); Target(); Tick(0); output = RadarAudioOutput.Instances.Single();
NpcWorldProbe.Available = false; Tick(0.11);
Check(output.SilenceCalls == 1 && !output.IsPlaying, "Losing all eligible NPCs discards an unfinished cue");
Tick(0.22);
Check(output.PlayCalls == 1 && output.SilenceCalls == 1, "No-target interval stays silent");
Target(id: 3); Tick(0.33);
Check(output.PlayCalls == 2 && RadarAudioOutput.ConstructorCalls == 1,
    "A newly available NPC can sound immediately after no-target state");

Reset(); Target(); RadarAudioOutput.ThrowConstructor = true; Tick(0); Tick(0.11); Tick(4.99);
Check(RadarAudioOutput.ConstructorCalls == 1, "Failed creation is not retried before five seconds");
Tick(5.11);
Check(RadarAudioOutput.ConstructorCalls == 2 && LeAccess.Plugin.Log.Warnings.Count == 1,
    "Creation retries after five seconds and deduplicates identical failures");
RadarAudioOutput.ThrowConstructor = false; Tick(9.99);
Check(RadarAudioOutput.ConstructorCalls == 2, "Retry deadline persists after another failed creation");
Tick(10.22);
Check(RadarAudioOutput.ConstructorCalls == 3 && RadarAudioOutput.Instances.Single().PlayCalls == 1,
    "Creation eventually recovers and starts the current target cue");

Reset(); Target(); Tick(0); output = RadarAudioOutput.Instances.Single();
output.IsRunning = false; output.RecoverySucceeds = false;
Tick(0.11); Tick(4.99);
Check(output.RecoveryCalls == 0, "Stopped stream respects the existing five-second recovery throttle");
Tick(5.11);
Check(output.RecoveryCalls == 1 && output.PlayCalls == 1,
    "Recovery is attempted before considering stale pending samples");
NavigationAudioState.IsSuspended = true; Tick(5.12);
NavigationAudioState.IsSuspended = false; Target(id: 4, pan: -0.4f); Tick(5.23); Tick(9.99);
Check(output.RecoveryCalls == 1, "Suspension and target changes cannot bypass failed-recovery throttle");
output.RecoverySucceeds = true; Tick(10.22);
Check(output.RecoveryCalls == 2 && output.PlayCalls == 2 && output.IsRunning,
    "Successful recovery plays again without creating a second managed output");
Check(NpcCueSynthesizer.Calls.Last().Pan == -0.4f && RadarAudioOutput.ConstructorCalls == 1,
    "Recovery synthesizes only the latest NPC bearing");

Reset(); Target(); Tick(0); output = RadarAudioOutput.Instances.Single();
output.IsRunning = false; output.ThrowRecovery = true; Tick(5.01); Tick(5.12); Tick(10.12);
Check(output.RecoveryCalls == 2 && LeAccess.Plugin.Log.Warnings.Count == 1,
    "Thrown recovery failures are throttled and logged once");
output.ThrowRecovery = false; Tick(15.23);
Check(output.IsRunning && output.PlayCalls == 2, "A thrown recovery error does not permanently disable NPC cues");

Reset(); NpcWorldProbe.CaptureFailure = "Native probe failed"; Tick(0); Tick(0.11);
Check(LeAccess.Plugin.Log.Warnings.Count == 1 && RadarAudioOutput.ConstructorCalls == 0,
    "Unexpected probe failures are reported once even though suspension clears the probe cache");
NpcWorldProbe.CaptureFailure = null; Target(); Tick(0.22);
NpcWorldProbe.Available = false; NpcWorldProbe.CaptureFailure = "Native probe failed"; Tick(0.33);
Check(LeAccess.Plugin.Log.Warnings.Count == 2, "Successful playback rearms a later real probe failure report");
Reset(); NpcWorldProbe.Throw = true; Tick(0); Tick(0.11);
Check(LeAccess.Plugin.Log.Warnings.Count == 1 && NpcWorldProbe.Resets == 2,
    "Unexpected uncaught adapter exceptions also reset state and deduplicate warnings");

Reset(); Target(); Tick(0); output = RadarAudioOutput.Instances.Single();
output.IsPlaying = false; output.ThrowPlay = true; Tick(0.55); Tick(0.66); Tick(0.77);
Check(LeAccess.Plugin.Log.Warnings.Count == 1 && output.SilenceCalls >= 3,
    "Repeated submission failures discard state and remain deduplicated across successful probes");
output.ThrowPlay = false; Tick(0.99);
Check(output.Cues.Count == 2, "Submission failure can recover without replacing the output");
output.IsPlaying = false; output.ThrowPlay = true; Tick(1.44);
Check(LeAccess.Plugin.Log.Warnings.Count == 2, "A fresh failure after successful playback is reported again");

Reset(); Target(); Tick(0); output = RadarAudioOutput.Instances.Single();
NpcSounds.Cleanup();
Check(output.DisposeCalls == 1 && output.SilenceCalls == 1 && !output.IsPlaying,
    "Cleanup cancels pending samples and disposes the stream exactly once");
int oldCaptures = NpcWorldProbe.Captures;
Tick(0.11); NpcSounds.Cleanup();
Check(output.DisposeCalls == 1 && NpcWorldProbe.Captures == oldCaptures,
    "Cleanup is idempotent and subsequent updates are inert");
NpcSounds.Initialize(new ConfigFile()); Target(); Tick(0);
Tick(0.22);
Check(RadarAudioOutput.ConstructorCalls == 2 && RadarAudioOutput.Instances.Last().PlayCalls == 1,
    "Reinitialization clears timers and creates one fresh lazy output");
NpcSounds.Cleanup();

Reset(initialize: false); NpcSounds.Initialize(new ConfigFile()); Target(); Tick(0); Tick(0.11);
Check(ScreenReader.Messages.Single().Text == "NPC 1", "NPC name announcements are enabled by default");
var namesConfig = Reset(); namesConfig.Set("AnnounceNames", true); Target(); Tick(0);
Check(ScreenReader.Messages.Count == 0 && NpcSounds.HasPendingName,
    "NPC name waits for a second matching observation while its sound starts immediately");
Tick(0.11);
Check(ScreenReader.Messages.Single() == ("NPC 1", false),
    "NPC approach speaks the current name through the passive centralized sink");
output = RadarAudioOutput.Instances.Single();
for (int index = 1; index <= 8; index++)
{
    output.IsPlaying = false;
    Tick(0.11 + index * 0.55);
}
Check(ScreenReader.Messages.Count == 1 && output.PlayCalls > 2,
    "Repeating sound pulses never repeat an unchanged NPC name");
Check(!NpcSounds.HasPendingName, "Spoken name releases pending foreground priority");

namesConfig = Reset(); namesConfig.Set("AnnounceNames", true); Target(); Tick(0); Tick(0.11);
NpcWorldProbe.Available = false; Tick(0.22); Target(); Tick(0.33); Tick(0.44);
Check(ScreenReader.Messages.Count == 1, "Range-edge flicker of the same NPC stays silent");
NpcWorldProbe.Available = false; Tick(0.55); Target(); Tick(0.55 + NpcAnnouncementRepeatGap); Tick(0.66 + NpcAnnouncementRepeatGap);
Check(ScreenReader.Messages.Count == 2, "Leaving the same NPC for at least the gap reads its name again");
Target(id: 2); NpcWorldProbe.Target = NpcWorldProbe.Target with { Name = "NPC 1" }; Tick(0.77 + NpcAnnouncementRepeatGap); Tick(0.88 + NpcAnnouncementRepeatGap);
Check(ScreenReader.Messages.Count == 3, "Distinct NPCs with identical display names retain distinct identities");
Target(id: 2, session: 2); Tick(0.99 + NpcAnnouncementRepeatGap); Tick(1.1 + NpcAnnouncementRepeatGap);
Check(ScreenReader.Messages.Last().Text == "NPC 2" && ScreenReader.Messages.Count == 4,
    "Scene or player session change resets NPC name state");

foreach (string busy in new[] { "unavailable", "same frame", "ground loot", "inventory", "zone" })
{
    namesConfig = Reset(); namesConfig.Set("AnnounceNames", true); Target();
    if (busy == "unavailable") ScreenReader.Available = false;
    if (busy == "ground loot") GroundLootAccessibility.HasPendingContent = true;
    if (busy == "inventory") InventoryAccessibility.HasActiveContent = true;
    Tick(0, foregroundSpeech: busy == "same frame", otherPending: busy == "zone");
    Tick(0.11, foregroundSpeech: busy == "same frame", otherPending: busy == "zone");
    Check(ScreenReader.Messages.Count == 0 && NpcSounds.HasPendingName,
        $"NPC name yields while {busy} owns speech");
    Target(id: 2); Tick(0.22, foregroundSpeech: busy == "same frame", otherPending: busy == "zone");
    ScreenReader.Available = true;
    GroundLootAccessibility.HasPendingContent = false;
    InventoryAccessibility.HasActiveContent = false;
    Tick(0.33);
    Check(ScreenReader.Messages.Single().Text == "NPC 2", $"Only latest NPC is read after {busy} clears");
    Check(RadarAudioOutput.Instances.Single().PlayCalls >= 1,
        $"{busy} speech priority does not disable existing NPC sound cues");
}

foreach (string gate in new[] { "pause", "dialogue", "menu" })
{
    namesConfig = Reset(); namesConfig.Set("AnnounceNames", true); Target(); Tick(0);
    if (gate == "pause") NavigationAudioState.IsSuspended = true;
    if (gate == "dialogue") DialogueAnnouncer.HasActiveContent = true;
    if (gate == "menu") GameplayInputState.Owns = false;
    Tick(0.11);
    Check(!NpcSounds.HasPendingName && ScreenReader.Messages.Count == 0,
        $"{gate} discards an obsolete unspoken NPC name");
    NpcWorldProbe.Available = false;
    NavigationAudioState.IsSuspended = false;
    DialogueAnnouncer.HasActiveContent = false;
    GameplayInputState.Owns = true;
    Tick(0.22);
    Check(ScreenReader.Messages.Count == 0, $"No stale NPC speech survives {gate} completion");
}

foreach (string audioSetting in new[] { "Enabled", "Volume" })
{
    namesConfig = Reset(); namesConfig.Set("AnnounceNames", true);
    if (audioSetting == "Enabled") namesConfig.Set("Enabled", false); else namesConfig.Set("Volume", 0);
    Target(); Tick(0); Tick(0.11);
    Check(ScreenReader.Messages.Single().Text == "NPC 1" && RadarAudioOutput.ConstructorCalls == 0,
        $"Name speech works independently of muted {audioSetting}");
}
namesConfig = Reset(); namesConfig.Set("AnnounceNames", true); Target();
RadarAudioOutput.ThrowConstructor = true; Tick(0); Tick(0.11);
Check(ScreenReader.Messages.Single().Text == "NPC 1", "Unavailable audio device does not prevent NPC name speech");
namesConfig = Reset(); namesConfig.Set("AnnounceNames", true); Target(); Tick(0);
namesConfig.Set("AnnounceNames", false); Tick(0.11);
Check(!NpcSounds.HasPendingName && ScreenReader.Messages.Count == 0,
    "Disabling NPC names cancels an unsettled name without disabling sounds");
namesConfig.Set("AnnounceNames", true); Tick(0.22); Tick(0.33);
Check(ScreenReader.Messages.Count == 1, "Re-enabling NPC names observes the current target again");

namesConfig = Reset(); namesConfig.Set("AnnounceNames", true); Target();
NpcWorldProbe.Target = NpcWorldProbe.Target with { Name = null }; Tick(0); Tick(0.11);
Check(ScreenReader.Messages.Count == 0 && !NpcSounds.HasPendingName && RadarAudioOutput.ConstructorCalls == 1,
    "Unnamed NPC remains audible without invented speech or permanent pending priority");
NpcWorldProbe.Target = NpcWorldProbe.Target with { Name = "Armero" }; Tick(0.22);
NpcWorldProbe.Target = NpcWorldProbe.Target with { Name = "Herrero" }; Tick(0.33);
Check(ScreenReader.Messages.Count == 0, "Changing localized name restarts settlement");
Tick(0.44);
Check(ScreenReader.Messages.Single().Text == "Herrero", "Late-arriving localized name is announced once stable");
NpcSounds.Cleanup();
Check(!NpcSounds.HasPendingName, "Cleanup clears pending NPC names alongside sound state");

Console.WriteLine($"NPC lifecycle tests passed: {checks} checks.");
