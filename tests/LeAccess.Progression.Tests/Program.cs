using LeAccess.Accessibility.Progression;
using LeAccess.I18n;

int checks = 0;
void Check(bool condition, string label) { checks++; if (!condition) throw new Exception(label); }
void Equal<T>(T expected, T actual, string label) =>
    Check(Describe(expected) == Describe(actual), $"{label}: expected {Describe(expected)}, got {Describe(actual)}");
static string Describe<T>(T value) => value is IEnumerable<string> list ? string.Join(" | ", list) : value?.ToString();

ModText.Initialize(null, null);
foreach (string language in new[] { "en", "es" })
{
    ModText.SetLanguage(language);
    Run();
}
Console.WriteLine($"{checks} checks passed");

static SkillPoint Skill(string name) => new("key-" + name, name);

static ProgressionSnapshot Snap(int level = 10, int passive = 0, long session = 1, long xp = 50, long next = 200,
    bool newSkill = false, string zone = null, int max = 100, params string[] skills) =>
    new(session, level, max, xp, next, passive, skills.Select(Skill).ToList(), newSkill, zone);

List<string> Drain(ProgressionState state)
{
    var spoken = new List<string>();
    while (state.HasPending) { spoken.Add(state.Next); state.Acknowledge(); }
    return spoken;
}

void Run()
{
    var state = new ProgressionState();
    state.Observe(Snap(level: 12, passive: 3, skills: new[] { "A" }));
    Check(!state.HasPending, "first observation is a silent baseline");
    state.Observe(Snap(level: 12, passive: 3, skills: new[] { "A" }));
    Check(!state.HasPending, "unchanged state stays silent");

    state.Observe(Snap(level: 13, passive: 4, skills: new[] { "A" }));
    Equal(new List<string> { ModText.Format("progression.level_up", 13), ModText.Format("progression.passive_points", 4) },
        Drain(state), "level-up then passive points");
    state.Observe(Snap(level: 13, passive: 4, skills: new[] { "A" }));
    Check(!state.HasPending, "no repeat while unchanged");

    state.Observe(Snap(level: 13, passive: 4, skills: new[] { "A", "B" }));
    Equal(new List<string> { ModText.Format("progression.skill_can_level", "B") }, Drain(state), "new skill that can level");
    state.Observe(Snap(level: 13, passive: 4, skills: new[] { "B" }));
    Check(!state.HasPending, "a skill leaving the list is silent");
    state.Observe(Snap(level: 13, passive: 4, skills: new[] { "A", "B" }));
    Equal(new List<string> { ModText.Format("progression.skill_can_level", "A") }, Drain(state), "skill returning is announced again");

    state.Observe(Snap(level: 13, passive: 4, newSkill: true, skills: new[] { "A", "B" }));
    Equal(new List<string> { ModText.Get("progression.new_skill") }, Drain(state), "new unlock button");
    state.Observe(Snap(level: 13, passive: 4, newSkill: true, skills: new[] { "A", "B" }));
    Check(!state.HasPending, "unlock button held does not repeat");

    state.Observe(Snap(level: 13, passive: 5, newSkill: true, skills: new[] { "A", "B" }));
    Equal(new List<string> { ModText.Format("progression.passive_points", 5) }, Drain(state), "more points announce again");
    state.Observe(Snap(level: 13, passive: 1, newSkill: true, skills: new[] { "A", "B" }));
    Check(!state.HasPending, "spending points is silent");
    state.Observe(Snap(level: 13, passive: 2, newSkill: true, skills: new[] { "A", "B" }));
    Equal(new List<string> { ModText.Format("progression.passive_points", 2) }, Drain(state), "gaining after spending");

    // A single point uses the singular wording.
    state.Observe(Snap(level: 13, passive: 0, newSkill: true, skills: new[] { "A", "B" }));
    state.Observe(Snap(level: 13, passive: 1, newSkill: true, skills: new[] { "A", "B" }));
    Equal(new List<string> { ModText.Get("progression.passive_point") }, Drain(state), "one point");

    // Pending speech that became stale is withdrawn.
    state.Observe(Snap(level: 13, passive: 2, newSkill: true, skills: new[] { "A", "B" }));
    state.Observe(Snap(level: 13, passive: 0, newSkill: false, skills: new[] { "A" }));
    Check(!state.HasPending, "spent points and removed skill withdraw queued speech");

    // Two level-ups before speech keep only the latest level; rejected speech stays queued.
    state.Observe(Snap(level: 14, passive: 0, skills: new[] { "A" }));
    state.Observe(Snap(level: 15, passive: 0, skills: new[] { "A" }));
    Equal(ModText.Format("progression.level_up", 15), state.Next, "latest level only");
    Equal(ModText.Format("progression.level_up", 15), state.Next, "unacknowledged speech is retried");
    Drain(state);

    // A new session (character or load) and a level decrease are silent.
    state.Observe(Snap(level: 40, passive: 7, session: 2, skills: new[] { "C" }));
    Check(!state.HasPending, "new session baselines silently");
    state.Observe(Snap(level: 30, passive: 7, session: 2, skills: new[] { "C" }));
    state.Observe(Snap(level: 31, passive: 7, session: 2, skills: new[] { "C" }));
    Equal(new List<string> { ModText.Format("progression.level_up", 31) }, Drain(state), "level rises from the lowered baseline");
    state.Reset();
    state.Observe(Snap(level: 50, passive: 9));
    Check(!state.HasPending, "reset restores the silent baseline");

    // Unknown (negative) passive counts never announce.
    state.Observe(Snap(level: 50, passive: -1));
    Check(!state.HasPending, "unknown passive count is silent");

    Status();
}

void Status()
{
    var full = Snap(level: 12, passive: 2, xp: 45, next: 200, newSkill: true, zone: "Ruinas", skills: new[] { "A", "B" });
    string expected = string.Join(". ", ModText.Format("progression.level", 12),
        ModText.Format("progression.experience", 22), ModText.Format("progression.passive_points", 2),
        ModText.Format("progression.skills_can_level", "A, B"), ModText.Get("progression.new_skill"),
        ModText.Format("progression.zone", "Ruinas"));
    Equal(expected, ProgressionState.StatusText(full), "full status");

    Equal(string.Join(". ", ModText.Format("progression.level", 5), ModText.Format("progression.experience", 0)),
        ProgressionState.StatusText(Snap(level: 5, xp: 0)), "plain status omits empty parts");
    Equal(string.Join(". ", ModText.Format("progression.level", 5), ModText.Format("progression.experience", 22),
            ModText.Format("progression.skill_can_level", "Z")),
        ProgressionState.StatusText(Snap(level: 5, xp: 45, skills: new[] { "Z" })), "single skill wording");
    Equal(string.Join(". ", ModText.Format("progression.level", 100), ModText.Get("progression.max_level")),
        ProgressionState.StatusText(Snap(level: 100, xp: 5, max: 100)), "maximum level replaces the percentage");
    Equal(ModText.Format("progression.level", 7), ProgressionState.StatusText(Snap(level: 7, next: 0)), "no threshold, no percentage");

    Equal(-1, ProgressionState.Percent(10, 0), "zero threshold");
    Equal(0, ProgressionState.Percent(-5, 100), "negative experience");
    Equal(100, ProgressionState.Percent(500, 100), "overshoot clamps");
    Equal(99, ProgressionState.Percent(199, 200), "percent floors");
    Equal(50, ProgressionState.Percent(2_000_000_000_000_000_000L, 4_000_000_000_000_000_000L), "large values do not overflow");
}
