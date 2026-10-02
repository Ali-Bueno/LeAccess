using LeAccess.Accessibility;
using LeAccess.Accessibility.Inventory;
using LeAccess.Accessibility.Items;
using LeAccess.Accessibility.ScreenContent;
using UnityEngine;

int checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}
void UpdateTwice() { InventoryAccessibility.Update(); InventoryAccessibility.Update(); }

var state = new InventoryAnnouncementState();
Check(!state.Observe("a", "Old item", true, out _) && state.HasPendingContent, "First item snapshot settles");
Check(!state.Observe("b", "New item", true, out _), "Rapid navigation discards unspoken previous item");
Check(state.Observe("b", "New item", true, out var speech) && speech == "New item", "Latest item speaks once stable");
Check(!state.Observe("b", "New item", true, out _) && !state.HasPendingContent, "Unchanged item never repeats");
Check(!state.Observe("b", "Updated stats", false, out _) && state.HasPendingContent, "Stat update waits while higher-priority speech owns frame");
Check(state.Observe("b", "Updated stats", true, out speech) && speech == "Updated stats", "Same item changed stats are announced");
Check(!state.Observe("empty", "Empty slot", true, out _), "Moving off item clears old candidate");
Check(state.Observe("empty", "Empty slot", true, out speech) && speech == "Empty slot", "Empty slot cannot repeat the previous item");
Check(!state.Observe("missing", null, true, out _) && !state.HasPendingContent, "Missing item text cannot hold pending speech forever");

var container = new ItemContainerUI();
var item = new ItemDataUnpacked { Id = "selected", FullName = "Espada", LocalisedBaseTypeName = "Espada de una mano" };
var entry = new ItemContainerEntry { data = item };
container.currentlySelectedItem = entry;
var opener = new GameObject();
opener.transform.parent = container.transform;
var tooltip = new UITooltipItem();
tooltip.nameText.text = "Espada del fuego";
tooltip.typeText.text = "Raro. Espada de una mano";
tooltip.content.Text = "+15 daño cuerpo a cuerpo. +8 fuerza";
tooltip.compareContent.Text = "Old equipped sword. +99 obsolete comparison";
tooltip.requirementsFooter = new GameObject { Text = "Requiere nivel 12" };
var parameters = new TooltipItemManager.ItemTooltipParameters
{
    _Item_k__BackingField = item,
    _Opener_k__BackingField = opener,
    _SlotType_k__BackingField = TooltipItemManager.SlotType.INVENTORY
};
var manager = new TooltipItemManager { activeParameters = parameters, itemTooltip = tooltip };
var system = new LE.UI.Tooltips.TooltipSystem { _tooltipItemManager = manager, ActiveItemTooltipRect = manager.ActiveTooltipRect };
UIBase.instance = new UIBase { TooltipSystem = system };
Check(ItemTooltipReader.TryReadForItem(item, container.gameObject, out var full) &&
    full == "Espada del fuego. Raro. Espada de una mano. +15 daño cuerpo a cuerpo. +8 fuerza. Requiere nivel 12",
    "Selected item reads localized name, rarity/type, primary stats and requirements in order");
Check(!full.Contains("obsolete"), "Equipped comparison root never mixes with selected item stats");
var wrongContainer = new GameObject();
Check(!ItemTooltipReader.TryReadForItem(item, wrongContainer, out _), "Background container cannot claim matching item tooltip");
parameters._Item_k__BackingField = new ItemDataUnpacked { Id = "stale" };
Check(!ItemTooltipReader.TryReadForItem(item, container.gameObject, out _), "Pooled old tooltip with same opener but different item is rejected");
parameters._Item_k__BackingField = item;
manager.activeInitializationStage = TooltipItemManager.TooltipInitializationStage.SetContent;
Check(!ItemTooltipReader.TryReadForItem(item, container.gameObject, out _) && ItemTooltipReader.IsPreparingForItem(item, container.gameObject),
    "Partially initialized content is never read");
