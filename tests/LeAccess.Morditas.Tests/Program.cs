using System.Numerics;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Exploration;
using LeAccess.Accessibility.Morditas;
using LeAccess.Accessibility.ScreenContent;

int checks = 0;
void Equal<T>(T expected, T actual, string name)
{
    checks++;
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{name}: expected '{expected}', got '{actual}'");
}

// The test's own templates and game texts: expected speech is composed from them, never from the shipped catalogs.
var words = new Dictionary<string, string>
{
    ["morditas.hunt_goal"] = "goal {0}", ["morditas.kills_of"] = "{0}/{1}", ["morditas.kills"] = "{0} kills",
    ["morditas.requirement_met"] = "enough {0}", ["morditas.next_charge"] = "next {0}", ["morditas.time_left"] = "{0}s",
    ["morditas.time_left_one"] = "1s", ["morditas.stay_within"] = "within {0}", ["morditas.enemies_left"] = "{0} left",
    ["morditas.enemies_left_one"] = "1 left", ["morditas.outside"] = "outside", ["morditas.centre"] = "centre {0} {1}",
    ["morditas.abandon"] = "back in {0}", ["morditas.inside"] = "inside", ["morditas.complete"] = "{0} done",
    ["exploration.steps_one"] = "1 step", ["exploration.steps_many"] = "{0} steps", ["guidance.direction.down"] = "down",
};
MorditasText text = (key, arguments) => string.Format(words[key], arguments);
string Say(params string[] parts) => ScreenContentTracker.Combine(parts);
var game = new MorditasGameTexts("Rage", "Event Started", "Defeat many.", "Ritual", "Revive them.", "Defeat bloodcrafted.",
    "Interact.", "Charge Gained", "Event Failed", "Not impressed.");
// RageOfMorditasData charge thresholds, timer warnings and out-of-circle time; a 22 m circle at one-metre steps.
var rules = new MorditasRules(new[] { 20, 50, 90, 140, 200 }, 15, 5, 4, 22);
const int Encounter = 7;
MorditasSample Hunt(int kills, float left, int encounter = Encounter) => new(encounter, MorditasPhase.Hunt, false, kills, left, 0);
MorditasSample Gauntlet(int huntKills, int gauntletKills, bool? inside = null, bool allOut = false, int encounter = Encounter) =>
    new(encounter, MorditasPhase.Gauntlet, false, huntKills, 0, gauntletKills, inside, allOut, 30, "guidance.direction.down");
MorditasSample Phase(MorditasPhase phase, bool success = false, int encounter = Encounter) => new(encounter, phase, success, 24, 0, 24);

var state = new MorditasEventState();
MorditasUpdate Observe(MorditasSample sample, double now) => state.Observe(sample, rules, game, text, now);
string Heard(MorditasSample sample, double now)
{
    var update = Observe(sample, now);
    if (update.HasContent) state.Acknowledge();
    return update.Text ?? "";
}

// The game's thresholds: the first opens the gauntlet, each one is a charge.
Equal(20, rules.Requirement, "first threshold is the requirement");
Equal("0,1,2,5", $"{rules.Charges(19)},{rules.Charges(20)},{rules.Charges(55)},{rules.Charges(500)}", "charges counted");
Equal("50,0", $"{rules.Next(20)},{rules.Next(200)}", "next threshold");

// Start: silent before it, then the game's start text with the requirement it never shows and the time.
Equal("", Heard(Phase(MorditasPhase.NotStarted), 0), "crystal not yet used is silent");
var start = Observe(Hunt(0, 25), 0.1);
Equal(Say("Event Started", "Defeat many.", "goal 20", "25s"), start.Text, "hunt start");
Equal(false, start.Interrupt, "start queues");
Equal(start.Text, Heard(Hunt(0, 24.9f), 0.2), "blocked speech is said later, not lost");
Equal("", Heard(Hunt(0, 24.8f), 0.3), "start said once");

