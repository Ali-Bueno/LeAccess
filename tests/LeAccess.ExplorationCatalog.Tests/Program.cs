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
    ["exploration.on_level"] = "{0} [{1}]", ["exploration.level_higher"] = "<higher>", ["exploration.level_lower"] = "<lower>",
    ["exploration.steps_one"] = "<1 step>", ["exploration.steps_many"] = "<{0} steps>", ["exploration.entry"] = "{0}; {1}; {2}",
    ["exploration.route_steps_one"] = "<1 step by route>", ["exploration.route_steps_many"] = "<{0} steps by route>",
    ["exploration.route_heading"] = "<head {0}>", ["exploration.entry_unrouted"] = "{0} <no known route>",
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
        ExplorationCategory.Shrine, ExplorationCategory.Interactable, ExplorationCategory.Breakable, ExplorationCategory.Item
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

// End of Time: Elder Gaspar stood 15.7 above the player on the central platform, 9 horizontally (reference/exploration-mode.md).
// Levels compare NavMesh-sampled ground, never raw heights: an objective icon floats above its floor.
const float floorHeight = 2f;
float[] floors = { -15.69f, 0f, 15.69f };
Func<Vector3, float?> floorGround = point => floors.OrderBy(floor => Math.Abs(floor - point.Y)).First();
Func<Vector3, float?> exactGround = point => point.Y;
var gaspar = new ExplorationEntry("g", "Gaspar", ExplorationCategory.Npc, new Vector3(2.18f, 15.69f, 9.12f), 1);
Equal(text("exploration.on_level", Line("Gaspar", 9, "up"), text("exploration.level_higher")),
    ExplorationCatalog.Describe(gaspar, player, forward, right, step, text, floorHeight, floorGround), "a target on a platform above is on a higher level");
Equal(text("exploration.on_level", text("exploration.entry_here", "Knight"), text("exploration.level_lower")),
    ExplorationCatalog.Describe(new ExplorationEntry("k", "Knight", ExplorationCategory.Npc, new Vector3(0, -15.69f, 0.2f), 1),
        player, forward, right, step, text, floorHeight, floorGround), "a target right below says here, on a lower level");
Equal(Line("Maeve", 3, "right"), ExplorationCatalog.Describe(new ExplorationEntry("n", "Maeve", ExplorationCategory.Npc,
    new Vector3(3, 1.9f, 0), 1), player, forward, right, step, text, floorHeight, exactGround), "a step or ramp below one floor is the same level");
// The minimap icon of a quest marker floats 5 above the same floor: no level phrase.
Equal(Line("Objective", 3, "right"), ExplorationCatalog.Describe(new ExplorationEntry("o", "Objective", ExplorationCategory.Quest,
    new Vector3(3, 5, 0), 1), player, forward, right, step, text, floorHeight, floorGround), "a marker floating above the same floor claims no level");
// Without a ground sample for either end nothing is claimed.
Equal(Line("Gaspar", 9, "up"), ExplorationCatalog.Describe(gaspar, player, forward, right, step, text, floorHeight),
    "no ground sampler, no level claim");
Equal(Line("Gaspar", 9, "up"), ExplorationCatalog.Describe(gaspar, player, forward, right, step, text, floorHeight, point => point.Y > 1 ? null : 0f),
    "an unsampled target claims no level");
Equal(Line("Gaspar", 9, "up"), ExplorationCatalog.Describe(gaspar, new Vector3(0, 0, 0), forward, right, step, text, floorHeight, point => point.Y > 1 ? 15.69f : null),
    "an unsampled player claims no level");
var levelled = new ExplorationCatalog { LevelHeight = floorHeight, Ground = floorGround };
Equal(Category(ExplorationCategory.Npc) + ". " + text("exploration.on_level", Line("Gaspar", 9, "up"), text("exploration.level_higher")),
    levelled.StepCategory(1, new List<ExplorationEntry> { gaspar }, player, forward, right, step, text), "the scanner passes its floor height and ground");
