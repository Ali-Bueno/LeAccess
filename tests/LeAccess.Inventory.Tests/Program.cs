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
StoredSelectionTests.Run(Check);
Check(!state.Observe("a", "Old item", true, out _) && state.HasPendingContent, "First item snapshot settles");
Check(!state.Observe("b", "New item", true, out _), "Rapid navigation discards unspoken previous item");
Check(state.Observe("b", "New item", true, out var speech) && speech == "New item", "Latest item speaks once stable");
Check(!state.Observe("b", "New item", true, out _) && !state.HasPendingContent, "Unchanged item never repeats");
Check(!state.Observe("b", "Updated stats", false, out _) && state.HasPendingContent, "Stat update waits while higher-priority speech owns frame");
Check(state.Observe("b", "Updated stats", true, out speech) && speech == "Updated stats", "Same item changed stats are announced");
Check(!state.Observe("empty", Text("inventory.empty_slot"), true, out _), "Moving off item clears old candidate");
Check(state.Observe("empty", Text("inventory.empty_slot"), true, out speech) && speech == Text("inventory.empty_slot"), "Empty slot cannot repeat the previous item");
Check(!state.Observe("missing", null, true, out _) && !state.HasPendingContent, "Missing item text cannot hold pending speech forever");
var quiet = new InventoryAnnouncementState();
quiet.Observe("ring", "Anillo. 3 de 5", true, out _, identity: "Anillo");
Check(quiet.Observe("ring", "Anillo. 3 de 5", true, out speech, identity: "Anillo") && speech == "Anillo. 3 de 5", "Item speaks once with its position");
quiet.Observe("ring", "Anillo. 3 de 4", true, out _, identity: "Anillo");
Check(!quiet.Observe("ring", "Anillo. 3 de 4", true, out _, identity: "Anillo") && !quiet.HasPendingContent,
    "A drop elsewhere changing only the count never re-reads the focused item");
quiet.Observe("ring", "Anillo forjado. 3 de 4", true, out _, identity: "Anillo forjado");
Check(quiet.Observe("ring", "Anillo forjado. 3 de 4", true, out speech, identity: "Anillo forjado") && speech == "Anillo forjado. 3 de 4",
    "A changed name on the same item is still spoken");
quiet.Reset();
quiet.Observe("ring", "Anillo forjado. 3 de 4", true, out _, identity: "Anillo forjado");
Check(quiet.Observe("ring", "Anillo forjado. 3 de 4", true, out _, identity: "Anillo forjado"), "A deliberate repeat after reset still speaks");

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
    full == ScreenContentTracker.Combine(new[] { tooltip.nameText.text, tooltip.typeText.text, tooltip.content.Text, tooltip.requirementsFooter.Text }),
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
Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last() == Text("inventory.empty_slot"), "Empty selection never reads stale open item tooltip");
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
TMPro.TMP_Text MakeLabel(string text, UnityEngine.Transform parent)
{
    var label = new TMPro.TMP_Text { text = text };
    label.transform.parent = parent;
    return label;
}
modern._name = MakeLabel("Espada del fuego", modern.transform);
modern._primaryType = MakeLabel("Espada de una mano", modern.transform);
modern._secondaryType = MakeLabel("Raro", modern.transform);
var prefix = MakeLabel("+20 daño cuerpo a cuerpo", scrollRoot.transform);
prefix.canvasRenderer.cull = true;
modern._prefixesSection = prefix;
var suffix = MakeLabel("+8 fuerza", scrollRoot.transform);
modern._suffixesSection = suffix;
modern._requirements = MakeLabel("Requiere nivel 12", modern.transform);
var differences = new LE.UI.Tooltips.ItemTooltipModifiersSection();
differences.transform.parent = modern.transform;
var differenceLabel = MakeLabel("Comparación: -50 daño", differences.transform);
modern._combinedDifferencesSection = differences;
var promptLabel = MakeLabel("Más información. Equipar. Soltar", modern.transform);
modern._prompts = promptLabel;
modern._info = new LE.UI.Tooltips.ItemTooltipInfo { _Item_k__BackingField = item };
system._itemTooltip = modern;
system._activeItemTooltipParameters = parameters;
system._activeItemTooltipInfo = modern._info;
system.ActiveItemTooltipItem = item;
system.ActiveItemTooltipRect = modern.RectTransform;
Check(ItemTooltipReader.TryReadForItem(item, container.gameObject, out var modernText) &&
    modernText == ScreenContentTracker.Combine(new[] { modern._name.text, modern._primaryType.text, modern._secondaryType.text, prefix.text, suffix.text, modern._requirements.text }),
    "V2 reads typed primary sections including expanded stats clipped by its own scroll viewport");