// Kills: an area attack's burst settles into one count with its latest value, paced.
Equal("", Heard(Hunt(1, 24), 1.0), "first kill waits for the count to rest");
Equal("", Heard(Hunt(3, 23.7f), 1.3), "burst still landing");
Equal("", Heard(Hunt(3, 23.4f), 1.8), "not rested yet");
Equal("", Heard(Hunt(3, 23.2f), 1.95), "rested, but too soon after the start");
Equal(Say("3/20"), Heard(Hunt(3, 22.9f), 2.3), "latest count toward the requirement");
Equal("", Heard(Hunt(5, 22.7f), 2.5), "next kill");
Equal("", Heard(Hunt(5, 22), 3.2), "rested within the interval");
Equal(Say("5/20"), Heard(Hunt(5, 21), 4.3), "paced count");
var stream = new List<string>();
for (int index = 0; index < 10; index++)
    if (Heard(Hunt(6 + index, 20), 4.4 + 0.3 * index) is { Length: > 0 } heard) stream.Add(heard);
Equal(Say("15/20"), string.Join("|", stream), "a steady stream still speaks its latest count after the longest wait");

// The requirement and each charge are said at once; then counts toward the next charge, less often.
var met = Observe(Hunt(20, 20), 7.5);
Equal(Say("enough 20", "Charge Gained", "next 50"), met.Text, "requirement reached with its charge");
state.Acknowledge();
Equal("", Heard(Hunt(21, 19), 8.0), "kill after the requirement");
Equal("", Heard(Hunt(21, 19), 8.7), "charge counts are paced longer");
Equal(Say("21/50"), Heard(Hunt(21, 18), 13.5), "count toward the next charge");
Equal(Say("Charge Gained", "next 90"), Heard(Hunt(50, 30), 13.6), "second charge");
Equal(Say("Charge Gained"), Heard(Hunt(200, 30), 13.7), "last charge has no next");

// Timer warnings at the game's thresholds, re-armed only when kills push time well back up.
state = new MorditasEventState();
Heard(Hunt(0, 25), 0);
Equal("", Heard(Hunt(0, 16), 1), "above the warning");
Equal(Say("15s"), Heard(Hunt(0, 14.9f), 2), "warning crossed");
Equal("", Heard(Hunt(0, 14), 3), "warned once");
Equal("", Heard(Hunt(0, 16.5f), 4), "a kill's few seconds do not re-arm");
Equal("", Heard(Hunt(0, 14.5f), 5), "still warned");
Equal("", Heard(Hunt(0, 17.5f), 6), "re-armed silently");
Equal(Say("15s"), Heard(Hunt(0, 14.8f), 7), "warned again after re-arming");
var critical = Observe(Hunt(0, 4.9f), 8);
Equal(Say("5s"), critical.Text, "critical warning");
Equal(true, critical.Interrupt, "critical cuts in before the requirement");
state = new MorditasEventState();
Heard(Hunt(0, 25), 0);
Equal(Say("5s"), Heard(Hunt(0, 4.2f), 1), "a jump past both warnings says only the critical one");
Equal("", Heard(Hunt(0, 14.9f), 2), "the passed warning stays said");
state = new MorditasEventState();
Heard(Hunt(20, 25), 0);
var calm = Observe(Hunt(20, 4.5f), 1);
Equal(Say("5s"), calm.Text, "critical after the requirement");
Equal(false, calm.Interrupt, "running out of time is no threat once the requirement is met");

// Gauntlet: the instruction, the circle in steps and the enemies left; leaving, the countdown and returning.
state = new MorditasEventState();
Heard(Hunt(24, 1), 0);
Equal(Say("Ritual", "Revive them."), Heard(Phase(MorditasPhase.PrepareGauntlet), 20), "ritual starts");
Equal(Say("Defeat bloodcrafted.", "within 22 steps", "24 left"), Heard(Gauntlet(24, 0), 23), "gauntlet starts");
Equal("", Heard(Gauntlet(24, 2), 24), "kills land");
Equal("", Heard(Gauntlet(24, 2), 24.7), "rested within the interval");
Equal(Say("22 left"), Heard(Gauntlet(24, 2), 26), "enemies left");
var left = Observe(Gauntlet(24, 2, inside: false), 26.5);
Equal(Say("outside", "centre 30 steps down"), left.Text, "left the circle, with the way back");
Equal(true, left.Interrupt, "leaving cuts in");
state.Acknowledge();
var countdown = Observe(Gauntlet(24, 2, inside: false, allOut: true), 26.6);
Equal(Say("back in 4"), countdown.Text, "everyone out: the abandonment time");
Equal(true, countdown.Interrupt, "countdown cuts in");
state.Acknowledge();
Equal(Say("inside"), Heard(Gauntlet(24, 2, inside: true), 27), "back inside");
Equal(Say("outside", "centre 30 steps down", "back in 4"), Heard(Gauntlet(24, 2, inside: true, allOut: true), 28),
    "the all-out report before the player's own still warns");
