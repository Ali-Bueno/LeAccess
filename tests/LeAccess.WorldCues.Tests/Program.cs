using BepInEx.Configuration;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.WorldCues;
using UnityEngine;

int checks = 0;
void Check(bool result, string message)
{ checks++; if (!result) throw new InvalidOperationException(message); }
void Reset()
{
    WorldCueSounds.Cleanup(); WorldAudioReservation.Reset();
    GroundGoldVisuals.all = new(); GroundItemVisuals.all = new(); WorldObjectClickListener.allListener = new();
    GroundPotionVisuals.all = new(); GroundXPTomeVisuals.all = new(); GroundFavorTomeVisuals.all = new();
    PlayerFinder.player = new(); PlayerFinder.playerActorVisuals = new(); Camera.main = new();
    EpochInputManager.instance = new();
    NavigationAudioState.IsSuspended = false; GameplayInputState.Owns = true;
    LeAccess.Accessibility.ScreenContent.ScreenContentAnnouncer.HasActiveContent = false;
    LeAccess.Accessibility.Dialogue.DialogueAnnouncer.HasActiveContent = false;
    RadarAudioOutput.Instances.Clear(); RadarAudioOutput.Creates = 0; RadarAudioOutput.ThrowInitialize = false;
    LeAccess.Plugin.Log.Info.Clear(); LeAccess.Plugin.Log.Warnings.Clear();
    Time.unscaledTimeAsDouble = 0;
}
GroundGoldVisuals Coin(float x = 2, float z = 0)
{
    var value = new GroundGoldVisuals(); value.transform.position = new(x, 0, z);
    value.worldObjectClickListener.gameObject = value.gameObject;
    GroundGoldVisuals.all.Add(value); WorldObjectClickListener.allListener.Add(value.worldObjectClickListener);
    return value;
}
GroundItemVisuals Equipment(float x = 0, float z = 3)
{
    var value = new GroundItemVisuals(); value.transform.position = new(x, 0, z);
    value.worldObjectClickListener.gameObject = value.gameObject;
    GroundItemVisuals.all.Add(value); WorldObjectClickListener.allListener.Add(value.worldObjectClickListener);
    return value;
}
WorldObjectClickListener Interactable(float x = -2, float z = 0)
{
    var value = new WorldObjectClickListener(); value.transform.position = new(x, 0, z);
    WorldObjectClickListener.allListener.Add(value); return value;
}
void Tick(double now) { Time.unscaledTimeAsDouble = now; WorldCueSounds.Update(); }

Reset();
var probe = new WorldCueProbe();
Check(!probe.TryCapture(out _), "No registry targets means quiet output");
var coin = Coin();
Check(probe.TryCapture(out var target) && target.Key.Category == WorldCueCategory.Coins,
    "A live visible gold pile is classified as coins");
Check(target.Range == EpochInputManager.instance.targetingConfiguration.itemsRange && target.Distance == 2 && target.Pan == 1,
    "Gold bearing and range use world geometry and the current item targeting range");
var equipment = Equipment();
Check(probe.TryCapture(out target) && target.Key.Category == WorldCueCategory.Coins, "Nearest eligible target wins");
EpochInputManager.instance.InteractableTargeting.ActiveTarget = new() { item = equipment.label };
Check(probe.TryCapture(out target) && target.Key.Category == WorldCueCategory.Equipment,
    "The game's current valid label wins over a nearer candidate");
EpochInputManager.instance.InteractableTargeting.ActiveTarget.WasCollected = true;
Check(probe.TryCapture(out target) && target.Key.Category == WorldCueCategory.Coins,
    "Collected selection falls back to nearest without dereferencing the target");
EpochInputManager.instance.InteractableTargeting.WasCollected = true;
Check(probe.TryCapture(out target) && target.Key.Category == WorldCueCategory.Coins,
    "Collected targeting service cannot prevent registry detection");
EpochInputManager.instance.InteractableTargeting.WasCollected = false;
EpochInputManager.instance.InteractableTargeting.ActiveTarget.WasCollected = false;
equipment.label.itemText.alpha = 0;
Check(probe.TryCapture(out target) && target.Key.Category == WorldCueCategory.Equipment,
    "Equipment availability does not depend on selected-label opacity");
equipment.label.lastFilterResult = ItemFiltering.Rule.RuleOutcome.HIDE;
Check(probe.TryCapture(out target) && target.Key.Category == WorldCueCategory.Coins,
    "The authoritative loot filter excludes a hidden current item");
