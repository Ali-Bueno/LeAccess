using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Forge;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

int checks = 0;
void Check(bool value, string name) { checks++; if (!value) throw new Exception(name); }
string T(string key) => ModText.Get(key);
string F(string key, params object[] args) => ModText.Format(key, args);
string Join(params string[] parts) => ScreenContentTracker.Combine(parts);

// The forge prefab's grid, [column, row]: row 1/2 prefixes (add at 1, upgrade at 0) and suffixes (5, 6),
// item (3,1), glyph (3,2), shard (3,3), rune (4,3), forge (3,4), materials (0,0), help (6,0).
var panel = new GameObject();
var forge = panel.Add(new CraftingSlotManager());
var grid = new NativeObject[7, 5];
TextMeshProUGUI Label(GameObject owner, string text) => owner.Child().Add(new TextMeshProUGUI { text = text });
GameObject Cell(int x, int y)
{
    var go = panel.Child();
    grid[x, y] = go.Add(new RectTransform());
    return go;
}
forge.activeNavigationElements = new Il2CppSystem.Array(grid);
Cell(0, 0).Add(new CraftingMaterialsToggleButton());
var help = Cell(6, 0); help.Add(new Button()); Label(help, "Ayuda del juego");
var prefixOne = Cell(1, 1).Add(new AffixSlotForge { slotID = AffixSlotForge.AffixSlotID.PREFIX_ONE, availableUpgrade = true });
prefixOne.nameTMP = Label(prefixOne.gameObject, "Vida añadida");
prefixOne.tierTMP = Label(prefixOne.gameObject, "T3");
prefixOne.countTMP = Label(prefixOne.gameObject, "12");
var upgradeGo = Cell(0, 1);
var upgrade = upgradeGo.Add(new CraftingUpgradeButton { affixSlotForge = prefixOne, _upgradeButton = upgradeGo.Add(new Button()) });
prefixOne.upgradeButton = upgrade;
var suffixTwo = Cell(5, 2).Add(new AffixSlotForge { slotID = AffixSlotForge.AffixSlotID.SUFFIX_TWO });
suffixTwo.addAffixButton = suffixTwo.gameObject.Child().Add(new Button());
Cell(3, 1).Add(new CraftingMainUI());
Cell(3, 2).Add(new CraftingSupportUI());
Cell(3, 3).Add(new CraftingModifierUI());
Cell(4, 3).Add(new CraftingRuneButton());
var forgeGo = Cell(3, 4);
forge.forgeButton = forgeGo.Add(new Button());
forge.forgeButtonText = Label(forgeGo, "Mejorar afijo");
forge.cannotForgeText = Label(panel, "Necesitas un objeto");
forge.minForgingPotentialText = Label(panel, "2");
forge.maxForgingPotentialText = Label(panel, "6");
forge.forgingPotentialText = Label(panel, "28");
NavigationManager.currentlySelected = forge;

bool Read(int x, int y, out ForgeCell cell) { forge.currentlySelectedIdx = new Vector2Int(x, y); return ForgeCellReader.TryRead(forge, out cell); }
Check(Read(1, 1, out var cell) && cell.Brief == Join(F("forge.prefix", 1), "Vida añadida", F("forge.tier", 3),
    F("forge.shards_available", "12"), T("forge.upgrade_left")),
    "A filled prefix names its slot, affix, tier, shards and that its upgrade is to the left: " + cell.Brief);
Check(Read(5, 2, out cell) && cell.Brief == Join(F("forge.suffix", 2), T("forge.slot_empty"), T("forge.add_hint")),
    "A free slot says Cross chooses a shard");
suffixTwo.addAffixButton.gameObject.Active = false;
Check(Read(5, 2, out cell) && cell.Brief == Join(F("forge.suffix", 2), T("forge.slot_unavailable")), "A slot that cannot take an affix says so");
Check(Read(0, 1, out cell) && cell.Brief == Join(F("forge.upgrade", "Vida añadida"), F("forge.tier", 3), F("forge.shards_available", "12"),
    T("forge.upgrade_hint")), "The upgrade cell names the affix and what Cross does");
upgrade._upgradeButton.interactable = false;
Check(Read(0, 1, out cell) && cell.Brief.EndsWith(T("forge.slot_unavailable")), "An upgrade the game refuses says it is unavailable");
Check(Read(3, 1, out cell) && cell.Brief == T("forge.item_empty"), "An empty item slot says how to bring an item");
forge.main.Put("Espada de hueso");
Check(Read(3, 1, out cell) && cell.Brief == Join(F("forge.item", "Espada de hueso"), F("forge.potential", "28"), T("forge.item_hint")),
    "The item slot names the item and its forging potential");
