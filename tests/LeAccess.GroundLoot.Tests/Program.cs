using LeAccess.Accessibility;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Items;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;

int checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}
void Tick(bool otherSpeechPending = false)
{
    Time.frameCount++;
    ScreenReader.SpokeThisFrame = false;
    GroundLootAccessibility.Update(otherSpeechPending);
}
void Reset()
{
    GroundLootAccessibility.Reset();
    ScreenReader.Messages.Clear();
    ScreenReader.Available = true;
    ScreenReader.Speaking = false;
    ScreenReader.SpokeThisFrame = false;
    NavigationAudioState.IsSuspended = false;
    ScreenContentAnnouncer.HasActiveContent = false;
    DialogueAnnouncer.HasActiveContent = false;
    InventoryAccessibility.HasActiveContent = false;
    NavigationManager.currentlySelected = null;
    PlayerFinder.player = new GameObject();
    EpochInputManager.IsControllerActive = true;
    ControllerManager.instance = new GameObject().Add(new ControllerManager
    {
        _CurrentMap_k__BackingField = new DefaultMap()
    });
    EpochInputManager.instance = new GameObject().Add(new EpochInputManager { InteractableTargeting = new InteractableTargeting() });
    TooltipItem.highlightedTooltipItem = null;
    ItemTooltipReader.Opener = null;
    ItemTooltipReader.Item = null;
    ItemTooltipReader.Text = null;
    ItemTooltipReader.Slot = TooltipItemManager.SlotType.GROUND;
    LeAccess.Plugin.Log.Warnings.Clear();
}
GroundItemLabel Item(string name, uint id = 1)
{
    var root = new GameObject();
    var label = root.Add(new GroundItemLabel
    {
        itemText = root.Child().Add(new TMPro.TextMeshProUGUI { text = name }),
        visuals = new GameObject().Add(new GroundItemVisuals { itemData = new ItemDataUnpacked(), id = id })
    });
    label.tooltipItem = root.Child().Add(new TooltipItem { slotType = TooltipItemManager.SlotType.GROUND });
    return label;
}
void Focus(PickupableGroundLabel label) => EpochInputManager.instance.InteractableTargeting.ActiveTarget = new Targetable
{ item = label, type = Targetable.TargetType.GroundLabel };
void Details(GroundItemLabel item, string text)
{
    ItemTooltipReader.Opener = item.tooltipItem.gameObject;
    ItemTooltipReader.Item = item.visuals.itemData;
    ItemTooltipReader.Text = text;
}

Reset();
var selected = Item(" <color=blue>Espada de hielo</color> ");
Focus(selected);
Tick();
Check(ScreenReader.Messages.Count == 0 && GroundLootAccessibility.HasPendingContent,
    "Focused label waits for a second stable frame");
GroundLootAccessibility.Update();
Check(ScreenReader.Messages.Count == 0, "Duplicate calls in one frame cannot settle a label");
Tick();
Check(ScreenReader.Messages.Single().Text == "Espada de hielo", "Real localized visible label is cleaned and spoken");
Check(!ScreenReader.Messages[0].Interrupt, "Ground focus speech uses the noninterrupting sink");
for (int frame = 0; frame < 20; frame++) Tick();
Check(ScreenReader.Messages.Count == 1 && GroundLootAccessibility.HasActiveTarget,
    "An unchanged selected drop is announced once");

Reset();
var first = Item("Primero");
var second = Item("Segundo", 2);
Focus(first); Tick();
Focus(second); Tick();
Check(ScreenReader.Messages.Count == 0, "Changing target discards the previous unsettled label");
Tick();
Check(ScreenReader.Messages.Single().Text == "Segundo", "Only the latest target is spoken");
Focus(first); Tick(); Tick();
Check(ScreenReader.Messages.Last().Text == "Primero", "Returning to an earlier target reads it again");