coin.label.itemText.canvasRenderer.cull = true;
Check(probe.TryCapture(out target) && target.Key.Category == WorldCueCategory.Coins,
    "Gold remains detectable while its label is culled");
coin.label.itemText.canvasRenderer.cull = false;
coin.worldObjectClickListener.Available = false;
Check(probe.TryCapture(out _), "Automatic gold pickup does not require click-listener interaction eligibility");
coin.worldObjectClickListener.Available = true;
coin.worldObjectClickListener.interactionRange = 1;
Check(probe.TryCapture(out target) && target.Range == EpochInputManager.instance.targetingConfiguration.itemsRange,
    "Nearby loot uses the game's item detection range instead of its shorter interaction range");
coin.worldObjectClickListener.interactionRange = float.NaN;
EpochInputManager.instance.targetingConfiguration.itemsRange = float.NaN;
Check(!probe.TryCapture(out _), "Non-finite game radius is unavailable");
coin.worldObjectClickListener.interactionRange = 5;
EpochInputManager.instance.targetingConfiguration.itemsRange = 5;
coin.gameObject.activeInHierarchy = false;
Check(!probe.TryCapture(out _), "Pooled visuals cannot produce a cue");
coin.gameObject.activeInHierarchy = true;
coin.WasCollected = true;
Check(!probe.TryCapture(out _), "A collected visual is never dereferenced");
coin.worldObjectClickListener.WasCollected = true;
Check(!probe.TryCapture(out _), "Collected visual and listener wrappers are safe");

Reset(); probe = new(); equipment = Equipment();
equipment.itemData.Equipment = false;
Check(!probe.TryCapture(out _), "Crafting material is not misclassified as equipment or interactable");
equipment.itemData.Equipment = true; equipment.label.canvasGroup.alpha = 0;
Check(probe.TryCapture(out _), "An unfiltered item is detected without rendering its label");
equipment.label.canvasGroup.alpha = 1;
equipment.itemData.WasCollected = true;
Check(!probe.TryCapture(out _), "Collected item data is skipped before native classification");
equipment.itemData.WasCollected = false;
equipment.worldObjectClickListener.Throw = true; equipment.gameObject.activeInHierarchy = false;
var chest = Interactable();
Check(probe.TryCapture(out target) && target.Key.Category == WorldCueCategory.Interactable && target.Pan == -1,
    "One failed candidate does not discard another available world object");
Check(probe.LastFailure == null, "Loot never calls the click listener availability method");
chest.Npc = true;
Check(!probe.TryCapture(out _), "NPCs retain their distinct service and sound");
chest.Npc = false; chest.requiresClick = false;
Check(probe.TryCapture(out target) && target.Key.Category == WorldCueCategory.Interactable,
    "Available doors and transitions need not require a mouse click");
chest.requiresClick = true; equipment.worldObjectClickListener.Throw = false; equipment.gameObject.activeInHierarchy = true;
PlayerFinder.playerActorVisuals.dead = true;
Check(!probe.TryCapture(out _), "Dead player receives no world cues");
PlayerFinder.playerActorVisuals.dead = false;
Camera.main.WasCollected = true; Camera.main = new();
Check(probe.TryCapture(out _), "A collected cached camera can be replaced");
WorldObjectClickListener.allListener.WasCollected = true; GroundItemVisuals.all.WasCollected = true;
Check(!probe.TryCapture(out _), "Collected registries are never enumerated");

Reset();
var state = new WorldCueState();
target = new(1, new(WorldCueCategory.Coins, 1), 1, 5, 0, 1);
Check(state.IsDue(target, 0), "Initial target announces"); state.MarkPlayed(target, 0);
Check(state.IsDue(target, 99), "An unchanged nearby target remains a repeating beacon");
var changed = target with { Key = new(WorldCueCategory.Equipment, 2) };
state.Reset(); state.MarkPlayed(target, 0);
Check(!state.IsDue(changed, 0.1), "Rapid target cycling cannot burst cues");
Check(state.IsDue(changed, 0.45), "Latest target can play at the minimum interval");
Check(state.IsDue(target with { Session = 2 }, 0.1), "A new scene clears target deduplication");
Check(!state.IsDue(target, double.NaN), "Invalid clocks do not schedule cues");
state.Reset(); Check(state.IsDue(target, 0), "Leaving all target ranges rearms a later approach");

state = new();
WorldCueTarget?[] nearby = { target, changed, target with { Key = new(WorldCueCategory.Interactable, 3), Distance = 3 } };
Check(state.TryChoose(nearby, target, 0, out var chosen) && chosen.Key.Category == WorldCueCategory.Coins,
    "The closest preferred category starts a group of nearby cues");
