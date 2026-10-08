# Equipped idols (2026-10-08)

User report: "I need an accessible way to see my equipped idols; I found no way to see them." The log shows
Triangle (Equip) on an idol in the backpack working, but the idol grid was never focused. The grid was
reachable all along; nothing told the user where it was, and its cells were read as plain "Empty slot".

## Native controller route (Steam build 25764382)

RVAs from the installed `MethodAddressToToken.db` + interop, read with Capstone on `GameAssembly.dll`
(same method as [equipped-items.md](equipped-items.md)); field offsets via Cpp2IL. V = verified in the
disassembly, I = inferred.

- V Equipment (`EquippedItemsNavigable`, `rects`/`navigables[x,y]`, y=0 is the top row, 3 columns):
  `HandleNavigateInput` 0x104C060, Right past the last column (0x104C28B–0x104C309) selects
  `IDOL_ALTAR` (180) from row 0, `IDOLS_CONTAINER` (7) from row 1, `INVENTORY_CRAFTING_BUTTONS` (8)
  from rows 2+. Empty cells and further cells of the same slot are skipped (`ShouldKeepNavigating`
  0x104D290 compares `rects`). Down from the bottom row (gloves, boots, relic: ContainerIDs 6/8/12 in
  `CheckContextChange` 0x104B530) goes to the item tabs (145).
- V Backpack Up (`InventoryContainerUI.DpadContextChange` 0x13EC150): x≤1 → equipment (0,3), 2–4 → (1,3),
  5–7 → (2,3), x≥8 → first crafting button. `InventoryCraftingButtonsNavigable.HandleNavigateInput`
  0x1050660: Up on the first button → idol grid; Left → equipment (2,2); Right → (0,2); Down past the
  last → item tabs.
- V Idol grid (`IdolsContainerUI`, `IdolsItemContainer`: ContainerID `IDOLS` 29, ContainerType
  `EQUIPPED`, built at 0x1400EC2). `get_GetDefaultNavigationIndex` 0x13E6200: entry focuses the first
  `Unlocked` cell, top row first. `HandleNavigateInput` 0x13E50D0 steps one cell; `FocusSlot` 0x1425700
  skips the rest of the focused idol but visits empty, locked and blocked cells. Edges
  (`DpadContextChange` 0x13E4D00): Left → equipment (2,0) from the top two rows, else (2,1); Up → altar;
  Down → crafting buttons; Right wraps to column 0. Holding an idol: Down leaves to the backpack
  (0x13E4C00). Circle returns a held idol, otherwise calls `UIBase.ExitKeyDown`/`ControllerCloseAllKeyDown`
  (0x13E4E80; I: closes the panel).
- V Slot states: `IdolsItemContainer.GetSlotState(x, y, out refracted)` 0x13E8820 is a pure read of the
  active grid's `unlockMatrix[x, rows-1-y]`: 99 = `Blocked` (no cell in this shape), value%100 ≤
  `unlockIdx` = `Unlocked`, else `Locked`; ≥100 = refracted. `cachedSlotStates` (0x210) is the UI's copy.
- V `IDOLS_INVENTORY` (123) is the idol altar slot (`IdolsInventoryContainerUI`): `GetActiveGridData`
  0x141F4C0 picks the grid shape from the item it holds. Its edges (0x13E6510): Left/Right → equipment
  (2,0), Down → idol grid. I: its navigable ID is `IDOL_ALTAR` (the grid's Up and the equipment's Right
  select 180; the altar's Down selects 7).

So: from the equipment's right column, Right on the second row opens the idols and on the top row the
altar; from the backpack's right half, Up twice (through the first crafting button) also reaches them.

## What the mod adds

- `IdolRoute`: on the equipment slot whose Right press reaches them, the reading ends with "Right: idol
  altar" or "Right: idols", only when that navigable is registered and visible.
- `IdolGrid` + `InventoryAccessibility`: entering the grid leads with "Idols, N equipped, N free slots,
  N locked slots" (locked omitted at 0). Idols read name, type, "W by H", "On a refracted slot", "N of M",
  actions. Empty cells read "Free slot", "Free refracted slot", "Locked slot" or "No slot" from
  `GetSlotState`. The altar slot is named "Idol altar". R3 details are the existing item-details path.
- `InventoryNavigation`: with an empty cursor the grid is an idol list like the backpack (reading
  order, "N of M"). Ends follow the native exits: Left from the first idol, Up from the top row and Down
  with no idol row below leave through the game's own step from the border cell; Right at the last idol
  repeats it. Held idols keep the native cell navigation. The crafting-button route gets no hint.
- New hook: prefix on `IdolsContainerUI.HandleNavigateInput` (`IdolNavigatePatch`). Its prologue is
  byte-for-byte the shape of the already hooked `ItemContainerUI.HandleNavigateInput` (0x1426730):
  `push rsi; push rdi; sub rsp,28h; cmp [rip+…],0; mov esi,edx; mov rdi,rcx; jne` — first relative
  branch at +0x13, outside the 16-byte rule of [runtime-feedback.md](runtime-feedback.md). Patch
  safety: unique native body.

Tests: `tests/LeAccess.Inventory.Tests/IdolTests.cs` and new planner checks in `NavigationTests.cs`
(Inventory 356 checks).

## Unverified

Not heard in game. Which slots sit at the right column's rows 0/1 (the mod reads them at runtime);
that the altar navigable is registered and visible before an altar is owned; `GetSlotState` agreeing
with the container size for every altar shape (reads are guarded); the summary count staying stable
while the server syncs idols online.