Reset();
selected = Item("Anillo"); Focus(selected); Tick();
selected.itemText.text = "Anillo ártico"; Tick();
Check(ScreenReader.Messages.Count == 0, "Localization changes restart settlement");
Tick();
Check(ScreenReader.Messages.Single().Text == selected.itemText.text, "Settled localized name wins");
Details(selected, "Anillo ártico. +12 de resistencia al frío"); Tick();
Check(ScreenReader.Messages.Count == 1, "Automatic tooltip population does not repeat the drop");
Tick();
Check(ScreenReader.Messages.Single().Text == selected.itemText.text, "Ground focus stays name-only even when stats appear");
ItemTooltipReader.Text = null; Tick(); Tick();
Check(ScreenReader.Messages.Count == 1, "Closing details does not repeat the bare name");

foreach (string mismatch in new[] { "slot", "opener", "item" })
{
    Reset(); selected = Item("Guantes"); Focus(selected); Details(selected, "INCORRECTO");
    if (mismatch == "slot") ItemTooltipReader.Slot = TooltipItemManager.SlotType.INVENTORY;
    if (mismatch == "opener") ItemTooltipReader.Opener = new GameObject();
    if (mismatch == "item") ItemTooltipReader.Item = new ItemDataUnpacked();
    Tick(); Tick();
    Check(ScreenReader.Messages.Single().Text == selected.itemText.text, $"Reject stale tooltip with wrong {mismatch}");
}
Reset(); selected = Item("Botas"); Focus(selected); Details(selected, "Botas. +4 de armadura");
ItemTooltipReader.Opener = selected.gameObject; Tick(); Tick();
Check(ScreenReader.Messages.Single().Text == selected.itemText.text, "Already-populated ground tooltip still speaks only the visible name");
selected.visuals.gameObject.Active = false; Tick(); Tick();
Check(ScreenReader.Messages.Count == 1 && !GroundLootAccessibility.HasPendingContent,
    "Picking up an item after tooltip population leaves no delayed property reading");

foreach (string obstruction in new[] { "hidden", "parent", "alpha", "cull", "canvas", "group", "disabled", "collected", "destroyed", "visuals", "data" })
{
    Reset(); selected = Item("Oculto"); Focus(selected);
    switch (obstruction)
    {
        case "hidden": selected.gameObject.Active = false; break;
        case "parent": selected.gameObject.Parent = new GameObject { Active = false }; break;
        case "alpha": selected.itemText.alpha = 0; break;
        case "cull": selected.itemText.canvasRenderer.cull = true; break;
        case "canvas": selected.gameObject.Add(new Canvas { Enabled = false }); break;
        case "group": selected.gameObject.Add(new CanvasGroup { alpha = 0 }); break;
        case "disabled": selected.Enabled = false; break;
        case "collected": selected.WasCollected = true; break;
        case "destroyed": selected.Destroyed = true; break;
        case "visuals": selected.visuals.gameObject.Active = false; break;
        case "data": selected.visuals.itemData.WasCollected = true; break;
    }
    Tick(); Tick();
    Check(ScreenReader.Messages.Count == 0 && !GroundLootAccessibility.HasActiveTarget,
        $"Do not read a {obstruction} ground target");
    Check(LeAccess.Plugin.Log.Warnings.Count == 0, $"Handle {obstruction} lifecycle without unsafe wrapper access");
}

foreach (string gate in new[] { "suspended", "popup", "dialogue", "inventory", "navigation", "map", "player" })
{
    Reset(); selected = Item("Retenido"); Focus(selected); Tick();
    switch (gate)
    {
        case "suspended": NavigationAudioState.IsSuspended = true; break;
        case "popup": ScreenContentAnnouncer.HasActiveContent = true; break;
        case "dialogue": DialogueAnnouncer.HasActiveContent = true; break;
        case "inventory": InventoryAccessibility.HasActiveContent = true; break;
        case "navigation": NavigationManager.currentlySelected = new INavigable { Id = NavigableID.INVENTORY_CONTAINER }; break;
        case "map": ControllerManager.instance._CurrentMap_k__BackingField = new InventoryMap(); break;
        case "player": PlayerFinder.player = null; break;
    }
    Tick(); Tick();
    Check(ScreenReader.Messages.Count == 0 && !GroundLootAccessibility.HasPendingContent,
        $"Discard retained ground target while {gate} owns context");
}