manager.activeInitializationStage = TooltipItemManager.TooltipInitializationStage.Completed;
tooltip.contentVisible = false;
Check(!ItemTooltipReader.TryReadForItem(item, container.gameObject, out _), "Hidden completed tooltip is not read");
tooltip.contentVisible = true;
parameters._SlotType_k__BackingField = TooltipItemManager.SlotType.GROUND;
Check(!ItemTooltipReader.TryReadForItem(item, container.gameObject, out _), "Ground tooltip never becomes inventory detail");
Check(ItemTooltipReader.TryReadForOpener(opener, TooltipItemManager.SlotType.GROUND, out _, item), "Ground reader can reuse exact opener and item identity");
Check(!ItemTooltipReader.TryReadForOpener(new GameObject(), TooltipItemManager.SlotType.GROUND, out _, item), "Different ground opener is rejected");
Check(!ItemTooltipReader.TryReadForOpener(opener, TooltipItemManager.SlotType.GROUND, out _, new ItemData { Id = "different drop" }),
    "Reused ground opener with different data is rejected");
parameters._SlotType_k__BackingField = TooltipItemManager.SlotType.INVENTORY;

NavigationManager.currentlySelected = container;
Check(InventoryAccessibility.HasActiveContent && InventoryAccessibility.TryHandleFocus(opener), "Actual focused inventory container owns its icons");
Check(!InventoryAccessibility.TryHandleFocus(wrongContainer), "Unrelated controls retain ordinary focus reading");
manager.activeInitializationStage = TooltipItemManager.TooltipInitializationStage.SetContent;
InventoryAccessibility.Update();
Check(InventoryAccessibility.HasPendingContent && ScreenReader.Messages.Count == 0,
    "Matching live tooltip initialization reserves title priority over action hints");
manager.activeInitializationStage = TooltipItemManager.TooltipInitializationStage.Completed;
InventoryAccessibility.Update();
Check(!InventoryAccessibility.HasPendingContent && ScreenReader.Messages.Count == 1, "Item identity settles independently of late tooltip rendering");
InventoryAccessibility.Update();
Check(ScreenReader.Messages.Single() == "Espada. Espada de una mano" && !InventoryAccessibility.HasPendingContent,
    "Focus reads the item name and type without tooltip stats");
UpdateTwice();
Check(ScreenReader.Messages.Count == 1, "Steady focus cannot spam inventory stats");
tooltip.content.Text = "+20 daño cuerpo a cuerpo. +8 fuerza";
UpdateTwice();
Check(ScreenReader.Messages.Count == 1, "Changed visible tooltip stats stay silent until requested");
container.currentlySelectedItem = null;
UpdateTwice();
Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last() == "Empty slot", "Empty selection never reads stale open item tooltip");
container.currentlySelectedItem = entry;
manager.active = false;
UpdateTwice();
Check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages.Last() == "Espada. Espada de una mano",
    "Unavailable tooltip uses actual localized item identity without guessing rarity");
ScreenContentAnnouncer.HasActiveContent = true;
UpdateTwice();
Check(!InventoryAccessibility.HasActiveContent && ScreenReader.Messages.Count == 3, "Foreground tutorial blocks inventory background speech");
ScreenContentAnnouncer.HasActiveContent = false;
NavigationManager.currentlySelected = null;
UpdateTwice();
Check(!InventoryAccessibility.HasActiveContent && ScreenReader.Messages.Count == 3, "Leaving inventory discards retained selection");
NavigationManager.currentlySelected = container;
container.WasCollected = true;
UpdateTwice();
Check(!InventoryAccessibility.HasActiveContent && LeAccess.Plugin.Log.Warnings.Count == 0, "Collected container is rejected before Unity null access");
container.WasCollected = false;
manager.WasCollected = true;
Check(!ItemTooltipReader.TryReadForItem(item, container.gameObject, out _), "Collected tooltip manager is rejected safely");
InventoryAccessibility.Reset();

