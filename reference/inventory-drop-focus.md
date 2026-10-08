# Inventory focus after a controller drop (2026-10-04)

User report: after dropping an item, focus returned to the first slot, so dropping several
items meant walking through many empty slots. The mod never wrote inventory focus.

## Native path (build 25690669, static disassembly)

- `ItemContainerUI.HandleSubmitInput` (0x1423F50) reaches virtual `ProcessDropSubmit`
  (0x1427710); `InventoryContainerUI` does not override it (shop containers do).
- `TryToDropItem` (0x14297E0) moves the item to the cursor (`Container_ItemRemoved` sets
  `forceUpdate`), drops it in the world and calls `SetFocusItem(null)`. Everything is
  synchronous, offline and online (removal is predicted locally); there is no prompt.
- End state: `currentFocusSlot` is the emptied slot. A stash quick move instead runs an
  inlined `FocusNearestItem`; the drop does not.
- Later reset, most likely: `OnUpdateTick` with `isHovered` and the pending `forceUpdate`
  writes the slot under the (clamped) mouse position. Correction (2026-10-04): y=0 is the
  bottom row, and in controller mode `EpochInputManager.GetMousePosition` returns
  `Vector3.zero`, which clamps to (0,0), the bottom-left cell. Re-selection through `OnSelected(Vector2Int)` was not
  reachable statically. Not confirmed live.

## Fix

