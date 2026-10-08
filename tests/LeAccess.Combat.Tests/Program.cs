using LeAccess.Accessibility.Combat;

var state = new NearestEnemyAnnouncementState();
int checks = 0;

void Check(bool condition, string description)
{
    checks++;
    if (!condition) throw new InvalidOperationException(description);
}

Check(!state.TryAnnounce(null, 0, true, out _), "No enemy means no announcement");
Check(state.TryAnnounce("Esqueleto", 0, true, out var name) && name == "Esqueleto",
    "Nearest enemy speaks immediately without a damage event");
for (int second = 1; second <= 12; second++)
    Check(!state.TryAnnounce("Esqueleto", second, true, out _), "Unchanged nearest name does not repeat");
Check(state.TryAnnounce("Lobo", 13, true, out name) && name == "Lobo", "Changed nearest enemy is announced");
Check(!state.TryAnnounce("Oso", 14, true, out _), "Rapid distance changes respect speech interval");
Check(state.TryAnnounce("Esqueleto", 15, true, out name) && name == "Esqueleto",
    "Latest nearest name replaces a superseded enemy during cooldown");

state.Reset();
Check(!state.TryAnnounce("Lobo", 20, false, out _), "Ongoing menu speech has priority");
Check(state.TryAnnounce("Oso", 21, true, out name) && name == "Oso",
    "Only the currently nearest enemy speaks when the reader becomes idle");
Check(!state.TryAnnounce("Lobo", 22, false, out _), "Busy reader does not queue an old enemy");
Check(!state.TryAnnounce("Oso", 24, true, out _), "Return to the announced name drops the unspoken change");
Check(!state.TryAnnounce(null, 25, true, out _), "Combat end drops the last enemy");
Check(state.TryAnnounce("Oso", 26, true, out _), "Same name can be announced in a new encounter");

Check(!state.TryAnnounce(" ", 27, true, out _), "Empty localized names are ignored");
Check(!state.TryAnnounce("Lobo", 27, true, out _), "Transient missing enemy does not bypass the cooldown");
Check(state.TryAnnounce("Lobo", 28, true, out _), "A current name is eligible after cooldown");
state.Reset();
Check(state.TryAnnounce("Lobo", 28, true, out _), "Scene or player reset clears deduplication");
Check(!state.TryAnnounce("Oso", double.NaN, true, out _), "Invalid time cannot speak");
Check(!state.TryAnnounce("Oso", double.PositiveInfinity, true, out _), "Infinite time cannot speak");
Check(state.TryAnnounce("Lobo", 1, true, out _), "Clock reset cannot leave speech blocked");

state.Reset();
state.SpeechIntervalSeconds = 1;
Check(state.TryAnnounce("Lobo", 0, true, out _), "Custom interval first enemy");
Check(!state.TryAnnounce("Oso", 0.5, true, out _), "Custom interval limits rapid changes");
Check(state.TryAnnounce("Oso", 1, true, out _), "Custom interval permits the current enemy on time");
NearestEnemyProbeTests.Run(Check);
CombatServiceTests.Run(Check);
EnemyClassTests.Run(Check);
Console.WriteLine($"Combat tests passed: {checks} checks.");