// Steam's primary V2 widget is selected by TooltipSystem, independently of its comparison widget.
manager.WasCollected = false;
manager.active = true;
container.currentlySelectedItem = entry;
ScreenReader.Messages.Clear();
var modern = new LE.UI.Tooltips.ItemTooltip();
var scrollRoot = new GameObject();
scrollRoot.transform.parent = modern.transform;
modern._tooltipTemplate = new LE.UI.Tooltips.TooltipTemplate
{ _scrollRect = new UnityEngine.UI.ScrollRect { content = scrollRoot.transform } };
TMPro.TMP_Text Label(string text, UnityEngine.Transform parent)
{
    var label = new TMPro.TMP_Text { text = text };
    label.transform.parent = parent;
    return label;
}
modern._name = Label("Espada del fuego", modern.transform);
modern._primaryType = Label("Espada de una mano", modern.transform);
modern._secondaryType = Label("Raro", modern.transform);
var prefix = Label("+20 daño cuerpo a cuerpo", scrollRoot.transform);
prefix.canvasRenderer.cull = true;
modern._prefixesSection = prefix;
var suffix = Label("+8 fuerza", scrollRoot.transform);
modern._suffixesSection = suffix;
modern._requirements = Label("Requiere nivel 12", modern.transform);
modern._combinedDifferencesSection = Label("Comparación: -50 daño", modern.transform);
modern._prompts = Label("Más información. Equipar. Soltar", modern.transform);
modern._info = new LE.UI.Tooltips.ItemTooltipInfo { _Item_k__BackingField = item };
system._itemTooltip = modern;
system._activeItemTooltipParameters = parameters;
system._activeItemTooltipInfo = modern._info;
system.ActiveItemTooltipItem = item;
system.ActiveItemTooltipRect = modern.RectTransform;
Check(ItemTooltipReader.TryReadForItem(item, container.gameObject, out var modernText) &&
    modernText == "Espada del fuego. Espada de una mano. Raro. +20 daño cuerpo a cuerpo. +8 fuerza. Requiere nivel 12",
    "V2 reads typed primary sections including expanded stats clipped by its own scroll viewport");
Check(!modernText.Contains("Comparación") && !modernText.Contains("Equipar"),
    "Primary inventory details exclude comparison deltas and separately owned action prompts");
suffix.gameObject.Visible = false;
Check(ItemTooltipReader.TryReadForItem(item, container.gameObject, out var filtered) && !filtered.Contains("+8"),
    "Inactive tooltip sections stay excluded even inside scroll content");
suffix.gameObject.Visible = true;
suffix.alpha = 0;
Check(ItemTooltipReader.TryReadForItem(item, container.gameObject, out filtered) && !filtered.Contains("+8"),
    "Transparent tooltip text is not disclosed");
suffix.alpha = 1;
suffix.transform.parent = modern.transform;
suffix.canvasRenderer.cull = true;
Check(ItemTooltipReader.TryReadForItem(item, container.gameObject, out filtered) && !filtered.Contains("+8"),
    "Renderer culling is ignored only inside the explicit tooltip scroll content");
suffix.transform.parent = scrollRoot.transform;
suffix.canvasRenderer.cull = false;
system._activeItemTooltipInfo = new LE.UI.Tooltips.ItemTooltipInfo { _Item_k__BackingField = item };
Check(!ItemTooltipReader.TryReadForItem(item, container.gameObject, out _),
    "Stale V2 rendered info cannot fall through to a still-visible old legacy tooltip");
system._activeItemTooltipInfo = modern._info;
system.ActiveItemTooltipItem = new ItemDataUnpacked { Id = "old selection" };
Check(!ItemTooltipReader.TryReadForItem(item, container.gameObject, out _), "Global active item must match the rendered V2 data");
system.ActiveItemTooltipItem = item;
parameters._Opener_k__BackingField = wrongContainer;
Check(!ItemTooltipReader.TryReadForItem(item, container.gameObject, out _), "V2 opener must belong to the focused inventory container");
parameters._Opener_k__BackingField = opener;
parameters._Item_k__BackingField = new ItemDataUnpacked { Id = "pooled old item" };
Check(!ItemTooltipReader.TryReadForItem(item, container.gameObject, out _), "V2 current parameters cannot refer to another item");
parameters._Item_k__BackingField = item;
modern.gameObject.Visible = false;
Check(!ItemTooltipReader.TryReadForItem(item, container.gameObject, out _), "Hidden active V2 widget never reuses visible legacy leftovers");
modern.gameObject.Visible = true;
system.IsItemTooltipReady = false;
Check(ItemTooltipReader.IsPreparingForItem(item, container.gameObject) &&
    !ItemTooltipReader.TryReadForItem(item, container.gameObject, out _), "Partially prepared V2 content waits for the authoritative readiness flag");
UpdateTwice();
Check(ScreenReader.Messages.Single() == "Espada. Espada de una mano" && !InventoryAccessibility.HasPendingContent,
    "Stalled tooltip still reads actual item identity and releases action-hint priority");
