using System.Numerics;
using LeAccess.Accessibility.Exploration;

int checks = 0;
void Equal<T>(T expected, T actual, string name)
{
    checks++;
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{name}: expected {expected}, got {actual}");
}

// The test's own templates: expected speech is composed from them, never from the shipped catalogs.
var words = new Dictionary<string, string>
{
    ["exploration.scanner.empty"] = "<nothing>", ["exploration.scanner.category_empty"] = "<no {0}>",
    ["exploration.boss"] = "{0} <boss>", ["exploration.entry_here"] = "{0} <here>",
    ["exploration.steps_one"] = "<1 step>", ["exploration.steps_many"] = "<{0} steps>", ["exploration.entry"] = "{0}; {1}; {2}",
    ["guidance.direction.up"] = "<up>", ["guidance.direction.up_right"] = "<up-right>",
    ["guidance.direction.right"] = "<right>", ["guidance.direction.down_right"] = "<down-right>",
    ["guidance.direction.down"] = "<down>", ["guidance.direction.down_left"] = "<down-left>",
    ["guidance.direction.left"] = "<left>", ["guidance.direction.up_left"] = "<up-left>",
};
foreach (var category in Enum.GetValues<ExplorationCategory>())
    words[ExplorationCatalog.CategoryKey(category)] = $"<{category}>";
ExplorationText text = (key, args) => string.Format(words[key], args);
string Steps(int count) => count == 1 ? text("exploration.steps_one") : text("exploration.steps_many", count);
string Line(string name, int steps, string direction) =>
    text("exploration.entry", name, Steps(steps), text($"guidance.direction.{direction}"));
string Category(ExplorationCategory category) => text(ExplorationCatalog.CategoryKey(category));

var player = new Vector3(0, 0, 0);
var forward = new Vector3(0, 0, 1);
var right = new Vector3(1, 0, 0);
const float step = 1f;

ExplorationEntry Entry(string key, string name, ExplorationCategory category, float x, float z, bool boss = false) =>
    new(key, name, category, new Vector3(x, 0, z), 1.5f, boss);

// The D2R order (map_survey.cpp CategoryOrder), every category in it once, each with its own name key.
Equal(string.Join(",", new[]
    {
        ExplorationCategory.Exit, ExplorationCategory.Portal, ExplorationCategory.Waypoint, ExplorationCategory.UnexploredRoom,
        ExplorationCategory.Room, ExplorationCategory.Quest, ExplorationCategory.Npc, ExplorationCategory.Player,
        ExplorationCategory.LastDeath, ExplorationCategory.Stash, ExplorationCategory.Enemy, ExplorationCategory.Treasure,
        ExplorationCategory.Shrine, ExplorationCategory.Interactable, ExplorationCategory.Item
    }), string.Join(",", ExplorationCatalog.Order), "D2R category order");
Equal(Enum.GetValues<ExplorationCategory>().Length, ExplorationCatalog.Order.Distinct().Count(), "every category is in the order once");
Equal(Enum.GetValues<ExplorationCategory>().Length,
    Enum.GetValues<ExplorationCategory>().Select(ExplorationCatalog.CategoryKey).Distinct().Count(), "every category has its own name");

// Empty catalogue.
var catalog = new ExplorationCatalog();
Equal(text("exploration.scanner.empty"), catalog.StepCategory(1, new List<ExplorationEntry>(), player, forward, right, step, text), "empty");
Equal(null, catalog.Category, "empty category");

var entries = new List<ExplorationEntry>
{
    Entry("exit-far", "Exit far", ExplorationCategory.Exit, 0, 20),
    Entry("exit-near", "Exit near", ExplorationCategory.Exit, -5, 5),
    Entry("npc", "Maeve", ExplorationCategory.Npc, 3, 0),
    Entry("boss", "Rahyeh", ExplorationCategory.Enemy, 0, -1, boss: true),
};

// Down (next) from nothing starts at the first non-empty category, nearest entry first; the category name
// comes first and no count is spoken (D2R).
Equal(Category(ExplorationCategory.Exit) + ". " + Line("Exit near", 7, "up_left"),
    catalog.StepCategory(1, entries, player, forward, right, step, text), "first category");
Equal("exit-near", catalog.SelectedKey, "nearest selected");
Equal(Line("Exit far", 20, "up"), catalog.StepEntry(1, entries, player, forward, right, step, text), "next entry");
Equal(Line("Exit near", 7, "up_left"), catalog.StepEntry(1, entries, player, forward, right, step, text), "entry wraps");
Equal(Line("Exit far", 20, "up"), catalog.StepEntry(-1, entries, player, forward, right, step, text), "previous wraps");