Check(!modernText.Contains(differenceLabel.text) && !modernText.Contains(promptLabel.text),
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

LE.UI.Tooltips.ItemTooltipModifier Modifier(string text, UnityEngine.Transform parent,
    LE.UI.Tooltips.ItemTooltipAssetList.ItemTooltipModifierIconSprite icon)
{
    var modifier = new LE.UI.Tooltips.ItemTooltipModifier { Info = new() { IconSprite = icon } };
    modifier.transform.parent = parent;
    modifier._modifierText = MakeLabel(text, modifier.transform);
    return modifier;
}
var compared = new LE.UI.Tooltips.ItemTooltip
{ _info = new LE.UI.Tooltips.ItemTooltipInfo { _Item_k__BackingField = new ItemDataUnpacked { Id = "equipped" } } };
compared._name = MakeLabel("Espada vieja", compared.transform);
Modifier("+5 daño", differences.transform, LE.UI.Tooltips.ItemTooltipAssetList.ItemTooltipModifierIconSprite.Compare_CombinedIncrease);
Modifier("-3 fuerza", differences.transform, LE.UI.Tooltips.ItemTooltipAssetList.ItemTooltipModifierIconSprite.Compare_CombinedDecrease);
var raised = Modifier("+8 fuerza", modern.transform, LE.UI.Tooltips.ItemTooltipAssetList.ItemTooltipModifierIconSprite.Plain);
var lost = Modifier("+10 vida", compared.transform, LE.UI.Tooltips.ItemTooltipAssetList.ItemTooltipModifierIconSprite.Plain);
var same = Modifier("+1 nivel", modern.transform, LE.UI.Tooltips.ItemTooltipAssetList.ItemTooltipModifierIconSprite.Plain);
ItemComparisonIcons.Set(raised, LE.UI.Tooltips.ItemTooltipAssetList.ItemTooltipModifierIconSprite.Compare_Increase);
ItemComparisonIcons.Set(lost, LE.UI.Tooltips.ItemTooltipAssetList.ItemTooltipModifierIconSprite.Compare_Removed);
ItemComparisonIcons.Set(same, LE.UI.Tooltips.ItemTooltipAssetList.ItemTooltipModifierIconSprite.Compare_NoChange);
system._itemTooltipComparison = compared;
system._activeComparisonItemTooltipInfo = compared._info;
string Text(string key, params object[] args) => LeAccess.I18n.ModText.Format(key, args);
// Holding L3 (or Ctrl) switches the game's tooltip to its comparison mode; releasing it returns to the default.
void Mode(TooltipMode mode) => modern._info._TooltipMode_k__BackingField = mode;
Mode(TooltipMode.CompareDefault);
string brief = ScreenContentTracker.Combine(new[] { item.FullName, item.LocalisedBaseTypeName });
string comparedWith = Text("item.comparison_with", compared._name.text);
string comparisonSpeech = ScreenContentTracker.Combine(new[] { comparedWith,
    Text("item.compare_differences", string.Join("; ", Text("item.compare_increase", "+5 daño"), Text("item.compare_decrease", "-3 fuerza"))),
    Text("item.compare_changes", string.Join("; ", Text("item.compare_increase", raised._modifierText.text),
        Text("item.compare_removed", lost._modifierText.text))) });
UpdateTwice();
Check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages.Last() == comparisonSpeech,
    "Holding compare reads the equipped item, combined differences and changed lines with their visual icons: " + ScreenReader.Messages.Last());
