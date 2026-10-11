using LeAccess.Accessibility.Exploration;

int checks = 0;
void Check(bool condition, string label) { checks++; if (!condition) throw new Exception(label); }
void Is(ExplorationCommand expected, float x, float y, float px, float py, string label) =>
    Check(StickFlick.Detect(x, y, px, py) == expected, $"{label}: expected {expected}, got {StickFlick.Detect(x, y, px, py)}");

Is(ExplorationCommand.CategoryPrevious, 0, 1, 0, 0, "up");
Is(ExplorationCommand.CategoryNext, 0, -1, 0, 0, "down");
Is(ExplorationCommand.EntryPrevious, -1, 0, 0, 0, "left");
Is(ExplorationCommand.EntryNext, 1, 0, 0, 0, "right");
Is(ExplorationCommand.CategoryPrevious, 0, 0.5f, 0, 0, "threshold is inclusive");
Is(ExplorationCommand.None, 0, 0.49f, 0, 0, "below threshold");
Is(ExplorationCommand.None, 0.2f, -0.3f, 0, 0, "drift");
Is(ExplorationCommand.EntryNext, 0.9f, 0.6f, 0, 0, "horizontal dominant");
Is(ExplorationCommand.CategoryNext, 0.6f, -0.9f, 0, 0, "vertical dominant down");
Is(ExplorationCommand.CategoryPrevious, 0.7f, 0.7f, 0, 0, "exact diagonal prefers vertical");
Is(ExplorationCommand.None, 0, 1, 0, 1, "held stick does not repeat");
Is(ExplorationCommand.None, 1, 0, 0, 1, "rotating a held stick needs neutral");
Is(ExplorationCommand.None, 0, 1, 0.5f, 0, "previous at threshold is not neutral");
Is(ExplorationCommand.CategoryPrevious, 0, 1, 0.49f, 0.49f, "previous just under threshold is neutral");
Is(ExplorationCommand.None, float.NaN, 1, 0, 0, "NaN rejected");
Is(ExplorationCommand.None, 0, 1, 0, float.PositiveInfinity, "infinity rejected");

// One command per flick: simulate a frame sequence of one push, hold, release, second push.
var frames = new (float x, float y)[] { (0, 0), (0.3f, 0.1f), (0, 0.9f), (0, 1), (0.1f, 0.8f), (0, 0.2f), (0, 0), (-1, 0.1f), (-1, 0) };
float px = 0, py = 0; var fired = new List<ExplorationCommand>();
foreach (var (x, y) in frames)
{
    var command = StickFlick.Detect(x, y, px, py);
    if (command != ExplorationCommand.None) fired.Add(command);
    px = x; py = y;
}
Check(fired.Count == 2 && fired[0] == ExplorationCommand.CategoryPrevious && fired[1] == ExplorationCommand.EntryPrevious,
    "A held push yields one command and the next flick another");

// The stick scans only while Create is held; otherwise the game keeps it.
foreach (var flick in Enum.GetValues<ExplorationCommand>())
{
    Check(ScannerGate.Controller(true, flick) == flick, $"Create held passes {flick}");
    Check(ScannerGate.Controller(false, flick) == ExplorationCommand.None, $"Create up drops {flick}");
}

// Create stays the mod button on PlayStation pads; other pads use L3 unless the option is off.
Check(ModifierChoice.For(true, true) == ModifierButton.Back, "DualSense/DualShock keep Create with the option on");
Check(ModifierChoice.For(true, false) == ModifierButton.Back, "DualSense/DualShock keep Create with the option off");
Check(ModifierChoice.For(false, true) == ModifierButton.LeftStick, "Other pads use L3");
Check(ModifierChoice.For(false, false) == ModifierButton.Back, "Other pads fall back to Back/View with the option off");

// Suppression ledger with fakes.
var enabled = new Dictionary<string, bool> { ["a"] = true, ["b"] = true, ["c"] = true, ["d"] = false };
var held = new HashSet<string>();
var enableLog = new List<string>();
void Enable(string id) { enabled[id] = true; enableLog.Add(id); }
var ledger = new BindingSuppression<string>();
foreach (var id in new[] { "a", "b", "c" }) { enabled[id] = false; ledger.Record(id); }
Check(ledger.Count == 3, "Ledger remembers each disabled binding");
held.Add("b");
int restored = ledger.ReleaseWhenUp(held.Contains, Enable);
Check(restored == 2 && ledger.Count == 1 && enabled["a"] && enabled["c"] && !enabled["b"], "Only released bindings are re-enabled");
Check(!enabled["d"] && !enableLog.Contains("d"), "A binding the mod did not disable is never enabled");
Check(ledger.ReleaseWhenUp(held.Contains, Enable) == 0 && !enabled["b"], "A still-held binding stays disabled");
held.Clear();
Check(ledger.ReleaseWhenUp(held.Contains, Enable) == 1 && enabled["b"] && ledger.Count == 0, "Binding restores once its button is up");
Check(enableLog.Count(x => x == "b") == 1 && ledger.ReleaseWhenUp(held.Contains, Enable) == 0, "Nothing is restored twice");

foreach (var id in new[] { "a", "b" }) { enabled[id] = false; ledger.Record(id); }
held.Add("a"); held.Add("b");
Check(ledger.RestoreAll(Enable) == 2 && enabled["a"] && enabled["b"] && ledger.Count == 0, "RestoreAll ignores held hardware and empties the ledger");
Check(ledger.RestoreAll(Enable) == 0, "RestoreAll on an empty ledger is a no-op");

ledger.Record("a"); ledger.Record("b"); enabled["a"] = enabled["b"] = false;
int calls = 0;
ledger.RestoreAll(id => { calls++; if (id == "a") throw new InvalidOperationException("collected"); Enable(id); });
Check(calls == 2 && enabled["b"] && ledger.Count == 0, "A failing restore cannot strand the others");
ledger.Record("a"); ledger.Record("b"); enabled["a"] = enabled["b"] = false; held.Clear();
ledger.ReleaseWhenUp(id => id == "a" ? throw new Exception("stale") : false, Enable);
Check(ledger.Count == 0 && enabled["a"] && enabled["b"], "A button state read that throws is treated as released");

// The mod button's face buttons: Square vitals, Triangle gold and points, Circle repeats the entry, Cross locks.
Check(FaceButtonCommand.For(true, false, false, false) == ExplorationCommand.Vitals, "Square reads the vitals");
Check(FaceButtonCommand.For(false, false, true, false) == ExplorationCommand.Gold, "Triangle reads gold and points");
Check(FaceButtonCommand.For(false, false, false, true) == ExplorationCommand.RepeatEntry, "Circle repeats the scanner entry");
Check(FaceButtonCommand.For(false, true, false, false) == ExplorationCommand.Lock, "Cross locks the beacon");
Check(FaceButtonCommand.For(false, false, false, false) == ExplorationCommand.None, "no face button, no command");
Check(FaceButtonCommand.For(true, true, true, true) == ExplorationCommand.Vitals &&
    FaceButtonCommand.For(false, true, true, true) == ExplorationCommand.Lock &&
    FaceButtonCommand.For(false, false, true, true) == ExplorationCommand.Gold, "several buttons in one frame resolve in a fixed order");
Check(ScannerGate.Controller(false, ExplorationCommand.EntryNext) == ExplorationCommand.None &&
    ScannerGate.Controller(true, ExplorationCommand.EntryNext) == ExplorationCommand.EntryNext, "the stick belongs to the scanner only while the modifier is held");

Console.WriteLine($"Exploration input tests passed: {checks} checks.");
