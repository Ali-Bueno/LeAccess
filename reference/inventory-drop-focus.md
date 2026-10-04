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
is ever true in controller mode.