state.MarkPlayed(chosen, 0);
Check(!state.TryChoose(nearby, target, 0.1, out _), "Different categories still respect the global burst limit");
Check(state.TryChoose(nearby, target, 0.45, out chosen) && chosen.Key.Category == WorldCueCategory.Equipment,
    "A persistent nearer gold pile cannot starve equipment");
state.MarkPlayed(chosen, 0.45);
Check(state.TryChoose(nearby, target, 0.9, out chosen) && chosen.Key.Category == WorldCueCategory.Interactable,
    "Persistent loot cannot starve a nearby door");
state.MarkPlayed(chosen, 0.9);
Check(state.TryChoose(nearby, target, 10, out chosen) && chosen.Key.Category == WorldCueCategory.Coins, "A stationary group loops back to the least recently played category");
nearby[1] = null; state.TryChoose(nearby, target, 11, out _); nearby[1] = changed;
Check(state.TryChoose(nearby, target, 12, out chosen) && chosen.Key.Category == WorldCueCategory.Equipment,
    "Leaving and reentering one category rearms it while the others remain quiet");

Reset();
Check(WorldAudioReservation.TryBegin(WorldAudioSource.Npc, 0, 0.084, 50), "First source reserves audio");
Check(!WorldAudioReservation.TryBegin(WorldAudioSource.Guidance, 0.1, 0.12, 100), "Reservation includes queued device samples");
Check(!WorldAudioReservation.TryBegin(WorldAudioSource.Object, 0.14, 0.16, 100), "Later source cannot jump ahead of waiting guidance");
Check(WorldAudioReservation.TryBegin(WorldAudioSource.Guidance, 0.14, 0.12, 100), "Oldest pending source gets its turn");
Check(!WorldAudioReservation.TryBegin(WorldAudioSource.Npc, 0.4, 0.084, 50), "Repeating NPC does not starve waiting objects");
Check(WorldAudioReservation.TryBegin(WorldAudioSource.Object, 0.4, 0.16, 100), "Object source receives its fair turn");
WorldAudioReservation.Cancel(WorldAudioSource.Npc);
Check(WorldAudioReservation.TryBegin(WorldAudioSource.Guidance, 0.7, 0.12, 100), "Suspending a waiter removes its claim");
Check(WorldAudioReservation.TryBegin(WorldAudioSource.Npc, 0, 0.084, 50), "Time reversal clears stale device reservations");
Check(!WorldAudioReservation.TryBegin(WorldAudioSource.Npc, double.NaN, 0.1, 50), "Invalid reservation times are rejected");

WorldAudioReservation.Reset();
Check(WorldAudioReservation.TryBegin(WorldAudioSource.Guidance, 0, 0.3, 100), "Guidance reserves its natural sample and device tail");
Check(WorldAudioReservation.TryBegin(WorldAudioSource.Terrain, 0.1, 0.12, 100), "Terrain never waits behind a guidance pulse");
Check(!WorldAudioReservation.TryBegin(WorldAudioSource.Object, 0.35, 0.16, 100), "Other sounds still wait for the guidance pulse");
Check(!WorldAudioReservation.TryBegin(WorldAudioSource.Guidance, 0.4, 0.3, 100), "Repeating guidance cannot starve a waiting object");
Check(WorldAudioReservation.TryBegin(WorldAudioSource.Object, 0.4, 0.16, 100), "The waiting object receives the next turn");
Check(!WorldAudioReservation.TryBegin(WorldAudioSource.Npc, 0.7, 0.084, 50), "Later NPC cue respects waiting guidance");
WorldAudioReservation.Cancel(WorldAudioSource.Guidance);
Check(WorldAudioReservation.TryBegin(WorldAudioSource.Npc, 0.7, 0.084, 50), "Losing a route removes its waiting guidance claim");
Check(WorldAudioReservation.TryBegin(WorldAudioSource.Terrain, 0.71, 0.12, 100), "Terrain plays over a running NPC tap");
Check(!WorldAudioReservation.TryBegin(WorldAudioSource.Npc, 0.9, 0.084, 50), "The next tap waits for the terrain cue and its buffer");
WorldAudioReservation.Reset();
Check(WorldAudioReservation.TryBegin(WorldAudioSource.Guidance, 0, 0.3, 100), "Scene reset clears all four audio sources");