`InventoryDropFocus` (prefix/postfix on `ItemContainerUI.ProcessDropSubmit`): when the
focused slot held an item, is now empty and the cursor is empty, it calls the game's own
`FocusNearestItem` and `SetFocusItem` (which moves the controller cursor, tooltip and the
mod's focus speech), then clears `forceUpdate`. For half a second the inventory poll
restores that item if anything other than navigation moves the slot; a prefix on
`ItemContainerUI.HandleNavigateInput` ends the guard. A restore logs
`[Inventory drop] focus moved to x,y; restored ...`, which identifies the reset source.
Refused/returned drops, quick moves and an emptied inventory keep the game's focus.

Tests: 10 drop-focus checks in `tests/LeAccess.Inventory.Tests`. Listening pending.

## Controller navigation (2026-10-04)

User report: "sometimes the cursor goes crazy" in the inventory. Static analysis, build 25690669:

- `ItemContainerUI.HandleNavigateInput` @0x1423ca0 (backpack 14×8, y=0 bottom) steps one cell;
  empty cells are visited (`SetFocusItem(null)`), a press inside a multi-cell item recurses until it
  leaves the item, and out-of-range steps go to `DpadContextChange`, which in the backpack exits
  only for Up (to equipment by column, or the crafting buttons for x≥8). Left/Right/Down
  **wrap silently** (x 0↔13, bottom→top).
- `InventoryMap` fires on press and auto-repeats a held d-pad: 0.5 s delay, then every 0.1 s
  (0.3 s over items), accelerating to about 30 cells/s after 1 s.
- `OnUpdateTick` @0x1426aa0 rewrites only the logical `currentFocusSlot` from the mouse position
  when `isHovered` and the pointer moved or `forceUpdate` is pending (any item add/remove/update);
  the visual cursor and `currentlySelectedItem` stay. `isHovered` comes from real pointer-enter
  events and from switching to keyboard/mouse (any key or mouse movement flips the device mode).
- A held item moves its anchor one cell per press through `HandleCursorItemNavigation`.

Mod behaviour (`InventoryNavigation`, the existing `HandleNavigateInput` prefix, whose original has
no relative branch in its prologue): in the backpack (`ContainerID.INVENTORY`), with an empty cursor
and multi-cell navigation, items form one list ordered by their top-left cell (rows top to bottom,
then left to right). Right/Left = next/previous item; Down/Up = first item of the next/previous
row; the ends repeat the item instead of wrapping; Up from the top row places the cursor on the
border row of the item's column and lets the game's step leave to the equipment. The start point is
the spoken `currentlySelectedItem` before the logical slot, so a hover rewrite cannot move it.
Focus uses the game's own `currentFocusSlot` + `SetFocusItem`. Items end with "N de M". Holding an
item, the stash, equipment and the auto-repeat timing are unchanged.

Merchant (same day, on request): the ordinary `ShopPanel` grids, stock (`ContainerID.SHOP`) and
buyback (`SHOP_BUYBACK`), are 12×16 `BaseShopContainerUI`s whose `HandleNavigateInput` @0x13c3990
clears the EventSystem selection and jumps into the same `ItemContainerUI` body, so the existing
prefix covers them. Their `DpadContextChange` @0x13c3200 exits only Up, to the shop tabs (x 0–1
Shop, 2–5 Buyback, ≥6 Shop highlighted); every other edge wraps. They therefore use the same list.
`LE.Factions.ShopContainerUI` overrides the edges and is excluded. Selling is X on a backpack item;
buying is X on a stock or buyback item and (offline) leaves the focus on the emptied cell, from
which the next step resumes in order. LB/RB switch Shop/Buyback, LT/RT move between the shop and
the backpack. Inventory suite: 257 checks. Runtime unverified: auto-repeat feel over items, whether `isHovered`
is ever true in controller mode (answered below: it is).

## Hidden pointer hover (2026-10-07)

User report (online): while navigating and equipping, after a while the cursor jumps to the last item.
The game updated that day to Steam build **25764382**; addresses below are for it (V = verified in
disassembly, I = inferred).

Evidence, mod log of the 2026-10-07 session: after Triangle equips an item the swapped item is read in
place, then the next line jumps without input, e.g. "Yelmo de roble de la esquiva … 16 de 19" →
"Amuleto de latón de comandante … 19 de 19" (lines 6910–6912; also 6923–6926, 11524–11527, 15291–15295);
reopening the inventory started on "22 de 22" (7348 → 7358); and each of about 30 drops logged
`[Inventory drop] focus moved to 0,0; restored …` (4040–4124, 7998–8025). The item reached is the one at
cell (0,0), bottom left; alone in the bottom row it is last in reading order.

- V `ItemContainerUI.OnUpdateTick` 0x1429530 (the body runs past its pdata size): with `isHovered`
  (+0xDC), when the slot under the mouse differs from `lastInventoryPos` or `forceUpdate` is set, it writes
  `currentFocusSlot` = that slot and raises the hover events. `EpochInputManager.GetMousePosition`
  0x10C2320 returns zero while the controller is active, so that slot is always (0,0).
- V `forceUpdate` comes from every item add/remove/update (`Container_ItemAdded` 0x1423570,
  `Container_ItemRemoved` 0x1423A20, `Container_ItemUpdated` 0x1423E30) and from `TryQuickEquip` 0x142BF60.
  `Container_ItemRemoved` sets `currentlySelectedItem` to the entry at `currentFocusSlot`, and `OnSelected`
  0x1429230 reopens on `currentFocusSlot`: that is when the spoken item jumps.
- V `isHovered` is written only by `set_IsHovered` 0x142E080. `onPointerEnter` 0x142DCA0 sets it without a
  controller check (only `SelectNavigableUnderPointer` is skipped); `OnMouseStatusChanged` 0x14290F0 clears
  it when the controller becomes active, `OnDisable` clears it. A hidden mouse resting over the grid when the
  inventory opens therefore leaves it hovered in controller mode until the inventory closes.
- I Online, the server's inventory sync after an equip supplies the removal right after the swap; offline
  the rewritten slot shows only at the next removal or reopen. Not measured.

Fix: a prefix on `ItemContainerUI.set_IsHovered` (`InventoryPointerHover.Filter`) stores false while the
controller is active, as the game itself does on switching to the controller; mouse and keyboard keep the
game's hover. Blocking a new hover logs `[Inventory hover] pointer hover ignored in controller mode:
INVENTORY`. The prologue's first relative branch is at +0x17. The drop guard above stays as a fallback.
Tests: 6 checks in `tests/LeAccess.Inventory.Tests` (`PointerHoverTests`). Listening pending: no jump after
equips, drops and pickups; reopening keeps the last item; `[Inventory drop] focus moved` no longer logged.