UpdateTwice();
Check(ScreenReader.Messages.Count == 1, "Stalled preparation does not repeat the fallback item name");
system.IsItemTooltipReady = true;
UpdateTwice();
Check(ScreenReader.Messages.Count == 1, "Late V2 tooltip cannot add unsolicited details");
InventoryAccessibility.Update(readDetails: true);
Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last() == modernText,
    "Ready V2 item details are read on the manual request");
prefix.text = "+25 daño cuerpo a cuerpo";
UpdateTwice();
Check(ScreenReader.Messages.Count == 2, "Changed primary V2 stats remain silent automatically");
LeAccess.Accessibility.ControllerMenus.ControllerMenuAccessibility.HasActiveContent = true;
UpdateTwice();
Check(!InventoryAccessibility.HasActiveContent && ScreenReader.Messages.Count == 2,
    "Controller wheel owns the foreground even if inventory navigation remains cached");
LeAccess.Accessibility.ControllerMenus.ControllerMenuAccessibility.HasActiveContent = false;
system.WasCollected = true;
Check(!ItemTooltipReader.TryReadForItem(item, container.gameObject, out _), "Collected TooltipSystem is rejected before native access");
system.WasCollected = false;
UIBase.instance.WasCollected = true;
Check(!ItemTooltipReader.TryReadForItem(item, container.gameObject, out _), "Collected UIBase is rejected before reading TooltipSystem");
UIBase.instance.WasCollected = false;
modern._info.WasCollected = true;
Check(!ItemTooltipReader.TryReadForItem(item, container.gameObject, out _), "Collected V2 info is rejected before item identity reads");
modern._info.WasCollected = false;
InventoryAccessibility.Reset();
LeAccess.I18n.ModText.SetLanguage("es");
container.currentlySelectedItem = null;
NavigationManager.currentlySelected = container;
ScreenReader.Messages.Clear();
UpdateTwice();
Check(ScreenReader.Messages.Single() == "Espacio vacío", "Empty inventory slots follow the mod language");
LeAccess.I18n.ModText.SetLanguage("en");
UpdateTwice();
Check(ScreenReader.Messages.Last() == "Empty slot", "A language change updates an unchanged empty slot");
InventoryAccessibility.Reset();
var actionPrompt = new LE.UI.PanelSystem.PanelActionUIPrompt();
actionPrompt.transform.parent = modern._prompts.transform;
actionPrompt._label = new TMPro.TMP_Text { text = "Equipar" };
actionPrompt._label.transform.parent = actionPrompt.transform;
actionPrompt._panelAction = new() { ActionName = "equip-selected" };
LeAccess.Accessibility.Announcements.InputBindingText.Bindings["equip-selected"] = "A";
Check(LeAccess.Accessibility.Inventory.InventoryActionReader.Read(modern).Single() == "A, Equipar",
    "Modern current-item action pairs its displayed localized verb with authoritative binding");
LeAccess.Accessibility.Announcements.InputBindingText.Bindings["equip-selected"] = "Y";
Check(LeAccess.Accessibility.Inventory.InventoryActionReader.Read(modern).Single() == "Y, Equipar",
    "Inventory action bindings follow remaps without fixed Xbox assignments");
actionPrompt._panelAction.GamepadButtonNamesOverride = new() { Values = new[] { "LT", "X" } };
Check(LeAccess.Accessibility.Inventory.InventoryActionReader.Read(modern).Single() == "LT + X, Equipar",
    "Explicit gamepad overrides take priority over the ordinary action mapping");
actionPrompt._panelAction.MouseAndKeyboardButtonNamesOverride = new() { Values = new[] { "Shift", "Click" } };
EpochInputManager.IsControllerActive = false;
Check(LeAccess.Accessibility.Inventory.InventoryActionReader.Read(modern).Single() == "Shift + Click, Equipar",
    "Device changes select the actual keyboard override rather than retained controller hints");
EpochInputManager.IsControllerActive = true;
actionPrompt._panelAction.IsHoldAction = true;
Check(LeAccess.Accessibility.Inventory.InventoryActionReader.Read(modern).Single() == "Hold LT + X, Equipar",
    "Inventory hold actions retain their native hold semantics");