var categoryCues = new List<float[]>();
foreach (WorldCueCategory category in Enum.GetValues<WorldCueCategory>())
{
    var center = WorldCueSynthesizer.Synthesize(category, 0, 1, 1); categoryCues.Add(center);
    Check(center.Length == Math.Round(WorldCueSynthesizer.Duration(category) * 48000) * 2,
        $"{category} uses exact stereo sample duration");
    Check(center.All(float.IsFinite) && center.All(x => Math.Abs(x) <= 1) && center.Any(x => Math.Abs(x) > 0.01),
        $"{category} is audible, finite and unclipped");
    Check(center[0] == 0 && Math.Abs(center[^1]) < 1e-10, $"{category} starts and ends without a discontinuity");
    var left = WorldCueSynthesizer.Synthesize(category, -1, 1, 1);
    var right = WorldCueSynthesizer.Synthesize(category, 1, 1, 1);
    Check(left.Where((_, i) => i % 2 == 1).All(x => x == 0) && right.Where((_, i) => i % 2 == 0).All(x => x == 0),
        $"{category} left and right remain spatially distinct");
    Check(!center.SequenceEqual(WorldCueSynthesizer.Synthesize(category, 0, -1, 1)), $"{category} encodes rear position by pitch");
    Check(WorldCueSynthesizer.Synthesize(category, float.NaN, float.NaN, float.NaN).All(x => x == 0),
        $"{category} safely bounds invalid input");
}
Check(!categoryCues[0].SequenceEqual(categoryCues[1]) && !categoryCues[0].SequenceEqual(categoryCues[2]) &&
    !categoryCues[1].SequenceEqual(categoryCues[2]), "Each category retains a distinct sound");

Reset(); var config = new ConfigFile(); WorldCueSounds.Initialize(config); Tick(0);
Check(RadarAudioOutput.Creates == 0, "No targets create no device");
coin = Coin(); Tick(0.11); Tick(0.22); Tick(0.6);
var output = RadarAudioOutput.Instances.Single();
Check(output.Plays == 2, "A stable target repeats and keeps one persistent output");
chest = Interactable(-1); output.IsPlaying = true; Tick(0.71);
Check(output.Plays == 2, "New targets wait until the previous provider samples are consumed");
output.IsPlaying = false; Tick(1.06);
Check(output.Plays == 3 && RadarAudioOutput.Creates == 1, "A new nearest category reuses the stream");
GameplayInputState.Owns = false; Tick(1.07);
Check(output.Silences == 1, "Opening a menu silences pending object samples before poll time");
Tick(1.08); Check(output.Silences == 1, "Suspension does not repeatedly cancel the provider");
GameplayInputState.Owns = true; Tick(1.7);
Check(output.Plays == 4, "Gameplay resumption captures the current target");
WorldCueSounds.Cleanup(); WorldCueSounds.Cleanup();
Check(output.Disposed, "Cleanup releases the persistent output");

Reset(); WorldCueSounds.Initialize(new ConfigFile()); Coin(); RadarAudioOutput.ThrowInitialize = true;
Tick(0); Tick(0.11); Tick(4.9);
Check(RadarAudioOutput.Creates == 1 && LeAccess.Plugin.Log.Warnings.Count == 1, "Failed device initialization is bounded and deduplicated");
RadarAudioOutput.ThrowInitialize = false; Tick(5.1);
Check(RadarAudioOutput.Creates == 2 && RadarAudioOutput.Instances.Single().Plays == 1,
    "Audio retries after the existing five-second recovery interval");
WorldCueSounds.Cleanup();

Reset(); WorldCueSounds.Initialize(new ConfigFile()); coin = Coin(); Tick(0);
output = RadarAudioOutput.Instances.Single(); output.IsPlaying = true;
GroundGoldVisuals.all.Clear(); coin.gameObject.activeInHierarchy = false;
Tick(0.11);
Check(output.Silences == 1 && output.Plays == 1,
    "Automatic pickup stops queued sound when no nearby target remains");
NavigationAudioState.IsSuspended = true; Tick(0.12);
Check(output.Silences == 2, "Scene/menu suspension still cancels pending audio immediately");
WorldCueSounds.Cleanup();

Reset(); probe = new(); coin = Coin(); coin.label = null;
Check(probe.TryCapture(out target) && target.Key.Category == WorldCueCategory.Coins,
    "A live gold pile needs no label at all");
Reset(); probe = new(); chest = Interactable(); chest.transform.position = new(1, 20, 0);
Check(!probe.TryCapture(out _), "An elevated object normally preserves its native vertical range");
chest.controllerTargetingUsePlayerY = true;
Check(probe.TryCapture(out target) && target.Distance == 1,
    "Native controllerTargetingUsePlayerY makes raised interaction anchors reachable");
