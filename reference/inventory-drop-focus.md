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
  writes the slot under the (clamped) mouse position, which is (0,0) when the pointer
  rests above-left of the grid. Re-selection through `OnSelected(Vector2Int)` was not
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