// Categories skip empty ones and wrap both ways.
Equal(Category(ExplorationCategory.Npc) + ". " + Line("Maeve", 3, "right"),
    catalog.StepCategory(1, entries, player, forward, right, step, text), "skip empty");
Equal(Category(ExplorationCategory.Enemy) + ". " + Line(text("exploration.boss", "Rahyeh"), 1, "down"),
    catalog.StepCategory(1, entries, player, forward, right, step, text), "boss and one step");
catalog.StepCategory(1, entries, player, forward, right, step, text);
Equal(ExplorationCategory.Exit, catalog.Category, "wrap forward");
catalog.StepCategory(-1, entries, player, forward, right, step, text);
Equal(ExplorationCategory.Enemy, catalog.Category, "wrap back");

// Up (previous) from nothing starts at the last category.
var fresh = new ExplorationCatalog();
fresh.StepCategory(-1, entries, player, forward, right, step, text);
Equal(ExplorationCategory.Enemy, fresh.Category, "previous from nothing");

// An entry step with no category behaves like the first category step.
var entryFirst = new ExplorationCatalog();
Equal(Category(ExplorationCategory.Exit) + ". " + Line("Exit near", 7, "up_left"),
    entryFirst.StepEntry(1, entries, player, forward, right, step, text), "entry without category");

// Selected entry follows its key after re-sorting; a vanished category says it has none here.
Equal("exit-near", entryFirst.Selected(entries)?.Key, "selected lookup");
var moved = new Vector3(0, 0, 18);
Equal(Line("Exit far", 2, "up"), entryFirst.StepEntry(1, entries, moved, forward, right, step, text),
    "resorted step moves from selection");
var withoutExits = entries.Where(e => e.Category != ExplorationCategory.Exit).ToList();
Equal(text("exploration.scanner.category_empty", Category(ExplorationCategory.Exit)),
    entryFirst.StepEntry(1, withoutExits, player, forward, right, step, text), "category emptied");
Equal(null, entryFirst.Selected(withoutExits)?.Key, "no selection after emptying");
// From an emptied category, next continues in the fixed order (NPCs after exits), previous wraps back.
entryFirst.StepCategory(1, withoutExits, player, forward, right, step, text);
Equal(ExplorationCategory.Npc, entryFirst.Category, "continue after emptied");
var emptiedBack = new ExplorationCatalog();
emptiedBack.StepCategory(1, entries, player, forward, right, step, text);
emptiedBack.StepCategory(-1, withoutExits, player, forward, right, step, text);
Equal(ExplorationCategory.Enemy, emptiedBack.Category, "previous from emptied first wraps");

// Within one step says "here"; step length scales distance; height does not count.
Equal(text("exploration.entry_here", "Maeve"),
    ExplorationCatalog.Describe(Entry("n", "Maeve", ExplorationCategory.Npc, 0.3f, 0), player, forward, right, step, text), "here");
Equal(Line("Maeve", 2, "right"),
    ExplorationCatalog.Describe(Entry("n", "Maeve", ExplorationCategory.Npc, 4, 0), player, forward, right, 2f, text), "step length");
Equal(Line("Maeve", 3, "right"), ExplorationCatalog.Describe(new ExplorationEntry("n", "Maeve", ExplorationCategory.Npc,
    new Vector3(3, 9, 0), 1), player, forward, right, step, text), "horizontal distance");

// Non-finite entries are ignored.
var broken = new List<ExplorationEntry> { new("x", "X", ExplorationCategory.Exit, new Vector3(float.NaN, 0, 0), 1) };
Equal(text("exploration.scanner.empty"), new ExplorationCatalog().StepCategory(1, broken, player, forward, right, step, text),
    "non-finite ignored");