Reset(); probe = new(); equipment = Equipment(0, 6);
EpochInputManager.instance.targetingConfiguration.itemsRange = 7;
Check(probe.TryCapture(out target) && target.Range == 7,
    "Changed runtime item targeting range is used without a guessed constant");
EpochInputManager.instance.targetingConfiguration.itemsRange = 4;
Check(!probe.TryCapture(out _), "Item range changes constrain subsequent scans");
Check(probe.Diagnostics.Contains("outOfRange=1"), "Rejected range is visible in diagnostics");
equipment.transform.position = new(0, 0, 3);
EpochInputManager.instance.targetingConfiguration.WasCollected = true;
Check(probe.TryCapture(out target) && target.Range == equipment.worldObjectClickListener.interactionRange,
    "A collected targeting configuration falls back to the live listener's native range");
EpochInputManager.instance.targetingConfiguration = null;
Check(probe.TryCapture(out target), "A missing targeting configuration does not suppress nearby live loot");

Reset(); WorldCueSounds.Initialize(new ConfigFile()); Coin(1); Equipment(0, 2); Interactable(-3);
Tick(0); Tick(0.5); Tick(1); Tick(2);
Check(RadarAudioOutput.Instances.Single().Plays == 4,
    "Stable coins, equipment and an interactable cycle repeatedly without starving a category");
Check(LeAccess.Plugin.Log.Info.Count(x => x.StartsWith("[World sounds]")) == 1,
    "World diagnostics are bounded independently of the target polling rate");
WorldCueSounds.Cleanup();
Reset(); probe = new(); coin = Coin(); coin.worldObjectClickListener = null;
Check(probe.TryCapture(out target) && target.Key.Category == WorldCueCategory.Coins,
    "Registered positive gold has no mandatory click listener");
coin.worldObjectClickListener = new() { isActiveAndEnabled = false, Available = false, Throw = true };
Check(probe.TryCapture(out target), "Disabled optional gold listener does not control automatic pickup availability");
coin.goldValue = 0;
Check(!probe.TryCapture(out _), "Cleared native gold value stops eligibility before registry removal");
Reset(); probe = new(); equipment = Equipment(); equipment.worldObjectClickListener = null;
Check(probe.TryCapture(out target) && target.Key.Category == WorldCueCategory.Equipment,
    "Unfiltered equipment needs no click listener either");
equipment.label = null;
Check(!probe.TryCapture(out _) && probe.Diagnostics.Contains("missingLabel=1"),
    "Missing authoritative filter label remains unavailable and separately diagnosable");
Reset(); probe = new(); coin = Coin(); coin.gameObject.scene = new(2);
Check(!probe.TryCapture(out _), "Retained loot from another scene cannot produce a cue");
PlayerFinder.player.scene = new(2);
Check(!probe.TryCapture(out _) && !probe.HasWorld, "A retained player from another scene cannot own the world snapshot");

Reset(); probe = new(); coin = Coin();
Camera.main.isActiveAndEnabled = false;
Check(!probe.TryCapture(out _) && !probe.HasWorld,
    "A disabled main camera cannot authorize a new world-cue snapshot");
Camera.main.isActiveAndEnabled = true;
Check(probe.TryCapture(out _), "Reactivated main camera restores live world cues");
Camera.main.isActiveAndEnabled = false;
Check(!probe.TryCapture(out _) && !probe.HasWorld,
    "Reacquiring the same disabled cached camera cannot retain world cues");
Camera.main = new();
Check(probe.TryCapture(out _), "A new enabled camera restores cues after disabled-camera suspension");

state = new();
target = new(1, new(WorldCueCategory.Coins, 1), 1, 5, 0, 1);
nearby = new WorldCueTarget?[] { target, target with { Key = new(WorldCueCategory.Equipment, 2) },
    target with { Key = new(WorldCueCategory.Interactable, 3) } };
var played = new List<WorldCueCategory>();
for (int i = 0; i < 12; i++)
{
    Check(state.TryChoose(nearby, target, i * 0.5, out chosen), "Each subsequent interval admits a live looping cue");
    played.Add(chosen.Key.Category); state.MarkPlayed(chosen, i * 0.5);
}
Check(played.SequenceEqual(Enumerable.Range(0, 12).Select(i => (WorldCueCategory)(i % 3))),
    "Preferred persistent coins cannot starve either category across four complete cycles");