Equal(Say("inside"), Heard(Gauntlet(24, 2, inside: true), 28.5), "back inside after it");
Equal("", Heard(Gauntlet(24, 24, inside: true), 40), "no count for zero enemies");
Equal(Say("Interact."), Heard(Phase(MorditasPhase.ReadyForReward), 41), "reward");
Equal(Say("Rage done"), Heard(Phase(MorditasPhase.Finished, success: true), 50), "success");

// Failure, baselines and new encounters.
state = new MorditasEventState();
Heard(Hunt(3, 2, encounter: 8), 0);
var failed = Observe(Phase(MorditasPhase.Finished, encounter: 8), 1);
Equal(Say("Event Failed", "Not impressed."), failed.Text, "failure in the game's words");
Equal(true, failed.Interrupt, "failure cuts in");
state = new MorditasEventState();
Equal("", Heard(Phase(MorditasPhase.Finished), 0), "an already finished encounter is a silent baseline");
Equal(Say("Event Started", "Defeat many.", "goal 20", "25s"), Heard(Hunt(0, 25, encounter: 9), 1), "a new encounter starts over");
state = new MorditasEventState();
Equal(Say("Defeat bloodcrafted.", "within 22 steps", "20 left"), Heard(Gauntlet(30, 10), 0), "joined mid-gauntlet");
state = new MorditasEventState();
Equal(Say("Event Started", "Defeat many.", "goal 20", "12s", "30/50"), Heard(Hunt(30, 12), 0), "joined mid-hunt");
Equal("", Heard(Hunt(30, 12), 0.1), "joined state is the baseline");
var empty = new MorditasRules(Array.Empty<int>(), 15, 5, 0, 0);
state = new MorditasEventState();
Equal(Say("Event Started", "Defeat many.", "25s"), state.Observe(Hunt(0, 25), empty, game, text, 0).Text,
    "no requirement without the game's data");
Equal(Say("Event Started", "Defeat many.", "25s"), state.Observe(Hunt(0, 25), empty, game, text, 0).Text, "still unsaid");

// The on-demand status.
Equal(Say("Rage", "12/20", "9s"), MorditasEventState.Status(Hunt(12, 8.2f), rules, game, text), "hunt status");
Equal(Say("Rage", "35/50", "10s"), MorditasEventState.Status(Hunt(35, 10), rules, game, text), "hunt status after the requirement");
Equal(Say("Rage", "20 left", "outside", "centre 30 steps down"),
    MorditasEventState.Status(Gauntlet(24, 4, inside: false), rules, game, text), "gauntlet status outside");
Equal(Say("Rage", "1 left"), MorditasEventState.Status(Gauntlet(24, 23), rules, game, text), "one enemy");
Equal(Say("Rage", "Interact."), MorditasEventState.Status(Phase(MorditasPhase.ReadyForReward), rules, game, text), "reward status");
Equal(null, MorditasEventState.Status(Phase(MorditasPhase.NotStarted), rules, game, text), "no status before the start");

// Blood trails: nearest first, scanner entries and the locked trail following the latest refresh.
var trails = new BloodTrails();
var player = Vector3.Zero;
var forward = new Vector3(0, 0, 1);
var right = new Vector3(1, 0, 0);
trails.Refresh(Encounter, new[] { new Vector3(0, 0, 40), new Vector3(float.NaN, 0, 0), new Vector3(15, 2, 0) }, player);
Equal(2, trails.Targets.Count, "unreadable trail dropped");
Equal(new Vector3(15, 2, 0), trails.Targets[0], "nearest first");
Equal(1, trails.Refreshes, "refresh counted");
var entries = new List<ExplorationEntry>();
Equal(2, trails.AddEntries(entries, number => $"trail {number}", 10), "one entry per trail");
Equal("bloodtrail:1|trail 1|BloodTrail|10", $"{entries[0].Key}|{entries[0].Name}|{entries[0].Category}|{entries[0].ArrivalRadius}",
    "entry key, name, category and arrival");
