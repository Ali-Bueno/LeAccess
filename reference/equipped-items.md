# Equipped item reading

Updated 2026-10-03 against Last Epoch 1.5.1 / Steam build 25672295. The game
remained closed; controller listening is still pending.

## Cause and behavior

Equipped slots use `EquippedItemsNavigable` as the current navigation owner.
The previous inventory reader accepted only a directly selected `ItemContainerUI`.
Consequently the ordinary focus reader reached object-name fallbacks such as
`Head` and `Hands`, while equipped item names, actions and manual details were
never owned by the inventory reader.

`InventorySelectionReader` now resolves the native `navigables[x,y]` cell at
`currentlySelectedIdx`. Its `rects[x,y]` cell identifies the exact highlighted
rectangle, including layouts where that rectangle is separate from the item
container. Array rank, bounds, collection state, component lifetime, enabled
state and visibility are checked. No navigation arrays are reordered and no
input, selection, tooltip or inventory mutation is performed.

Equipped item identity comes from the selected UI's existing backing container,
which must report `ContainerType.EQUIPPED` and the matching `ContainerID`.
`GetEntryAt(currentFocusSlot)` reads its live contents. A retained
`currentlySelectedItem` therefore cannot conceal an item swap or removal.
Only a valid, empty backing slot is called empty; missing, collected or mismatched
data is reported as unavailable. The ordinary inventory continues using its
native current item selection.

Automatic output is slot, actual item name/type, equipped state, then the item's
available actions and verified merchant context. Full item properties remain on
the existing manual detail gesture. The native model fallback, action reader and
merchant reader share the same selection ownership and recheck it after building
content. A slot change or removal during construction discards stale output.
Names settle over two observations; unchanged equipment does not repeat.

The 2026-10-03 follow-up also rechecks foreground ownership immediately before
output. Previously a manual model read could still announce equipment after a
modal, dialogue, controller menu, loading screen or focus loss began during that
read: item identity alone remained valid. Five failing regressions reproduce
those transitions; the inventory service now passes 178 checks. Native model
coverage remains 102 checks. No game was launched.

## Native and localization evidence

Resolved from the installed `BepInEx/interop/MethodAddressToToken.db` and inspected
read-only with Capstone. GameAssembly SHA256:
`502E32F1F31BC1979AC6C387FAEF1266A62AD29F5B573E6D9B51CB577A72F5F8`.
Addresses below are evidence only; production code uses typed interop APIs.

- `EquippedItemsNavigable.Select`, RVA `0x104B5E0`, indexes `navigables` with the
  selected vector's x and y at `0x104B623`–`0x104B651`, calls the selected item's
  virtual selection method at `0x104B696`, then passes the corresponding
  `rects[x,y]` to `ControllerFocusCursor.Show` at `0x104B745`.
  The method does not replace the navigation manager's owner with that child.
- `EquippedItemsNavigable.GetIndexOfNavigable`, RVA `0x1049F10`, searches the same
  rectangular table and returns its two indices; absent cells return (-1,-1).
  Runtime reading uses `Il2CppSystem.Array.GetValue(x,y)` rather than guessed
  enum positions, raw object offsets or a hierarchy search.
- `ItemContainerUI.SetFocusItem`, RVA `0x14287E0`, stores the hover selection and
  separately invokes cursor/tooltip handling. `OneSlotItemContainer.GetEntryAt`,
  RVA `0x4A4050`, returns the current contained entry directly. Its Type and
  container ID are exposed by the native getters at `0x3B0D80` and `0x4171F0`.
- Installed `localization-assets-shared_assets_all.bundle` and English/Spanish
  string tables confirm `EquipmentType_HELMET`, `BODY_ARMOR`, `GLOVES`, `BELT`,
  `BOOTS`, `RING`, `AMULET` and `RELIC`. These provide game-localized slot nouns.
  `Cosmetics_Flyout_Title_Mainhand` reads “Main Hand Weapon” / “Arma de mano
  principal”; `Bazaar_MainPanel_EquippedPreview_OffHand` reads “Off-Hand” /
  “Mano secundaria”. The cosmetics offhand title is intentionally unused because
  its actual text describes cosmetics rather than the equipment slot.
- Missing native translations fall back to the mod's English/Spanish catalogs;
  left/right ring labels remain distinct. Unresolved localization keys are never
  spoken as slot names.

The reproducible native mapping, inspection helper and localized-key probe are
in `%TEMP%/LeAccess-equipped-20261002/`. Existing analysis-only UnityPy was used;
no dependencies, native hooks or bundled game assets were added.

## Validation and limits

The source-linked inventory and native item-model suites cover equipment-grid
selection, x/y order, separate focus rectangles, every equipment slot label,
localized fallback, unknown versus empty slots, stale hover data, same-slot
swaps/removals, collected objects, hidden owners, foreground interruptions,
manual details, native item actions and merchant sale prices. They also reject
selection changes during native model/action/price construction.

These checks verify adapter ownership and speech assembly, not runtime sound or
controller behavior. Listening should confirm each worn slot, both rings, a
swap/removal without leaving the slot, manual properties, and closing inventory
or opening a dialog while a selection is pending. Native navigation order is
preserved; the mod follows the same selected cell and highlight as the game.

Idols beside the equipment (route, idol grid and altar reading): see [idols.md](idols.md).