actionPrompt._panelAction.CanUse = false;
Check(!LeAccess.Accessibility.Inventory.InventoryActionReader.Read(modern).Any(), "Unavailable item actions do not claim an executable button");
actionPrompt._panelAction.CanUse = true;
actionPrompt._cg = new() { alpha = 0 };
Check(!LeAccess.Accessibility.Inventory.InventoryActionReader.Read(modern).Any(), "Hidden pooled prompts are excluded");
actionPrompt._cg.alpha = 1;
actionPrompt._panelAction.WasCollected = true;
Check(!LeAccess.Accessibility.Inventory.InventoryActionReader.Read(modern).Any(), "Collected native action data is excluded before binding queries");
actionPrompt._panelAction.WasCollected = false;
actionPrompt._label.alpha = 0;
Check(!LeAccess.Accessibility.Inventory.InventoryActionReader.Read(modern).Any(), "Transparent action labels do not leak stale hints");
actionPrompt._label.alpha = 1;
actionPrompt._panelAction.GamepadButtonNamesOverride = null;
actionPrompt._panelAction.IsHoldAction = false;
Check(ItemTooltipReader.TryReadForItem(item, container.gameObject, out var actionDetails) && actionDetails.Contains("Y, Equipar"),
    "Matched modern item reading includes its visible action button names");
system._activeItemTooltipInfo = new() { _Item_k__BackingField = item };
Check(!ItemTooltipReader.TryReadForItem(item, container.gameObject, out _), "Mismatched pooled tooltip info cannot contribute actions for another item");
system._activeItemTooltipInfo = modern._info;
container.currentlySelectedItem = entry;
NavigationManager.currentlySelected = container;
InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
UpdateTwice();
InventoryAccessibility.Update(readDetails: true);
Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last() == actionDetails,
    "Manual stick gesture rereads the complete current item tooltip and its actions");
UpdateTwice();
Check(ScreenReader.Messages.Count == 2, "Manual item read cannot cause a second automatic replay");
container.currentlySelectedItem = null;
InventoryAccessibility.Update(readDetails: true);
Check(ScreenReader.Messages.Last() == "Empty slot", "Manual read on empty slot cannot replay previous equipment");
int beforeBlockedRead = ScreenReader.Messages.Count;
ScreenContentAnnouncer.HasActiveContent = true;
InventoryAccessibility.Update(readDetails: true);
Check(ScreenReader.Messages.Count == beforeBlockedRead, "Manual inventory details respect foreground ownership");
ScreenContentAnnouncer.HasActiveContent = false;
container.currentlySelectedItem = entry;
system.IsItemTooltipReady = false;
ScreenReader.Messages.Clear(); InventoryAccessibility.Reset();
ItemTooltipModelReader.Text = "Espada. Rare. +20 daño cuerpo a cuerpo. Requires level 12";
UpdateTwice();
Check(ItemTooltipModelReader.Reads == 0, "Automatic inventory reading never builds hidden tooltip models");
InventoryAccessibility.Update(readDetails: true);
Check(ScreenReader.Messages.Last() == ItemTooltipModelReader.Text && ItemTooltipModelReader.LastItem == item &&
    ItemTooltipModelReader.LastContainer == container, "Manual details use the selected item model when visual tooltip is missing");
int afterManualModel = ScreenReader.Messages.Count;
UpdateTwice();
Check(ScreenReader.Messages.Count == afterManualModel, "Automatic title fallback cannot interrupt manually read full properties");
ItemTooltipModelReader.Text = null;
InventoryAccessibility.Update(readDetails: true);
Check(ScreenReader.Messages.Last().Contains("Item details are unavailable"), "Unavailable manual data gives clear feedback instead of only repeating a name");
InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
InventoryAccessibility.Update(readDetails: true); UpdateTwice();
Check(ScreenReader.Messages.Count == 1, "Immediate manual inspection suppresses pending automatic identity speech");
container.identifier = ContainerID.EQ_WEAPON; UpdateTwice();
Check(ScreenReader.Messages.Last().EndsWith("Equipped"), "An actual equipment container identifies the selected item as equipped");
container.identifier = ContainerID.INVENTORY; UpdateTwice();
Check(!ScreenReader.Messages.Last().Contains("Equipped"), "Returning item to inventory clears equipped status");
InventoryPromptTests.Run(Check);
InventoryActionTests.Run(Check);
Console.WriteLine($"Inventory tests passed: {checks} checks.");