Reset(); WorldCueSounds.Initialize(new ConfigFile()); coin = Coin(); chest = Interactable(); Tick(0);
output = RadarAudioOutput.Instances.Single(); output.IsPlaying = true;
GroundGoldVisuals.all.Clear(); coin.gameObject.activeInHierarchy = false; Tick(0.11);
Check(output.Silences == 1, "Pickup cancels the playing coin even while another nearby category remains");
output.IsPlaying = false; Tick(0.5); Tick(1);
Check(output.Plays == 3, "The remaining interactable continues repeating after loot disappears");
Check(LeAccess.Plugin.Log.Info.Count(x => x.StartsWith("[World cue]")) == 2,
    "Looping playback does not produce a per-beep information log");
WorldCueSounds.Cleanup();
// Landmarks: exits, waypoints and the stash sound from beyond their native click radius.
Reset(); probe = new();
var waypoint = Interactable(-8); waypoint.interactionRange = 2; waypoint.ServiceKey = "service.waypoint";
Check(!probe.TryCapture(out _), "Without a landmark range a waypoint is limited to its native radius");
probe.LandmarkRange = 12;
Check(probe.TryCapture(out target) && target.Sound == NaturalCueKind.Portal && target.Pan == -1,
    "A waypoint eight metres away sounds within the landmark range");
Check(Math.Abs(target.Gain - WorldCueReach.Gain(8, 2, 12)) < 1e-6 && target.Gain < 1 && target.Gain > WorldCueReach.FarGain,
    "A distant landmark is quieter but still audible");
var lever = Interactable(-6); lever.interactionRange = 2;
Check(probe.TryCapture(out target) && target.Key.InstanceId == waypoint.GetInstanceID(),
    "An ordinary object beyond its native radius does not take the landmark's turn");
lever.transform.position = new(-1.5f, 0, 0);
Check(probe.TryCapture(out target) && target.Key.InstanceId == lever.GetInstanceID() && target.Gain == 1 && target.Sound == null,
    "A nearer object within its native radius plays at full volume");
WorldObjectClickListener.allListener.Remove(lever);
waypoint.transform.position = new(-1, 0, 0);
Check(probe.TryCapture(out target) && target.Gain == 1, "Within its native radius a landmark plays at full volume");
waypoint.transform.position = new(-12.3f, 0, 0);
Check(probe.TryCapture(out target) && target.Key.InstanceId == waypoint.GetInstanceID(),
    "A sounding landmark keeps its turn just past the audible edge");
waypoint.transform.position = new(-12.6f, 0, 0);
Check(!probe.TryCapture(out _), "Beyond the edge margin the landmark stops");
waypoint.transform.position = new(-12.3f, 0, 0);
Check(!probe.TryCapture(out _), "Re-entering needs the real edge, not the held margin");
waypoint.ServiceKey = "service.merchant"; waypoint.transform.position = new(-8, 0, 0);
Check(!probe.TryCapture(out _), "A distant non-landmark service keeps its native radius");
var door = Interactable(-2.3f); door.interactionRange = 2;
Check(!probe.TryCapture(out _), "Just outside the native radius an ordinary object is silent");
door.transform.position = new(-1.9f, 0, 0);
Check(probe.TryCapture(out _), "Inside, it sounds");
door.transform.position = new(-2.3f, 0, 0);
Check(probe.TryCapture(out target) && target.Key.InstanceId == door.GetInstanceID(),
    "Stepping just past the edge does not chop a sounding object");
Check(Math.Abs(WorldCueReach.Gain(12, 2, 12) - WorldCueReach.FarGain) < 1e-6 && WorldCueReach.Gain(1, 2, 12) == 1 &&
    WorldCueReach.Gain(5, 2, 2) == 1 && WorldCueReach.Gain(float.NaN, 2, 12) == 1,
    "Landmark gain is full inside, FarGain at the edge, and safe for degenerate ranges");
Check(WorldCueReach.IsLandmark(NaturalCueKind.ZoneTransition) && WorldCueReach.IsLandmark(NaturalCueKind.Stash) &&
    !WorldCueReach.IsLandmark(NaturalCueKind.Chest) && !WorldCueReach.IsLandmark(null),
    "Exits, waypoints and the stash are landmarks; chests are not");