Reset(); selected = Item("Poción"); Focus(selected); Tick();
EpochInputManager.instance.InteractableTargeting.ActiveTarget = null; Tick(); Tick();
Check(ScreenReader.Messages.Count == 0, "Lost or picked-up target never speaks from stale pending state");
Focus(selected); Tick(); Tick();
Check(ScreenReader.Messages.Count == 1, "Focus can recover after a lost target");
selected.visuals.id++; Tick(); Tick();
Check(ScreenReader.Messages.Count == 2, "A reused label with new drop ID is a new item even with identical text");
selected.visuals.itemData = new ItemDataUnpacked(); Tick(); Tick();
Check(ScreenReader.Messages.Count == 3, "Changed item data also invalidates a pooled label identity");

Reset(); selected = Item("Ratón"); EpochInputManager.IsControllerActive = false;
TooltipItem.highlightedTooltipItem = selected.tooltipItem; Tick(); Tick();
Check(ScreenReader.Messages.Single().Text == "Ratón", "Mouse uses the actual highlighted ground tooltip owner");
TooltipItem.highlightedTooltipItem = null; Tick();
Check(!GroundLootAccessibility.HasActiveTarget, "Mouse exit clears the ground target");
TooltipItem.highlightedTooltipItem = selected.tooltipItem;
selected.tooltipItem.slotType = TooltipItemManager.SlotType.INVENTORY; Tick(); Tick();
Check(ScreenReader.Messages.Count == 1, "Inventory hover cannot be read as ground loot");

Reset(); selected = Item("Mando"); Focus(selected);
TooltipItem.highlightedTooltipItem = Item("Ratón anterior").tooltipItem; Tick(); Tick();
Check(ScreenReader.Messages.Single().Text == "Mando", "Controller selection wins over stale mouse highlight");
Reset(); selected = Item("Suelo"); Focus(selected);
NavigationManager.currentlySelected = new INavigable { Id = NavigableID.GROUND_ITEM_LABELS }; Tick(); Tick();
Check(ScreenReader.Messages.Single().Text == "Suelo", "Authoritative ground-label navigable is allowed");

Reset(); selected = Item("Importante"); Focus(selected); ScreenReader.Speaking = true; Tick(); Tick();
Check(ScreenReader.Messages.Single().Text == "Importante", "Stuck backend speaking state cannot starve actual focused loot");
Reset(); selected = Item("Pendiente"); Focus(selected); ScreenReader.Available = false; Tick(); Tick();
Check(ScreenReader.Messages.Count == 0 && GroundLootAccessibility.HasPendingContent,
    "Unavailable reader keeps only current focused candidate");
EpochInputManager.instance.InteractableTargeting.ActiveTarget = null; Tick();
ScreenReader.Available = true; Tick();
Check(ScreenReader.Messages.Count == 0, "Expired candidate is not sent when the reader returns");
Reset(); selected = Item("Primer objeto"); Focus(selected); Tick(true); Tick(true);
Check(ScreenReader.Messages.Count == 0 && GroundLootAccessibility.HasPendingContent,
    "A pending zone announcement takes priority over automatic loot speech");
selected = Item("Objeto actual", 2); Focus(selected); Tick(true); Tick(true);
Tick();
Check(ScreenReader.Messages.Single().Text == "Objeto actual",
    "Only the current brief item name follows a higher-priority announcement");
Reset();
foreach (string invalid in new[] { "manager absent", "manager collected", "manager disabled", "targeting absent", "targeting collected", "other target" })
{
    Reset(); selected = Item("Actual"); Focus(selected);
    switch (invalid)
    {
        case "manager absent": EpochInputManager.instance = null; break;
        case "manager collected": EpochInputManager.instance.WasCollected = true; break;
        case "manager disabled": EpochInputManager.instance.Enabled = false; break;
        case "targeting absent": EpochInputManager.instance.InteractableTargeting = null; break;
        case "targeting collected": EpochInputManager.instance.InteractableTargeting.WasCollected = true; break;
        case "other target": EpochInputManager.instance.InteractableTargeting.ActiveTarget.type = Targetable.TargetType.Interactable; break;
    }
    Tick(); Tick();
    Check(ScreenReader.Messages.Count == 0 && LeAccess.Plugin.Log.Warnings.Count == 0,
        $"Fresh input targeting rejects {invalid} without stale ground speech or native access");
}
Reset();
Console.WriteLine($"Ground loot tests passed: {checks} checks.");