var unsampled = new ExplorationCatalog { LevelHeight = floorHeight };
Equal(Category(ExplorationCategory.Npc) + ". " + Line("Gaspar", 9, "up"),
    unsampled.StepCategory(1, new List<ExplorationEntry> { gaspar }, player, forward, right, step, text), "the scanner without ground says no level");

// Routes (2026-10-11 tester report): a detour speaks its walking length and the way to head now (the beacon's
// look-ahead); no known route marks the straight line; a route that reads like the straight line keeps it.
string RouteLine(string name, int steps, string heading) => text("exploration.entry", name,
    steps == 1 ? text("exploration.route_steps_one") : text("exploration.route_steps_many", steps),
    text("exploration.route_heading", text($"guidance.direction.{heading}")));
var exitRight = Entry("exit", "Exit", ExplorationCategory.Exit, 20, 0);
string plainRight = Line("Exit", 20, "right");
Func<ExplorationEntry, Vector3, ExplorationRoute?> detour = (_, from) => new ExplorationRoute(46, from + new Vector3(0, 0, -4), false);
Equal(RouteLine("Exit", 46, "down"), ExplorationCatalog.Describe(exitRight, player, forward, right, step, text, route: detour),
    "a detour speaks the route and the way to head");
Equal(text("exploration.entry_unrouted", plainRight),
    ExplorationCatalog.Describe(exitRight, player, forward, right, step, text, route: (_, _) => null), "no known route marks the straight line");
Equal(plainRight, ExplorationCatalog.Describe(exitRight, player, forward, right, step, text,
    route: (_, from) => new ExplorationRoute(20.4f, from + new Vector3(4, 0, 0), true)), "a direct route keeps the plain readout");
Equal(plainRight, ExplorationCatalog.Describe(exitRight, player, forward, right, step, text,
    route: (_, from) => new ExplorationRoute(20.3f, from + new Vector3(4, 0, 0.5f), false)), "a route reading like the line keeps it");
Equal(plainRight, ExplorationCatalog.Describe(exitRight, player, forward, right, step, text), "without routes the readout is unchanged");
Equal(RouteLine("Exit", 1, "up"), ExplorationCatalog.Describe(Entry("exit", "Exit", ExplorationCategory.Exit, 0.3f, 0), player, forward, right,
    step, text, route: (_, from) => new ExplorationRoute(1.2f, from + new Vector3(0, 0, 1.2f), false)), "here, behind a rail, says its route");
// The level phrase follows the routed or marked readout.
Equal(text("exploration.on_level", RouteLine("Gaspar", 60, "right"), text("exploration.level_higher")),
    ExplorationCatalog.Describe(gaspar, player, forward, right, step, text, floorHeight, floorGround,
        (_, from) => new ExplorationRoute(60, from + new Vector3(4, 0, 0), false)), "a route up to another level keeps the level phrase");
Equal(text("exploration.on_level", text("exploration.entry_unrouted", Line("Gaspar", 9, "up")), text("exploration.level_higher")),
    ExplorationCatalog.Describe(gaspar, player, forward, right, step, text, floorHeight, floorGround, (_, _) => null),
    "no route to another level keeps the level phrase");
// The scanner routes only the entry it speaks, through one function.
int routedCount = 0;
var routing = new ExplorationCatalog
{
    Route = (entry, from) =>
    {
        routedCount++;
        return entry.Key == "exit-far" ? new ExplorationRoute(31, from + new Vector3(-4, 0, 0), false) : null;
    }
};
Equal(Category(ExplorationCategory.Exit) + ". " + text("exploration.entry_unrouted", Line("Exit near", 7, "up_left")),
    routing.StepCategory(1, entries, player, forward, right, step, text), "a category step routes its entry");
Equal(1, routedCount, "only the spoken entry is routed");
Equal(RouteLine("Exit far", 31, "left"), routing.StepEntry(1, entries, player, forward, right, step, text), "an entry step routes its entry");
Equal(RouteLine("Exit far", 31, "left"), routing.DescribeEntry(routing.Selected(entries).Value, player, forward, right, step, text),
    "the selected entry is described by the same function");