Check(Read(3, 2, out cell) && cell.Brief == T("forge.glyph_empty"), "An empty glyph slot");
forge.support.Put("Glifo de esperanza");
Check(Read(3, 2, out cell) && cell.Brief == F("forge.glyph", "Glifo de esperanza"), "The glyph in place");
Check(Read(3, 3, out cell) && cell.Brief == T("forge.modifier_empty"), "An empty shard slot");
forge.modifier.Put("Fragmento de vida");
Check(Read(3, 3, out cell) && cell.Brief == F("forge.modifier", "Fragmento de vida"), "The shard in place");
Check(Read(4, 3, out cell) && cell.Brief == T("forge.runes"), "The rune button");
Check(Read(3, 4, out cell) && cell.Brief == Join("Mejorar afijo", F("forge.cost", "2", "6")), "Forge names the game's action and the cost range");
forge.canForge = false;
Check(Read(3, 4, out cell) && cell.Brief == Join("Mejorar afijo", "Necesitas un objeto"), "A refused forge gives the game's reason");
forge.canForge = true;
Check(Read(0, 0, out cell) && cell.Brief == T("forge.materials") && Read(6, 0, out cell) && cell.Brief == "Ayuda del juego",
    "The materials toggle and the help button");
Check(!Read(2, 0, out _) && !Read(9, 9, out _), "Empty and out-of-range cells read nothing");

// The service: opening, moves, details, results and closing. Text speaks once it is stable for two frames.
void Tick() { for (int i = 0; i < 2; i++) { ForgeAccessibility.Update(); ScreenReader.SpokeThisFrame = false; } }
ForgeAccessibility.Reset(); ScreenReader.Messages.Clear(); ForgeResultLog.Install();
forge.currentlySelectedIdx = new Vector2Int(3, 3);
Tick();
Check(ScreenReader.Messages.Single().Text == Join(T("forge.opened"), F("forge.item", "Espada de hueso"), F("forge.modifier", "Fragmento de vida")),
    "Opening the forge names it and its item before the first cell: " + ScreenReader.Messages.Single().Text);
Tick();
Check(ScreenReader.Messages.Count == 1, "A cell speaks once");
forge.currentlySelectedIdx = new Vector2Int(3, 4);
Tick();
Check(ScreenReader.Messages.Last().Text == Join("Mejorar afijo", F("forge.cost", "2", "6")), "Moving speaks the new cell only");
ForgeSummaryReader.Summary = "Probabilidad de éxito 100%";
ForgeAccessibility.Update(readDetails: true); ScreenReader.SpokeThisFrame = false;
Check(ScreenReader.Messages.Last().Text == Join("Mejorar afijo", F("forge.cost", "2", "6"), "Probabilidad de éxito 100%"),
    "Details add the forge summary");
forge.singlelineFractureChanceTMP = Label(panel, "Afijo mejorado: Vida añadida");
forge.lostForgingPotentialTMP = Label(panel, "Potencial perdido: 4");
BepInEx.Logging.Logger.Listeners.Single().LogEvent(null, new BepInEx.Logging.LogEventArgs { Data = "[Forge Craft] Result | Crafting Success: True" });
Tick();
Check(ScreenReader.Messages.Last().Text == F("forge.result", Join("Afijo mejorado: Vida añadida", "Potencial perdido: 4")),
    "The game's result line makes the result labels speak: " + ScreenReader.Messages.Last().Text);
int count = ScreenReader.Messages.Count;
Tick();
Check(ScreenReader.Messages.Count == count, "A result speaks once");
BepInEx.Logging.Logger.Listeners.Single().LogEvent(null, new BepInEx.Logging.LogEventArgs { Data = "[Forge Craft] Result" });
forge.currentlySelectedIdx = new Vector2Int(3, 3);
Tick();
Check(ScreenReader.Messages.Last().Text == Join(F("forge.result", Join("Afijo mejorado: Vida añadida", "Potencial perdido: 4")),
    F("forge.modifier", "Fragmento de vida")), "When the game moves focus after a craft, the result leads the new cell");
BepInEx.Logging.Logger.Listeners.Single().LogEvent(null, new BepInEx.Logging.LogEventArgs { Data = "[Forge Craft] Input" });
count = ScreenReader.Messages.Count;
Tick();
Check(ScreenReader.Messages.Count == count, "Other forge log lines are ignored");
Check(ForgeAccessibility.TryHandleFocus(help) && !ForgeAccessibility.TryHandleFocus(new GameObject()),
    "The generic announcer leaves the forge grid's cells to this reader");
panel.Active = false; Tick();
panel.Active = true; Tick();
Check(ScreenReader.Messages.Last().Text.StartsWith(T("forge.opened")), "Reopening the forge introduces it again");
NavigationManager.currentlySelected = null;
ForgeAccessibility.Update();
Check(!ForgeAccessibility.HasActiveContent && !ForgeAccessibility.TryHandleFocus(help), "Another navigable releases the forge");
ForgeResultLog.Uninstall();
Check(BepInEx.Logging.Logger.Listeners.Count == 0, "Unloading removes the log listener");
ForgeNoticeTests.Run((value, name) => Check(value, name));
Console.WriteLine($"Forge tests passed: {checks} checks.");
