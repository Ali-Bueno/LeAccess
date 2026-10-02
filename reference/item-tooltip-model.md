# Manual item details from the game's tooltip model

Implemented 2026-10-02 against Steam 1.5.0.1 / build 25663618. The game remained
closed; listening validation is pending.

An explicit inventory detail request can now obtain the selected item's native
tooltip content even when the visual tooltip is unavailable. Ordinary selection
reading still uses the existing visible-tooltip reader and brief identity fallback.
The manual fallback never opens a tooltip, changes selection, moves an item or
alters a native input state. Existing visual ownership guards remain in place.

`ItemTooltipModelReader.TryRead` requires the exact currently navigated, enabled,
visible `ItemContainerUI` and a matching selected item. It validates the unpacked
item identity, builds fresh native presentation data, checks the returned item,
slot and mode, then rechecks selection before returning text. Exceptions return
no partial result; `LastFailure` records the reason for diagnostics.

Exact owned native tooltip parameters preserve the game's current slot and display
mode. If no such parameters exist, the reader maps the actual container's
`ContainerType.INVENTORY`, `EQUIPPED` or `STASH` to their corresponding native
tooltip slots and explicitly requests `TooltipMode.DefaultInfo`. Other container
types require owned native parameters, because vendor/crafting variants have
additional native context. Ground, undefined and foreign parameters cannot supply
that context. The never-opened fallback intentionally reads default details,
without assuming an Alt/Ctrl or comparison mode.

Output uses native name/type/rarity strings, description, potential, weapon range
and attack rate, implicits, both modifiers of each prefix/suffix/sealed affix,
unique modifiers, set items and required set counts, added affixes, eligible item
types, lore and requirements. Tier, stat, lore and faction text remain game-localized.
Unidentified items expose only their game-provided name/type strings. The model
fallback excludes comparison deltas, prices and UI action prompts: those require
the current visible UI context. Specialized decorative header labels are still
best represented by the visible reader.

## Native and installed-asset evidence

Addresses below were resolved through installed `MethodAddressToToken.db` and
read-only Capstone inspection of original `GameAssembly.dll`. Runtime code uses
typed interop APIs, not addresses or offsets.

- `ItemTooltipContentBuilder.GetItemTooltipInfo`, RVA `0x1FCF390`, allocates an
  `ItemTooltipInfo` and fills its presentation fields/lists from the item and
  native item databases. It calls content helpers for type, potential, description,
  weapon values, affixes and unique/set data. No tooltip activation, inventory
  move/equip or selection setter appears in this builder path. The standalone
  constructor at `0x2004490` only stores item/slot/mode and does not populate data.
- Native `TooltipSystem.OpenItemTooltip` calls that same builder at `0x20115B4`,
  then separately displays the result with `ItemTooltip.SetTooltipInfo`. The
  accessibility fallback only calls the builder and formatting methods.
- `TooltipSystem.OnItemHovered` uses `container.Container.Type`. Its jump table
  at `0x2010E78` maps inventory to `0x2010A55` (slot 0), equipped to `0x20105A4`
  (slot 1), and stash to `0x20105AC` (slot 4), matching the named interop enums.
- `TooltipItemManager.GetDisplayMode`, RVA `0x148D850`, mutates held-key and
  timestamp fields at `0x148D945`, `0x148D956`, `0x148D9CA` and `0x148D9DB`.
  The reader does not call it or `GetTooltipParameters`.
- `ItemData.IdEquals`, RVA `0x12009A0`, compares inherited item-ID byte arrays;
  packed/unpacked wrapper differences therefore do not by themselves break identity.
  `ItemTooltip.SetTooltipInfo`, RVA `0x1FD47E0`, stores its exact input info pointer
  at `0x1FD4909`; no info cloning was found at that assignment.
- `SetTooltipInfo` calls `ItemDataUnpacked.LoreText` at `0x1FD55AB`, the separate
  faction widget at `0x1FD55E8`, and `ToRequirementsString` at `0x1FD55FC`.
  The model reader includes lore explicitly and uses the existing native
  `TooltipItemManager.ReqFaction` formatter (`0x1491140`) alongside the model's
  class/level/trade requirements. `SetItemTypeInfo` at `0x1FD1E90` populates primary
  and secondary type strings from rarity/type data, so rarity labels are not guessed.
- Installed `scene_PersistentUI_unity.bundle`: `TooltipSystem` path ID 11119 is
  enabled on active GameObject 554; primary `ItemTooltip` 12657 / `TooltipTemplate`
  12828 are enabled on initially inactive GameObject 2584. Comparison components
  12140/12320 are separate. This does **not** establish why the user's live visual
  tooltip read fell back to identity. Disabled serialized components were not found.

Asset evidence is retained in
`%TEMP%/LeAccess-quest-portal-tooltip-20261002/scene_PersistentUI_unity.bundle.json`;
native mapping/disassembly helpers are in
`%TEMP%/LeAccess-class-native-20261002`. Neither evidence set is a listening test.

## Validation

`dotnet run --project tests/LeAccess.ItemTooltipModel.Tests/LeAccess.ItemTooltipModel.Tests.csproj`
passes **50 checks** against source-linked production logic. Coverage includes
game-formatted sections and both affix modifiers, lore/faction text, native slot
mapping, exact owned parameters, unknown/foreign context rejection, unidentified
item filtering, collected lists/rows, formatter exceptions, packed/unpacked
identity, builder result provenance and a selection change during construction.
Main builds succeed with zero warnings/errors and deployment disabled.

These doubles verify ownership/format assembly and failure handling, not the
native builder's behavior in a running game. Test ordinary, magic, rare, unique,
set and unidentified equipment; inventory, equipped slots and stash; and changes
of selection during a manual request. The original visual rejection branch still
requires the added inventory diagnostics in a user-run session.