Equal(3, routedCount, "one route per readout");

// Landmarks and loot (shrines, gold, items) are listed when on screen; NPCs, chests and objects keep the seen-range rule.
foreach (var landmark in new[] { ExplorationCategory.Shrine, ExplorationCategory.Portal, ExplorationCategory.Exit,
    ExplorationCategory.Stash, ExplorationCategory.Item, ExplorationCategory.Breakable })
    Equal(true, ExplorationSight.ListsOnScreen(landmark), $"{landmark} is listed on screen");
foreach (var other in new[] { ExplorationCategory.Npc, ExplorationCategory.Treasure, ExplorationCategory.Interactable,
    ExplorationCategory.Waypoint, ExplorationCategory.Enemy })
    Equal(false, ExplorationSight.ListsOnScreen(other), $"{other} keeps the seen-range rule");

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
    Entry("breakable:1", "Barrel", ExplorationCategory.Breakable, 0, -2),
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

// Breakables sit between objects and items, nearest first, and drop out of the list when broken.
var props = new List<ExplorationEntry>
{
    Entry("listener:4", "Note", ExplorationCategory.Interactable, -5, 0),
    Entry("breakable:far", "Crate", ExplorationCategory.Breakable, 0, 8),
    Entry("breakable:near", "Barrel", ExplorationCategory.Breakable, 0, 2),
    Entry("item:9", "Sword", ExplorationCategory.Item, 1, 0),
};
var breaker = new ExplorationCatalog();
breaker.StepCategory(1, props, player, forward, right, step, text);
Equal(ExplorationCategory.Interactable, breaker.Category.Value, "objects come first");
Equal(Category(ExplorationCategory.Breakable) + ". " + Line("Barrel", 2, "up"),
    breaker.StepCategory(1, props, player, forward, right, step, text), "the next category is the breakables, nearest first");
Equal(Line("Crate", 8, "up"), breaker.StepEntry(1, props, player, forward, right, step, text), "the next breakable is the farther one");
props.RemoveAll(entry => entry.Key == "breakable:far");
Equal(Category(ExplorationCategory.Item) + ". " + Line("Sword", 1, "right"),
    breaker.StepCategory(1, props, player, forward, right, step, text), "a broken breakable drops out and the walk goes on");
Equal(true, BreakableSelection.Listed(standing: true, seen: true) && !BreakableSelection.Listed(false, true) &&
    !BreakableSelection.Listed(true, false), "only a standing, seen prop is listed");
Equal("Urn", BreakableSelection.Name("  Urn ", "<generic>"), "the game's own name wins");
Equal("<generic>", BreakableSelection.Name("  ", "<generic>"), "without a game name the generic label is used");
Equal("<generic>", BreakableSelection.Name(null, "<generic>"), "a missing game name uses the generic label");

// Create+Circle repeats the selected entry with the current distance and direction.
var repeater = new ExplorationCatalog();
Equal(text("exploration.scanner.empty"), repeater.RepeatSelected(props, player, forward, right, step, text), "nothing selected says the scanner is empty");
var moving = new List<ExplorationEntry> { Entry("exit", "Gate", ExplorationCategory.Exit, 0, 6) };
Equal(Category(ExplorationCategory.Exit) + ". " + Line("Gate", 6, "up"), repeater.StepCategory(1, moving, player, forward, right, step, text), "select the gate");
Equal(Line("Gate", 6, "up"), repeater.RepeatSelected(moving, player, forward, right, step, text), "repeat is what an entry step says");
Equal(Line("Gate", 2, "up"), repeater.RepeatSelected(moving, new Vector3(0, 0, 4), forward, right, step, text), "the repeat uses the fresh position");
Equal(text("exploration.scanner.empty"), repeater.RepeatSelected(new List<ExplorationEntry>(), player, forward, right, step, text), "a vanished selection says the scanner is empty");

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
