using LeAccess.Accessibility.Items;
using LE.UI.Tooltips;

int checks = 0;
void Check(bool condition, string name) { checks++; if (!condition) throw new Exception(name); }
var item = new ItemDataUnpacked { LoreText = "Una espada forjada en otra era.", FactionRequirement = "Requiere rango 3 del Círculo de la Fortuna" };
var container = new ItemContainerUI { currentlySelectedItem = new ItemContainerEntry { data = item } };
container.Container.Entry = container.currentlySelectedItem;
NavigationManager.currentlySelected = container;
var info = new ItemTooltipInfo
{
    Name = "<b>Espada de hierro</b>", PrimaryType = "Espada", SecondaryType = "Raro",
    Description = "Golpea a enemigos cercanos.", Requirements = "Requiere nivel 7",
    IsWeapon = true, Range = "Alcance: 2 metros", Speed = "Ataques por segundo: 1,2",
    PotentialInfo = new ItemTooltipPotentialInfo { ShouldShow = true, Text = "Potencial de forja: 20" }
};
info.Implicits.Add(new ItemTooltipModifierInfo { Text = "+12 de daño físico" });
var prefix = new ItemTooltipAffixInfo { ShowTier = true, Tier = "Nivel 3",
    Modifier1 = new ItemTooltipModifierInfo { Text = "+5 de fuerza" },
    Modifier2 = new ItemTooltipModifierInfo { Text = "+10 de vida" } };
info.Prefixes.Add(prefix);
info.Suffixes.Add(new ItemTooltipAffixInfo { Modifier1 = new ItemTooltipModifierInfo { Text = "+8% de resistencia" } });
info.SealedAffixes.Add(new ItemTooltipAffixInfo { Modifier1 = new ItemTooltipModifierInfo { Text = "Afijo sellado" } });
info.UniqueModifiers.Add(new ItemTooltipModifierInfo { Text = "Efecto único" });
info.SetName = "Conjunto del guardián";
info.SetItems.Add(new ItemTooltipSetItemInfo { Text = "Botas del guardián" });
var bonus = new ItemTooltipSetBonusInfo { Requirement = "2 piezas" };
bonus.Modifiers.Add(new ItemTooltipModifierInfo { Text = "+15 de armadura" });
info.SetBonuses.Add(bonus);
info.AddsAffixes.Add(new ItemTooltipAffixInfo { Modifier1 = new ItemTooltipModifierInfo { Text = "Añade resistencia al frío" } });
info.CanRollOn.Add(new ItemTooltipEquipmentTypeLineItemInfo { Text = "Puede aparecer en cascos" });
ItemTooltipContentBuilder.Result = info;
Check(ItemTooltipModelReader.TryRead(item, container, out string text), "native builder usable with no visual tooltip or UI system");
foreach (string part in new[] { "Espada de hierro", "Raro", "Golpea", "Requiere nivel", "Alcance", "Ataques por segundo",
    "Potencial de forja", "+12 de daño físico", "Nivel 3", "+5 de fuerza", "+10 de vida", "+8% de resistencia",
    "Afijo sellado", "Efecto único", "Conjunto", "Botas", "2 piezas", "+15 de armadura", "Añade resistencia", "Puede aparecer",
    "forjada en otra era", "rango 3 del Círculo de la Fortuna" })
    Check(text.Contains(part), "game-formatted section " + part);
Check(!text.Contains('<'), "native formatting removed");
Check(info.SlotType == TooltipItemManager.SlotType.INVENTORY && info.TooltipMode == TooltipMode.DefaultInfo,
    "never opened inventory deliberately uses default details");
foreach (var pair in new[] { (ContainerType.EQUIPPED, TooltipItemManager.SlotType.EQUIPPED), (ContainerType.STASH, TooltipItemManager.SlotType.STASH) })
{
    container.Container.Type = pair.Item1;
    Check(ItemTooltipModelReader.TryRead(item, container, out _) && info.SlotType == pair.Item2, "native mapping " + pair.Item1);
}
container.Container.Type = ContainerType.SHOP;
int calls = ItemTooltipContentBuilder.Calls;
Check(!ItemTooltipModelReader.TryRead(item, container, out _) && ItemTooltipContentBuilder.Calls == calls, "unknown slot never guessed");
var parameters = new TooltipItemManager.ItemTooltipParameters
{ _Item_k__BackingField = item, _Opener_k__BackingField = container.gameObject,
    _SlotType_k__BackingField = TooltipItemManager.SlotType.SHOP, _TooltipMode_k__BackingField = TooltipMode.Explanation };
UIBase.instance = new UIBase { TooltipSystem = new TooltipSystem { _activeItemTooltipParameters = parameters } };
Check(ItemTooltipModelReader.TryRead(item, container, out _) && info.SlotType == TooltipItemManager.SlotType.SHOP &&
    info.TooltipMode == TooltipMode.Explanation, "owned native parameters retain specialized slot and display mode");
parameters._Opener_k__BackingField = new UnityEngine.GameObject();
Check(!ItemTooltipModelReader.TryRead(item, container, out _), "foreign opener cannot supply vendor context");
parameters._Opener_k__BackingField = container.gameObject;
parameters._Item_k__BackingField = new ItemData();
Check(!ItemTooltipModelReader.TryRead(item, container, out _), "foreign item cannot supply vendor context");
parameters._Item_k__BackingField = item;
parameters._SlotType_k__BackingField = TooltipItemManager.SlotType.GROUND;
Check(!ItemTooltipModelReader.TryRead(item, container, out _), "ground parameters cannot masquerade as inventory");
parameters._SlotType_k__BackingField = TooltipItemManager.SlotType.UNDEFINED;
Check(!ItemTooltipModelReader.TryRead(item, container, out _), "undefined context cannot construct");
container.Container.Type = ContainerType.INVENTORY;
UIBase.instance = null;
info.IsUnidentified = true;
prefix.Modifier1.Throw = true;
Check(ItemTooltipModelReader.TryRead(item, container, out text) && !text.Contains("fuerza") && !text.Contains("daño") &&
    !text.Contains("Requiere"), "unidentified item never reveals modifiers or requirements");
