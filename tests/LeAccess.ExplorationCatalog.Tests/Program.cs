using System.Numerics;
using LeAccess.Accessibility.Exploration;

int checks = 0;
void Equal<T>(T expected, T actual, string name)
{
    checks++;
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{name}: expected {expected}, got {actual}");
}

var words = new Dictionary<string, string>
{
    ["exploration.scanner.empty"] = "Nada descubierto cerca", ["exploration.scanner.category"] = "{0}, {1}",
    ["exploration.scanner.category_empty"] = "{0}: ninguno", ["exploration.boss"] = "{0}, jefe",
    ["exploration.entry_here"] = "{0}, aquí", ["exploration.steps_one"] = "1 paso", ["exploration.steps_many"] = "{0} pasos",
    ["exploration.entry"] = "{0}, {1}, {2}",
    ["exploration.category.objectives"] = "Objetivos", ["exploration.category.exits"] = "Salidas",
    ["exploration.category.travel"] = "Viaje", ["exploration.category.characters"] = "Personajes",
    ["exploration.category.services"] = "Servicios", ["exploration.category.enemies"] = "Enemigos",
    ["guidance.direction.up"] = "arriba", ["guidance.direction.up_right"] = "arriba a la derecha",
    ["guidance.direction.right"] = "derecha", ["guidance.direction.down_right"] = "abajo a la derecha",
    ["guidance.direction.down"] = "abajo", ["guidance.direction.down_left"] = "abajo a la izquierda",
    ["guidance.direction.left"] = "izquierda", ["guidance.direction.up_left"] = "arriba a la izquierda",
};
ExplorationText text = (key, args) => string.Format(words[key], args);
var player = new Vector3(0, 0, 0);
var forward = new Vector3(0, 0, 1);
var right = new Vector3(1, 0, 0);
const float step = 1f;

ExplorationEntry Entry(string key, string name, ExplorationCategory category, float x, float z, bool boss = false) =>
    new(key, name, category, new Vector3(x, 0, z), 1.5f, boss);

// Empty catalogue.
var catalog = new ExplorationCatalog();
Equal("Nada descubierto cerca", catalog.StepCategory(1, new List<ExplorationEntry>(), player, forward, right, step, text), "empty");
Equal(null, catalog.Category, "empty category");

var entries = new List<ExplorationEntry>
{
    Entry("exit-far", "Salida hacia Jardines", ExplorationCategory.Exits, 0, 20),
    Entry("exit-near", "Salida hacia Cueva", ExplorationCategory.Exits, -5, 5),
    Entry("npc", "Maeve", ExplorationCategory.Characters, 3, 0),
    Entry("boss", "Rahyeh", ExplorationCategory.Enemies, 0, -1, boss: true),
};

// Down (next) from nothing starts at the first non-empty category, nearest entry first.
Equal("Salidas, 2. Salida hacia Cueva, 7 pasos, arriba a la izquierda",
    catalog.StepCategory(1, entries, player, forward, right, step, text), "first category");
Equal("exit-near", catalog.SelectedKey, "nearest selected");
Equal("Salida hacia Jardines, 20 pasos, arriba", catalog.StepEntry(1, entries, player, forward, right, step, text), "next entry");
Equal("Salida hacia Cueva, 7 pasos, arriba a la izquierda", catalog.StepEntry(1, entries, player, forward, right, step, text), "entry wraps");
Equal("Salida hacia Jardines, 20 pasos, arriba", catalog.StepEntry(-1, entries, player, forward, right, step, text), "previous wraps");

// Categories skip empty ones and wrap both ways.
Equal("Personajes, 1. Maeve, 3 pasos, derecha", catalog.StepCategory(1, entries, player, forward, right, step, text), "skip empty");
Equal("Enemigos, 1. Rahyeh, jefe, 1 paso, abajo", catalog.StepCategory(1, entries, player, forward, right, step, text), "boss and one step");
catalog.StepCategory(1, entries, player, forward, right, step, text);
Equal(ExplorationCategory.Exits, catalog.Category, "wrap forward");
catalog.StepCategory(-1, entries, player, forward, right, step, text);
Equal(ExplorationCategory.Enemies, catalog.Category, "wrap back");

// Up (previous) from nothing starts at the last category.
var fresh = new ExplorationCatalog();
fresh.StepCategory(-1, entries, player, forward, right, step, text);
Equal(ExplorationCategory.Enemies, fresh.Category, "previous from nothing");

// An entry step with no category behaves like the first category step.
var entryFirst = new ExplorationCatalog();
Equal("Salidas, 2. Salida hacia Cueva, 7 pasos, arriba a la izquierda",
    entryFirst.StepEntry(1, entries, player, forward, right, step, text), "entry without category");

