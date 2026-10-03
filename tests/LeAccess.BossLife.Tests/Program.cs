using LeAccess.Accessibility.Combat;

int checks = 0;
void Equal<T>(T expected, T actual, string name)
{
    checks++;
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{name}: expected {expected}, got {actual}");
}
string Say(BossLifeUpdate update) => update.Text((name, percent) => $"{name}, {percent} percent");
BossSample Alive(int id, string name, int percent) => new(id, name, percent, false);
BossSample Dead(int id, string name) => new(id, name, 0, true);

var state = new BossLifeState();
Equal("", Say(state.Observe(new[] { Alive(1, "Lagon", 100) })), "first sighting silent");
Equal("", Say(state.Observe(new[] { Alive(1, "Lagon", 90) })), "chip damage silent");
var fall = state.Observe(new[] { Alive(1, "Lagon", 74) });
Equal("Lagon, 74 percent", Say(fall), "fall across 75");
state.Acknowledge(fall);
Equal("", Say(state.Observe(new[] { Alive(1, "Lagon", 74) })), "acknowledged fall not repeated");
Equal("", Say(state.Observe(new[] { Alive(1, "Lagon", 80) })), "one-band rise silent");
var low = state.Observe(new[] { Alive(1, "Lagon", 45) });
state.Acknowledge(low);
var heal = state.Observe(new[] { Alive(1, "Lagon", 100) });
Equal("Lagon, 100 percent", Say(heal), "big heal spoken");
state.Acknowledge(heal);

state = new BossLifeState();
state.Observe(new[] { Alive(1, "Lagon", 100), Alive(2, "Hydra", 100) });
var both = state.Observe(new[] { Alive(1, "Lagon", 70), Alive(2, "Hydra", 45) });
Equal("Lagon, 70 percent. Hydra, 45 percent", Say(both), "two bosses joined");
state.Acknowledge(both);

var death = state.Observe(new[] { Dead(1, "Lagon"), Alive(2, "Hydra", 45) });
Equal("Lagon, 0 percent", Say(death), "death spoken as zero");
state.Acknowledge(death);
Equal("", Say(state.Observe(new[] { Dead(1, "Lagon"), Alive(2, "Hydra", 45) })), "death spoken once");
Equal("", Say(state.Observe(new[] { Alive(1, "Lagon", 0) })), "zero health after forgetting silent");

state = new BossLifeState();
state.Observe(new[] { Alive(1, "Lagon", 100) });
Equal("", Say(state.Observe(Array.Empty<BossSample>())), "unloaded silent");
Equal("", Say(state.Observe(new[] { Alive(1, "Lagon", 30) })), "returning boss re-baselined");
Equal("Lagon, 20 percent", Say(state.Observe(new[] { Alive(1, "Lagon", 20) })), "falls after rebaseline");

state = new BossLifeState();
Equal("", Say(state.Observe(new[] { Dead(5, "Ghost") })), "never-seen dead silent");
state.Observe(new[] { Alive(1, "Lagon", 100) });
state.Reset();
Equal("", Say(state.Observe(new[] { Alive(1, "Lagon", 20) })), "session change resets baseline");

state = new BossLifeState();
state.Observe(new[] { Alive(1, "Lagon", 100) });
var rejected = state.Observe(new[] { Alive(1, "Lagon", 74) });
Equal("Lagon, 74 percent", Say(rejected), "rejected first attempt");
var retry = state.Observe(new[] { Alive(1, "Lagon", 60) });
Equal("Lagon, 60 percent", Say(retry), "retry uses fresh values");
state.Acknowledge(retry);
Equal("", Say(state.Observe(new[] { Alive(1, "Lagon", 55) })), "retry committed");
Equal("", Say(state.Observe(new[] { Alive(1, "Lagon", 55) })) == "" ? "" : "x", "alive after unaccepted death");
Equal("Lagon, 0 percent", Say(state.Observe(new[] { Dead(1, "Lagon") })), "death retried after rejection");

Console.WriteLine($"BossLife tests passed: {checks} checks");