Reset(); WorldCueSounds.Initialize(new ConfigFile()); waypoint = Interactable(-8); waypoint.interactionRange = 2;
waypoint.ServiceKey = "service.waypoint"; Tick(0);
Check(RadarAudioOutput.Instances.Single().Plays == 1, "The default configuration lets a waypoint sound from eight metres");
WorldCueSounds.Cleanup();
// Continuous loops (D2RAccess item_audio): loopable objects leave the repeated cue for the loop mixer.
Reset(); probe = new(); WorldLoopBank.Available = true;
probe.Loopable = WorldLoopBank.Has; probe.LoopRange = 12; probe.LandmarkRange = 12;
coin = Coin(2); var behind = Equipment(0, -3);
waypoint = Interactable(-8); waypoint.interactionRange = 2; waypoint.ServiceKey = "service.waypoint";
var plain = Interactable(1.5f); plain.interactionRange = 2;
Check(probe.TryCapture(out target) && target.Key.InstanceId == plain.GetInstanceID() &&
    probe.Targets[(int)WorldCueCategory.Coins] == null && probe.Targets[(int)WorldCueCategory.Equipment] == null,
    "Loot and landmarks loop; an unclassified object keeps the repeated cue");
Check(probe.Loops.Count == 3 && probe.Loops.Any(l => l.Kind == NaturalCueKind.Gold && l.Right == 1) &&
    probe.Loops.Any(l => l.Kind == NaturalCueKind.Equipment && Math.Abs(l.Front + 3) < 1e-4) &&
    probe.Loops.Any(l => l.Kind == NaturalCueKind.Portal && l.Right == -1 && Math.Abs(l.Distance - 8) < 1e-4),
    "Loop candidates carry kind, distance, pan and metres ahead");
waypoint.transform.position = new(-13, 0, 0);
Check(probe.TryCapture(out _) && probe.Loops.All(l => l.Kind != NaturalCueKind.Portal), "Beyond the loop range a landmark is silent");
waypoint.transform.position = new(-8, 0, 0); waypoint.Available = false;
Check(probe.TryCapture(out _) && probe.Loops.All(l => l.Kind != NaturalCueKind.Portal), "An unusable landmark does not loop");
waypoint.Available = true;
probe.Loopable = kind => kind != NaturalCueKind.Portal;
WorldObjectClickListener.allListener.Remove(plain);
Check(probe.TryCapture(out target) && target.Sound == NaturalCueKind.Portal && probe.Loops.Count == 2,
    "A kind the mixer cannot play falls back to its repeated cue");
probe.Loopable = null;
Check(probe.TryCapture(out _) && probe.Loops.Count == 0 && probe.Targets[(int)WorldCueCategory.Coins].HasValue,
    "With loops off every object uses the repeated cue");

// Potions and tomes: their own registries, loop-only.
Reset(); probe = new(); WorldLoopBank.Available = true;
probe.Loopable = WorldLoopBank.Has; probe.LoopRange = 12;
var potion = new GroundPotionVisuals(); potion.transform.position = new(0, 0, 4);
potion.worldObjectClickListener.gameObject = potion.gameObject;
GroundPotionVisuals.all.Add(potion); WorldObjectClickListener.allListener.Add(potion.worldObjectClickListener);
var tome = new GroundXPTomeVisuals(); tome.transform.position = new(3, 0, 0); GroundXPTomeVisuals.all.Add(tome);
var favor = new GroundFavorTomeVisuals(); favor.transform.position = new(30, 0, 0); GroundFavorTomeVisuals.all.Add(favor);
Check(!probe.TryCapture(out _) && probe.Loops.Count == 2 &&
    probe.Loops.Any(l => l.Kind == NaturalCueKind.Potion && Math.Abs(l.Front - 4) < 1e-4) &&
    probe.Loops.Any(l => l.Kind == NaturalCueKind.Tome && l.Right == 1),
    "A potion and a near tome loop; their listeners are not generic interactables; a far tome is out of range");
Check(probe.Diagnostics.Contains("registry=0/0/0/3"), "Pickups are counted in diagnostics");
potion.gameObject.activeInHierarchy = false;
Check(!probe.TryCapture(out _) && probe.Loops.Single().Kind == NaturalCueKind.Tome, "An inactive potion is silent");
probe.Loopable = null;
Check(!probe.TryCapture(out _) && probe.Loops.Count == 0, "With loops off potions and tomes stay silent, as before");
GroundPotionVisuals.all.WasCollected = true; probe.Loopable = WorldLoopBank.Has;
Check(!probe.TryCapture(out _) && probe.Loops.Single().Kind == NaturalCueKind.Tome, "A collected registry is never enumerated");
WorldLoopBank.Available = false;