// Selected entry follows its key after re-sorting; a vanished category reports it is empty.
Equal("exit-near", entryFirst.Selected(entries)?.Key, "selected lookup");
var moved = new Vector3(0, 0, 18);
// After walking north the far exit is nearest: order is [Jardines, Cueva]; next after Cueva wraps to Jardines.
Equal("Salida hacia Jardines, 2 pasos, arriba",
    entryFirst.StepEntry(1, entries, moved, forward, right, step, text), "resorted step moves from selection");
var withoutExits = entries.Where(e => e.Key != "exit-near" && e.Key != "exit-far").ToList();
Equal("Salidas: ninguno", entryFirst.StepEntry(1, withoutExits, player, forward, right, step, text), "category emptied");
Equal(null, entryFirst.Selected(withoutExits)?.Key, "no selection after emptying");
// From an emptied category, next continues in the fixed order (Characters after Exits), previous wraps back.
entryFirst.StepCategory(1, withoutExits, player, forward, right, step, text);
Equal(ExplorationCategory.Characters, entryFirst.Category, "continue after emptied");
var emptiedBack = new ExplorationCatalog();
emptiedBack.StepCategory(1, entries, player, forward, right, step, text);
emptiedBack.StepCategory(-1, withoutExits, player, forward, right, step, text);
Equal(ExplorationCategory.Enemies, emptiedBack.Category, "previous from emptied first wraps");

// Within one step says "here"; step length scales distance; height does not count.
Equal("Maeve, aquí", ExplorationCatalog.Describe(Entry("n", "Maeve", ExplorationCategory.Characters, 0.3f, 0), player, forward, right, step, text), "here");
Equal("Maeve, 2 pasos, derecha", ExplorationCatalog.Describe(Entry("n", "Maeve", ExplorationCategory.Characters, 4, 0), player, forward, right, 2f, text), "step length");
Equal("Maeve, 3 pasos, derecha", ExplorationCatalog.Describe(new ExplorationEntry("n", "Maeve", ExplorationCategory.Characters, new Vector3(3, 9, 0), 1), player, forward, right, step, text), "horizontal distance");

// Non-finite entries are ignored.
var broken = new List<ExplorationEntry> { new("x", "X", ExplorationCategory.Exits, new Vector3(float.NaN, 0, 0), 1) };
Equal("Nada descubierto cerca", new ExplorationCatalog().StepCategory(1, broken, player, forward, right, step, text), "non-finite ignored");

// Seen memory: places keyed by name and coarse position, characters by name; scenes are separate.
var memory = new ExplorationSeenMemory();
memory.Enter(ExplorationSeenMemory.Scope("Z1", 7, fixedLayout: true));
Equal(true, memory.Add(ExplorationSeenMemory.Key(ExplorationCategory.Exits, "Salida", new Vector3(10.4f, 0, 3))), "add place");
Equal(true, memory.Contains(ExplorationSeenMemory.Key(ExplorationCategory.Exits, "Salida", new Vector3(10.6f, 5, 3.2f))), "same coarse cell");
Equal(false, memory.Contains(ExplorationSeenMemory.Key(ExplorationCategory.Exits, "Salida", new Vector3(30, 0, 3))), "other place");
memory.Add(ExplorationSeenMemory.Key(ExplorationCategory.Characters, "Maeve", new Vector3(0, 0, 0)));
Equal(true, memory.Contains(ExplorationSeenMemory.Key(ExplorationCategory.Characters, "Maeve", new Vector3(50, 0, 50))), "character by name");
memory.Enter(ExplorationSeenMemory.Scope("Z2", 8, fixedLayout: true));
Equal(0, memory.Count, "new scene empty");
memory.Enter(ExplorationSeenMemory.Scope("Z1", 99, fixedLayout: true));
Equal(2, memory.Count, "fixed scene kept across sessions");
Equal("PCG#5", ExplorationSeenMemory.Scope("PCG", 5, fixedLayout: false), "procedural scope uses session");

// Seen memory export/import: only fixed scopes, a version bump on change.
var saved = new ExplorationSeenMemory();
saved.Enter("Town");
int version = saved.Version;
saved.Add("1|Salida|5|1");
saved.Add("1|Salida|5|1");
Equal(version + 1, saved.Version, "version bumps once per new key");
saved.Enter("PCG#3");
saved.Add("2|Cofre|0|0");
Equal(1, saved.FixedScopes().Count(), "procedural scopes are not exported");
var restored = new ExplorationSeenMemory();
Equal(1, restored.ImportFixed("Town", saved.FixedScopes().First().Value), "import adds keys");
Equal(0, restored.ImportFixed("Town", new[] { "1|Salida|5|1", "" }), "import dedupes and skips empty keys");
Equal(0, restored.ImportFixed("PCG#3", new[] { "x" }), "procedural scope import refused");
restored.Enter("Town");
Equal(true, restored.Contains("1|Salida|5|1"), "imported key is remembered");
Equal(1, restored.Count, "entering an imported scope reuses it");

Console.WriteLine($"Exploration catalog tests passed: {checks} checks");