info.IsUnidentified = false;
Check(!ItemTooltipModelReader.TryRead(item, container, out text) && text is null &&
    ItemTooltipModelReader.LastFailure.Contains("formatter unavailable"), "native exception returns no partial details");
prefix.Modifier1.Throw = false;
Check(ItemTooltipModelReader.TryRead(item, container, out _) && ItemTooltipModelReader.LastFailure is null, "failure clears after successful retry");
info.PotentialInfo.ShouldShow = false;
info.Implicits.WasCollected = true;
info.SetItems[0].WasCollected = true;
Check(ItemTooltipModelReader.TryRead(item, container, out text) && !text.Contains("Potencial") && !text.Contains("daño") &&
    !text.Contains("Botas"), "hidden potential and collected lists/rows excluded");
foreach (string gate in new[] { "disabled", "hidden", "foreign navigation", "collected item", "collected entry", "wrong selected item" })
{
    container.isActiveAndEnabled = gate != "disabled";
    container.gameObject.activeInHierarchy = gate != "hidden";
    NavigationManager.currentlySelected = gate == "foreign navigation" ? new ItemContainerUI() : container;
    item.WasCollected = gate == "collected item";
    container.currentlySelectedItem.WasCollected = gate == "collected entry";
    container.currentlySelectedItem.data = gate == "wrong selected item" ? new ItemData() : item;
    calls = ItemTooltipContentBuilder.Calls;
    Check(!ItemTooltipModelReader.TryRead(item, container, out _) && ItemTooltipContentBuilder.Calls == calls, "selection guard " + gate);
}
container.currentlySelectedItem.data = item;
var packed = new ItemData { Id = item.Id, Unpacked = item };
container.currentlySelectedItem.data = packed;
Check(ItemTooltipModelReader.TryRead(packed, container, out _), "packed and unpacked same native item ID accepted");
packed.Unpacked = new ItemDataUnpacked();
Check(!ItemTooltipModelReader.TryRead(packed, container, out _), "unpacked different ID rejected");
container.currentlySelectedItem.data = item;
ItemTooltipContentBuilder.WrongItem = true;
Check(!ItemTooltipModelReader.TryRead(item, container, out _), "builder must return requested item");
ItemTooltipContentBuilder.WrongItem = false;
ItemTooltipContentBuilder.WrongMode = true;
Check(!ItemTooltipModelReader.TryRead(item, container, out _), "builder must retain mode");
ItemTooltipContentBuilder.WrongMode = false;
ItemTooltipContentBuilder.WrongSlot = true;
Check(!ItemTooltipModelReader.TryRead(item, container, out _), "builder must retain slot");
ItemTooltipContentBuilder.WrongSlot = false;
ItemTooltipContentBuilder.BuildAction = () => container.currentlySelectedItem = null;
Check(!ItemTooltipModelReader.TryRead(item, container, out text) && text is null, "selection disappearing during build cannot deliver stale details");
ItemTooltipContentBuilder.BuildAction = null;
container.currentlySelectedItem = new ItemContainerEntry { data = item };
ItemTooltipContentBuilder.Result = null;
Check(!ItemTooltipModelReader.TryRead(item, container, out _), "missing native result handled");
Check(!ItemTooltipModelReader.TryReadShown(item, TooltipItemManager.SlotType.INVENTORY, out text) && text is null,
    "panel slot without native result has no details");
ItemTooltipContentBuilder.Result = info;
NavigationManager.currentlySelected = null;
Check(ItemTooltipModelReader.TryReadShown(item, TooltipItemManager.SlotType.INVENTORY, out text) && text.Contains("+5 de fuerza") &&
    info.SlotType == TooltipItemManager.SlotType.INVENTORY && info.TooltipMode == TooltipMode.DefaultInfo,
    "panel slot details need no inventory focus and use default details");
Check(ItemTooltipModelReader.TryReadShown(item, TooltipItemManager.SlotType.MORDITAS, out _) &&
    info.SlotType == TooltipItemManager.SlotType.MORDITAS, "panel slot keeps the game's slot type");
calls = ItemTooltipContentBuilder.Calls;
Check(!ItemTooltipModelReader.TryReadShown(null, TooltipItemManager.SlotType.INVENTORY, out _) &&
    ItemTooltipContentBuilder.Calls == calls, "empty panel slot builds nothing");
ItemTooltipContentBuilder.WrongItem = true;
Check(!ItemTooltipModelReader.TryReadShown(item, TooltipItemManager.SlotType.INVENTORY, out text) && text is null,
    "panel slot rejects another item");
ItemTooltipContentBuilder.WrongItem = false;
prefix.Modifier1.Throw = true;
Check(!ItemTooltipModelReader.TryReadShown(item, TooltipItemManager.SlotType.INVENTORY, out text) && text is null &&
    ItemTooltipModelReader.LastFailure is not null, "panel slot exception returns no partial details");
prefix.Modifier1.Throw = false;
NavigationManager.currentlySelected = container;
MerchantTests.Run(Check);
MerchantMenuTests.Run(Check);
EquippedModelTests.Run(Check);
Console.WriteLine($"Passed {checks} manual item tooltip model checks.");