UpdateTwice();
Check(ScreenReader.Messages.Count == 3, "A held comparison speaks once");
Mode(TooltipMode.DefaultInfo); UpdateTwice(); Mode(TooltipMode.CompareDefault); UpdateTwice();
Check(ScreenReader.Messages.Count == 4 && ScreenReader.Messages.Last() == comparisonSpeech, "Pressing compare again reads it again");
ItemComparisonIcons.Clear(raised);
Mode(TooltipMode.DefaultInfo); UpdateTwice(); Mode(TooltipMode.CompareDefault); UpdateTwice();
Check(!ScreenReader.Messages.Last().Contains(raised._modifierText.text), "Fresh line data drops a stale comparison icon");
InventoryAccessibility.Reset(); ScreenReader.Messages.Clear(); UpdateTwice();
Check(ScreenReader.Messages.Single().StartsWith(ScreenContentTracker.Combine(new[] { brief, comparedWith })),
    "Reaching a new item with compare held names the item before its comparison");
system._activeComparisonItemTooltipInfo = new LE.UI.Tooltips.ItemTooltipInfo();
InventoryAccessibility.Reset(); ScreenReader.Messages.Clear(); UpdateTwice();
Check(ScreenReader.Messages.Single() == ScreenContentTracker.Combine(new[] { brief, Text("item.compare_nothing") }),
    "A stale comparison widget is never read; the held compare says nothing is equipped to compare: " + ScreenReader.Messages.Single());
// The comparison state is logged once per change, so a live test shows why a held key stayed silent.
string StateLines() => string.Join("|", LeAccess.Plugin.Log.Information.Where(x => x.StartsWith("[Inventory comparison state]")));
Check(StateLines().Contains("v2 mode=CompareDefault"), "The comparison state names the tooltip mode: " + StateLines());
LeAccess.Plugin.Log.Information.Clear();
UpdateTwice(); UpdateTwice();
Check(StateLines() == "", "An unchanged comparison state is not logged again");
Mode(TooltipMode.DefaultInfo); UpdateTwice(); UpdateTwice();
Check(LeAccess.Plugin.Log.Information.Count(x => x.StartsWith("[Inventory comparison state]")) == 1 && StateLines().Contains("mode=DefaultInfo"),
    "A changed comparison state is logged once: " + StateLines());
Mode(TooltipMode.CompareDefault);
// Holding R3 (or Alt) shows each line's explanation; holding both shows the value ranges.
system._activeComparisonItemTooltipInfo = compared._info;
raised._explanationText = MakeLabel("Aumenta la fuerza", raised.transform);
raised._rangeText = MakeLabel("5 a 10", raised.transform);
InventoryAccessibility.Reset(); ScreenReader.Messages.Clear(); UpdateTwice();
Mode(TooltipMode.Explanation); UpdateTwice();
Check(ScreenReader.Messages.Last() == Text("item.explanations", Text("item.explained_line", "+8 fuerza", "Aumenta la fuerza")),
    "Holding the explanation button reads each explained line: " + ScreenReader.Messages.Last());
Mode(TooltipMode.DefaultModRanges); UpdateTwice();
Check(ScreenReader.Messages.Last() == Text("item.ranges", Text("item.ranged_line", "+8 fuerza", "5 a 10")),
    "Holding both buttons for ranges reads each line's range");
int beforeRanges = ScreenReader.Messages.Count;
UpdateTwice();
Check(ScreenReader.Messages.Count == beforeRanges, "A held mode speaks once");
Mode(TooltipMode.CompareModRanges); UpdateTwice();
Check(ScreenReader.Messages.Last().StartsWith(comparedWith) &&
    ScreenReader.Messages.Last().EndsWith(Text("item.ranges", Text("item.ranged_line", "+8 fuerza", "5 a 10"))),
    "Comparison with ranges reads the comparison, then the ranges");
modern._info = new LE.UI.Tooltips.ItemTooltipInfo { _Item_k__BackingField = item, _TooltipMode_k__BackingField = TooltipMode.CompareDefault };
UpdateTwice();
Check(ScreenReader.Messages.Last().EndsWith(Text("item.ranges", Text("item.ranged_line", "+8 fuerza", "5 a 10"))),
    "A tooltip that is not the game's active one is never read");