// Selection: exits first, at most three chests, eight voices, D2R gain law and pile limit.
var candidates = new List<LoopCandidate>();
for (int i = 1; i <= 10; i++) candidates.Add(new(100 + i, NaturalCueKind.Gold, i * 0.5f, 0, 1));
for (int i = 0; i < 4; i++) candidates.Add(new(200 + i, NaturalCueKind.Chest, 2 + i, 0, 1));
candidates.Add(new(300, NaturalCueKind.Portal, 11, -1, -2));
var voices = new List<LoopVoice>();
WorldLoopSelection.Select(candidates, 12, 1, voices);
Check(voices.Count == WorldLoopSelection.MaxVoices && voices[0].Kind == NaturalCueKind.Portal &&
    voices.Count(v => v.Kind == NaturalCueKind.Chest) == WorldLoopSelection.MaxChestVoices,
    "The way out claims a voice first and chests are capped before loot fills the rest");
Check(voices.Where(v => v.Kind == NaturalCueKind.Gold).Select(v => v.Id).SequenceEqual(new[] { 101, 102, 103, 104 }),
    "Loot voices go to the nearest piles");
float Scalar(LoopVoice v) => MathF.Sqrt(v.Left * v.Left + v.Right * v.Right);
Check(voices.Sum(Scalar) <= WorldLoopSelection.MixGainLimit + 1e-4, "A loud pile is limited together");
Check(voices[0].Right < 1e-6 && voices[0].Left > 0 && Math.Abs(voices[0].Pitch - WorldLoopSelection.RearPitch) < 1e-6,
    "A landmark to the left and behind pans left and drops its pitch");
Check(Math.Abs(WorldLoopSelection.Gain(0, 12) - 0.4f) < 1e-6 && Math.Abs(WorldLoopSelection.Gain(6, 12) - 0.1f) < 1e-6 &&
    WorldLoopSelection.Gain(12, 12) == 0 && WorldLoopSelection.Gain(float.NaN, 12) == 0 && WorldLoopSelection.Gain(1, 0) == 0,
    "Gain falls with the square of the remaining proximity");
Check(WorldLoopSelection.Pitch(1) == 1 && Math.Abs(WorldLoopSelection.Pitch(-0.5f) - 0.97f) < 1e-6 && WorldLoopSelection.Pitch(float.NaN) == 1,
    "Rear pitch blends across one metre behind the player");
var single = new List<LoopCandidate> { new(1, NaturalCueKind.Stash, 6, 0, 1) };
WorldLoopSelection.Select(single, 12, 0.5f, voices);
Check(Math.Abs(Scalar(voices.Single()) - 0.05f) < 1e-5, "Volume scales a single voice below the pile limit");
WorldLoopSelection.Select(single, 0, 1, voices);
Check(voices.Count == 0, "A zero range plays nothing");

// Playback wiring.
Reset(); WorldLoopBank.Available = true; LoopAudioOutput.Instances.Clear();
var loopConfig = new ConfigFile(); WorldCueSounds.Initialize(loopConfig); coin = Coin(2); Tick(0);
var loopOutput = LoopAudioOutput.Instances.Single();
Check(loopOutput.Voices.Single().Kind == NaturalCueKind.Gold && RadarAudioOutput.Instances.Count == 0,
    "Gold loops through the mixer without a repeated cue");
Tick(0.11); Tick(0.22);
Check(loopOutput.Voices.Count == 1 && LeAccess.Plugin.Log.Info.Count(x => x.StartsWith("[World loop]")) == 1,
    "A continuing loop is updated, not restarted or re-logged");
NavigationAudioState.IsSuspended = true; Tick(0.33);
Check(loopOutput.Silences > 0 && loopOutput.Voices.Count == 0, "Suspension fades loops out");
NavigationAudioState.IsSuspended = false; Tick(0.44);
Check(loopOutput.Voices.Count == 1, "Loops resume after suspension");
GroundGoldVisuals.all.Clear(); Tick(0.55);
Check(loopOutput.Voices.Count == 0, "Pickup removes the loop");
loopConfig.Set("Loops", false); coin = Coin(2); Tick(0.66);
Check(RadarAudioOutput.Instances.Single().Plays == 1 && loopOutput.Voices.Count == 0, "Loops off restores the repeated cue");
WorldCueSounds.Cleanup();
Check(loopOutput.Disposed, "Cleanup releases the loop device");
WorldLoopBank.Available = false; LoopAudioOutput.Instances.Clear();
checks += NaturalIdentityTests.Run();
Console.WriteLine($"World cue tests passed: {checks} checks.");