Equal("bloodtrail:2", entries[1].Key, "numbered by distance");
Equal(true, BloodTrails.TryFollow(entries, "bloodtrail:2", out var same) && same.Position == new Vector3(0, 0, 40),
    "a locked trail keeps its number while drawn");
Equal(true, BloodTrails.TryFollow(entries, "bloodtrail:3", out var nearest) && nearest.Key == "bloodtrail:3" &&
    nearest.Position == entries[0].Position, "a vanished trail follows the nearest under the locked key");
Equal(false, BloodTrails.TryFollow(entries, "enemy:1", out _), "other targets are not trails");
Equal(false, BloodTrails.TryFollow(new List<ExplorationEntry>(), "bloodtrail:1", out _), "no trails, nothing to follow");
Equal(true, trails.TryNearest(player, forward, right, out float pan, out float ahead, out float distance), "nearest found");
Equal("1,0,15", $"{pan:0},{ahead:0},{distance:0}", "nearest to the right");
trails.TryNearest(new Vector3(0, 0, 50), forward, right, out pan, out ahead, out distance);
Equal("0,-1,10", $"{pan:0},{ahead:0},{distance:0}", "nearest behind once the player has moved");
Equal(1f, BloodTrails.Gain(5, 10, 300), "full level near");
Equal(true, Math.Abs(BloodTrails.FarGain - BloodTrails.Gain(400, 10, 300)) < 1e-5f, "far level");
Equal(true, BloodTrails.Gain(155, 10, 300) is > 0.35f and < 1f, "level falls between");
Equal(1f, BloodTrails.Gain(155, 0, 0), "full level without the game's distances");
trails.Refresh(Encounter, Array.Empty<Vector3>(), player);
Equal(2, trails.Refreshes, "an empty refresh is still a refresh");
trails.Clear();
Equal(0, trails.AddEntries(entries = new List<ExplorationEntry>(), number => "", 0), "no entries after the hunt");
Equal(false, trails.TryNearest(player, forward, right, out _, out _, out _), "no drip after the hunt");

// One speaker: the generic readers leave the encounter's pop-up sentences to this reader while it follows one.
Equal(false, MorditasSpeechOwner.OwnsText("Event Started. Defeat as many monsters as you can."), "nothing owned before an encounter");
MorditasSpeechOwner.Begin(new[] { "Defeat as many monsters as you can.", null, "  " });
Equal(true, MorditasSpeechOwner.Active, "claimed");
Equal(true, MorditasSpeechOwner.OwnsText("Event Started. Defeat as many monsters as you can."), "owned pop-up");
Equal(false, MorditasSpeechOwner.OwnsText("Bloodrage Buff Gained. Gained Frenzy!"), "other pop-ups stay with the generic reader");
Equal(false, MorditasSpeechOwner.OwnsText(""), "blank text");
MorditasSpeechOwner.Status = "Rage";
MorditasSpeechOwner.End();
Equal(false, MorditasSpeechOwner.Active || MorditasSpeechOwner.OwnsText("Defeat as many monsters as you can."), "released");
Equal(null, MorditasSpeechOwner.Status, "status cleared");

// The drip: one short stereo cue, placed like the other world cues.
var drip = BloodTrailSynthesizer.Synthesize(0, 1, 1);
Equal((int)(BloodTrailSynthesizer.DurationSeconds * BloodTrailSynthesizer.SampleRate) * 2, drip.Length, "drip length");
Equal(0f, BloodTrailSynthesizer.Synthesize(0, 1, 0).Max(Math.Abs), "silent at zero volume");
var rightDrip = BloodTrailSynthesizer.Synthesize(1, 0, 1);
float Channel(float[] samples, int channel) => samples.Where((_, index) => index % 2 == channel).Max(Math.Abs);
Equal(true, Channel(rightDrip, 0) < 0.001f && Channel(rightDrip, 1) > 0.05f, "a drip to the right sounds right");
int Crossings(float[] samples) => Enumerable.Range(1, samples.Length / 2 - 1).Count(frame => samples[frame * 2] * samples[frame * 2 - 2] < 0);
Equal(true, Crossings(BloodTrailSynthesizer.Synthesize(0, -1, 1)) < Crossings(drip), "a drip behind is lower");

Console.WriteLine($"Morditas tests passed: {checks} checks");