modern._info = system._activeItemTooltipInfo;
Mode(TooltipMode.DefaultInfo);
raised._explanationText = raised._rangeText = null;
system._itemTooltipComparison = null; system._activeComparisonItemTooltipInfo = null;
ItemComparisonIcons.Reset();
InventoryAccessibility.Reset(); ScreenReader.Messages.Clear(); UpdateTwice();
InventoryAccessibility.Update(readDetails: true); UpdateTwice();
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
Check(ScreenReader.Messages.Single() == Text("inventory.empty_slot"), "Empty inventory slots follow the mod language");
LeAccess.I18n.ModText.SetLanguage("en");
UpdateTwice();
Check(ScreenReader.Messages.Last() == Text("inventory.empty_slot"), "A language change updates an unchanged empty slot");
InventoryAccessibility.Reset();
var actionPrompt = new LE.UI.PanelSystem.PanelActionUIPrompt();
actionPrompt.transform.parent = modern._prompts.transform;
actionPrompt._label = new TMPro.TMP_Text { text = "Equipar" };
string verb = actionPrompt._label.text;
actionPrompt._label.transform.parent = actionPrompt.transform;
actionPrompt._panelAction = new() { ActionName = "equip-selected" };
LeAccess.Accessibility.Announcements.InputBindingText.Bindings["equip-selected"] = "A";
Check(LeAccess.Accessibility.Inventory.InventoryActionReader.Read(modern).Single() == "A, " + verb,
    "Modern current-item action pairs its displayed localized verb with authoritative binding");
LeAccess.Accessibility.Announcements.InputBindingText.Bindings["equip-selected"] = "Y";
Check(LeAccess.Accessibility.Inventory.InventoryActionReader.Read(modern).Single() == "Y, " + verb,
    "Inventory action bindings follow remaps without fixed Xbox assignments");
actionPrompt._panelAction.GamepadButtonNamesOverride = new() { Values = new[] { "LT", "X" } };
Check(LeAccess.Accessibility.Inventory.InventoryActionReader.Read(modern).Single() == "LT + X, " + verb,
    "Explicit gamepad overrides take priority over the ordinary action mapping");
LeAccess.Accessibility.Announcements.InputBindingText.Localized["X"] = "Equis";
Check(LeAccess.Accessibility.Inventory.InventoryActionReader.Read(modern).Single() == "LT + Equis, " + verb,
    "Override button names pass through catalogue localization");
LeAccess.Accessibility.Announcements.InputBindingText.Localized.Clear();
actionPrompt._panelAction.MouseAndKeyboardButtonNamesOverride = new() { Values = new[] { "Shift", "Click" } };
EpochInputManager.IsControllerActive = false;
Check(LeAccess.Accessibility.Inventory.InventoryActionReader.Read(modern).Single() == "Shift + Click, " + verb,
    "Device changes select the actual keyboard override rather than retained controller hints");
EpochInputManager.IsControllerActive = true;
actionPrompt._panelAction.IsHoldAction = true;
Check(LeAccess.Accessibility.Inventory.InventoryActionReader.Read(modern).Single() == Text("input.hold", "LT + X") + ", " + verb,
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
Check(ItemTooltipReader.TryReadForItem(item, container.gameObject, out var actionDetails) && actionDetails.Contains("Y, " + verb),
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
Check(ScreenReader.Messages.Last() == Text("inventory.empty_slot"), "Manual read on empty slot cannot replay previous equipment");
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
Check(ScreenReader.Messages.Last().Contains(Text("inventory.details_unavailable")), "Unavailable manual data gives clear feedback instead of only repeating a name");
InventoryAccessibility.Reset(); ScreenReader.Messages.Clear();
InventoryAccessibility.Update(readDetails: true); UpdateTwice();
Check(ScreenReader.Messages.Count == 1, "Immediate manual inspection suppresses pending automatic identity speech");
container.identifier = ContainerID.EQ_WEAPON;
container.container = new() { Id = ContainerID.EQ_WEAPON, Type = ContainerType.EQUIPPED, Entry = entry };
UpdateTwice();
Check(ScreenReader.Messages.Last().EndsWith(Text("inventory.equipped")), "An actual equipment container identifies the selected item as equipped");
container.identifier = ContainerID.INVENTORY; UpdateTwice();
Check(!ScreenReader.Messages.Last().Contains(Text("inventory.equipped")), "Returning item to inventory clears equipped status");
InventoryPromptTests.Run(Check);
InventoryActionTests.Run(Check);
EquippedItemTests.Run(Check);
SpeechAcceptanceTests.Run(Check);
DropFocusTests.Run(Check);
PointerHoverTests.Run(Check);
NavigationTests.Run(Check);
IdolTests.Run(Check);
GoldNoticeTests.Run(Check);
ComparisonModeTests.Run(Check);
Console.WriteLine($"Inventory tests passed: {checks} checks.");