// The whole D2R walk: ways out, rooms, the quest, who is there, then the scenery.
var level = new List<ExplorationEntry>
{
    Entry("portal", "Portal", ExplorationCategory.Portal, 0, 9),
    Entry("waypoint", "Waypoint", ExplorationCategory.Waypoint, 0, -9),
    Entry("unexplored:2", "Unexplored 3", ExplorationCategory.UnexploredRoom, 6, 6),
    Entry("room:0", "Room 1", ExplorationCategory.Room, 0, 0),
    Entry("objective:1", "Quest", ExplorationCategory.Quest, 40, 0),
    Entry("player:7", "Friend", ExplorationCategory.Player, 2, 2),
    Entry("death", "Death", ExplorationCategory.LastDeath, -3, 0),
    Entry("listener:1", "Stash", ExplorationCategory.Stash, 1, 1),
    Entry("listener:2", "Chest", ExplorationCategory.Treasure, 5, 0),
    Entry("listener:3", "Shrine", ExplorationCategory.Shrine, 0, 5),
    Entry("listener:4", "Note", ExplorationCategory.Interactable, -5, 0),
    Entry("item:9", "Sword", ExplorationCategory.Item, 1, 0),
};
var walk = new ExplorationCatalog();
var visitedOrder = new List<ExplorationCategory>();
for (int index = 0; index < level.Count; index++)
{
    walk.StepCategory(1, level, player, forward, right, step, text);
    visitedOrder.Add(walk.Category.Value);
}
Equal(string.Join(",", ExplorationCatalog.Order.Where(category => level.Any(entry => entry.Category == category))),
    string.Join(",", visitedOrder), "categories walk in D2R order, skipping empty ones");

// Seen memory: places keyed by name and coarse position, NPCs by name; scenes are separate.
var memory = new ExplorationSeenMemory();
memory.Enter(ExplorationSeenMemory.Scope("Z1", 7, fixedLayout: true));
Equal(true, memory.Add(ExplorationSeenMemory.Key(ExplorationCategory.Exit, "Exit", new Vector3(10.4f, 0, 3))), "add place");
Equal(true, memory.Contains(ExplorationSeenMemory.Key(ExplorationCategory.Exit, "Exit", new Vector3(10.6f, 5, 3.2f))), "same coarse cell");
Equal(false, memory.Contains(ExplorationSeenMemory.Key(ExplorationCategory.Exit, "Exit", new Vector3(30, 0, 3))), "other place");
memory.Add(ExplorationSeenMemory.Key(ExplorationCategory.Npc, "Maeve", new Vector3(0, 0, 0)));
Equal(true, memory.Contains(ExplorationSeenMemory.Key(ExplorationCategory.Npc, "Maeve", new Vector3(50, 0, 50))), "NPC by name");
memory.Enter(ExplorationSeenMemory.Scope("Z2", 8, fixedLayout: true));
Equal(0, memory.Count, "new scene empty");
memory.Enter(ExplorationSeenMemory.Scope("Z1", 99, fixedLayout: true));
Equal(2, memory.Count, "fixed scene kept across sessions");
Equal("PCG#5", ExplorationSeenMemory.Scope("PCG", 5, fixedLayout: false), "procedural scope uses session");
// Keys saved before the D2R categories still match: exits 1, waypoints 2, NPCs 3, the stash 4.
Equal("1|Exit|0|0", ExplorationSeenMemory.Key(ExplorationCategory.Exit, "Exit", Vector3.Zero), "saved exit keys unchanged");
Equal("2|Waypoint|0|0", ExplorationSeenMemory.Key(ExplorationCategory.Waypoint, "Waypoint", Vector3.Zero), "saved waypoint keys unchanged");
Equal("3|Maeve", ExplorationSeenMemory.Key(ExplorationCategory.Npc, "Maeve", Vector3.Zero), "saved NPC keys unchanged");
Equal("4|Stash|0|0", ExplorationSeenMemory.Key(ExplorationCategory.Stash, "Stash", Vector3.Zero), "saved stash keys unchanged");

// Seen memory export/import: only fixed scopes, a version bump on change.
var saved = new ExplorationSeenMemory();
saved.Enter("Town");
int version = saved.Version;
saved.Add("1|Exit|5|1");
saved.Add("1|Exit|5|1");
Equal(version + 1, saved.Version, "version bumps once per new key");
saved.Enter("PCG#3");
saved.Add("2|Waypoint|0|0");
Equal(1, saved.FixedScopes().Count(), "procedural scopes are not exported");
var restored = new ExplorationSeenMemory();
Equal(1, restored.ImportFixed("Town", saved.FixedScopes().First().Value), "import adds keys");
Equal(0, restored.ImportFixed("Town", new[] { "1|Exit|5|1", "" }), "import dedupes and skips empty keys");
Equal(0, restored.ImportFixed("PCG#3", new[] { "x" }), "procedural scope import refused");
restored.Enter("Town");
Equal(true, restored.Contains("1|Exit|5|1"), "imported key is remembered");
Equal(1, restored.Count, "entering an imported scope reuses it");

Console.WriteLine($"Exploration catalog tests passed: {checks} checks");
