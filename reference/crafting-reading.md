# Crafting and specialized filter controls

Inspected the installed Steam build **25690669** on 2026-10-03 without launching
the game or opening audio. The current native metadata, wrappers and installed
assets support bounded control-reading fixes. Host checks exercise the production readers;
successful live focus and listening remain unverified.

## Verified fixes

- Forge add/upgrade buttons now include their own rendered affix name, tier and
  available shard count. The two actions have separate mod-owned English/Spanish
  labels. Visible empty or markup-only affix names suppress dependent tier/count
  text; hidden, foreign or collected labels cannot supply content.
- Crafting-material shard buttons now include their row's rendered name, affix
  type and quantity. Manual inspection also includes the visible improvement.
  Reused pooled rows are read afresh; there is no retained content cache.
- Rune/glyph flyouts and the rune/glyph materials panel now expose the native
  selected rectangle for polling and manual inspection. Icon-only material
  controls read their exact container's localized subtype name and visible
  quantity, including empty stacks. No item or tooltip model is constructed
  for brief speech. Invalid, foreign or collected selected entries declare an
  unavailable selection, preventing whole-panel fallback and stale focus.
- Manual forge inspection now adds its visible native ready preview, modifier
  explanation, readable glyph preview, and current forging-potential heading
  and value. When native `canForge` is false, it instead adds the exact visible
  unavailable reason, readable from another live selected forge control after
  the native forge button has been hidden. Brief navigation stays unchanged.
- Filter bounds now include their own visible range heading: four potential
  ranges and the prefix/suffix count ranges. For example, the old generic speech
  was `Min: 2`; the new speech is `Legendary Potential. Min: 2`. Native localized
  headings and placeholder roles are read verbatim. Empty bounds remain empty;
  no zero, allowed limit or default is inferred.
- Native `Selectable.IsInteractable()` and the existing shared disabled wrapper
  preserve unavailable actions once. Foreground popup and navigation ownership
  continue through `UiFocusOwnership`; the specialized readers do not bypass it.
  Associated `InfoForTooltip` descriptions remain available on manual crafting
  inspection through the existing native `SetText()` path. Brief specialized
  speech does not invoke that path or construct an item/tooltip model.

Production entry points:
`Accessibility/Announcements/CraftingControlReader.cs`,
`CraftingSelectionReader.cs`, `CraftingMaterialReader.cs`,
`ForgeSummaryReader.cs`, `FilterRangeControlReader.cs`, and the specialized branches in
`ElementAnnouncer.BuildAnnouncementText`. These readers perform no input,
selection, item, filter, player or network writes and add no hooks/dependencies.

## Installed source and asset evidence

`PermaLoad.bundle` contains the `PanelSystemAssetsProvider` entries for
`ForgePanel` and `CraftingMaterialsPanel`. Their current soft GUIDs were resolved
through the current `LEAssetBundles/Catalog.bin`; loading the actual resulting
prefabs independently verified their identities. No old bundle-index table or
native address table was used as current evidence.

| Source | Installed identity / relevant structure |
|---|---|
| Forge panel | `assets_47fdfc1e558a73e2.bundle`, root `ForgePanel` |
| Four forge rows | `MainContent/ModSlot` and its three numbered siblings, each with an `AffixSlotForge` |
| Affix labels | `affixDescHolder/AffixName`, `affixDescHolder/tierLevel`, `AvailableShardsofSlottedType/Available Shard Count TMP` |
| Add/upgrade controls | `Shard/addShardButton` and `upgradeAvailable`; serialized `addAffixButton`, `upgradeButton`, `_upgradeButton`, and reciprocal `affixSlotForge` |
| Crafting material panel | `assets_302b3eb53d6ca3c0.bundle`, `ShardAffixListElement` path ID `5942668086542144629` |
| Shard button | Serialized `button` GameObject has its own native `Button`; name, quantity, type and improvement are row fields, outside that button |
| Filter templates | Installed `resources.assets`, roots `potentialcondition`, `affixcountcondition`, `affixcondition` |

Current wrapper members are at `last epoch code/AffixSlotForge.cs:157`
(`nameTMP`), `:172` (`countTMP`), `:202` (`tierTMP`), `:232` (`addAffixButton`),
`:247` (`upgradeButton`); `CraftingUpgradeButton.cs:43` (`_upgradeButton`) and
`:58` (`affixSlotForge`). `AffixSlotForge.NumberOfShardsAvailable` is exposed at
`:713`. Current native `SetVisuals` (RVA `0x114A7D0`) reads the shard-storage
quantity, retains it at `0x114AB3B`, formats it at `0x114AE02`, and assigns the
count label. The installed authored hierarchy also identifies this as available
shards, not a forging cost.

`RuleHolderUI.GetConditionPrefab` (`RuleHolderUI.cs:559`, current RVA
`0x11A2B80`) concatenates the native condition type name with the metadata
literal `ConditionsUI/`. The installed resource roots independently confirm
the corresponding potential and affix-count templates. Each supported range
row has direct serialized siblings in this order: `Title`, its `InputRange`,
and an optional `sub-title`. The min/max input placeholders are `Min` and `Max`.
The reader requires exactly one meaningful visible direct heading before the
owned range; it does not identify headings or conditions by GameObject names.

The native references used for runtime ownership are exposed in
`PotentialConditionUI.cs:47` (four range fields),
`AffixCountConditionUI.cs:126` (two range fields), `InputRange.cs:121`
(`_minInput`/`_maxInput`) and `NumberInput.cs:109` (`_input`). A field, wrapper,
range, condition, heading or placeholder that no longer belongs to this exact
control yields to ordinary control reading. A populated input can hide its
own placeholder; its authored role is still valid when the placeholder remains
an exact descendant of that input.

### Rune/glyph selection

The installed forge contains `CraftingRuneSelectorFlyout` and
`CraftingGlyphSelectorFlyout`. Current
`CraftingMaterialSelectorPanel.HandleNavigateInput` (RVA `0x13CB6F0`) and
`OnSelected(Vector2Int)` (`0x13CC060`) use the same `_selectedButtonId` for
`_selectableRectTs` and `_selectables`; the latter supplies the native material
focus calls. The reader validates both list bounds and the selected control's
ownership of its rectangle. Wrapper fields are at
`CraftingMaterialSelectorPanel.cs:66`, `:109` and `:124`.

`CraftingRunesAndGlyphsPanel.FocusSelectable` (`0x287C470`) uses the rank-two
`_selectables` matrix with `_selectedRowIndex`, then `_selectedColumnIndex`,
and passes the resulting `MaterialContainerUI.rectTransform` to the focus
cursor. Fields are at `CraftingRunesAndGlyphsPanel.cs:247`, `:262` and `:275`.
`CraftingSlotManager.FocusSelectable` (`0x116BC20`) likewise uses its actual
rank-two rectangle matrix with `currentlySelectedIdx.x`, then `.y`
(`CraftingSlotManager.cs:3661`, `:3676`). The reader bounds-checks the native
arrays; it never guesses an index from authored row arrays or invokes native
focus, navigation, hover, popup or selection methods.

Material speech requires the exact selected rectangle GameObject, an active
owning `MaterialContainerUI`, and its raw `container` field cast to
`SingleSubTypeContainer`. The container identifier must equal the material's
identifier. Exactly one `allowedBaseType` and a nonnegative `allowedSubID`
supply `Localization.Items.GetSubTypeName`; no rune/glyph ID or name table is
hardcoded. This remains valid without a populated item stack. The rendered
`externalItemNumber` must be visible and belong to that material. Installed
flyout quantity labels are under their owning container's quantity child;
foreign or hidden labels contribute nothing. Native `material.interactable`
uses the existing disabled suffix once. The mod-owned quantity label is
localized in English and Spanish; material names come from the game.

The existing `UiFocusOwnership` path supplies immediate foreground/loading,
visibility and same-frame ownership checks. Manual details preserve the
existing owned `InfoForTooltip` path; no new tooltip builder or hook was added.

### Manual forge summary and unavailable reasons

The `CraftingSlotManager` component is on the installed `ForgePanel` root.
Its exact serialized labels are descendants of that root:

| Member | Installed visible source |
|---|---|
| `singleSuccessChanceString` | `MainContent/SuccessChanceHolder/singleSuccessChance` |
| `modifierDetails` | `MainContent/Modifier Item/Modifier Details TMP` |
| `glyphsPreviewText` | `MainContent/SuccessChanceHolder/SuccessChance (1)/Glyphs` |
| `cannotForgeText` | `MainContent/BottomVisuals/cannotForgeText` |
| `typeOfForgeText` and `currentForgingPotentialText` | The `CurrentForgingPotential` heading and owned value under `SuccessChance (1)` |

Current native `SetForgeButtonInteractable` (RVA `0x11725D0`) writes `canForge`
at `0x1172615`. Its blocked branch hides the forge action at `0x1172680`,
writes the supplied native reason into `cannotForgeText` at `0x11726C6`,
and shows that label. Its ready branch hides the old reason and writes the
native preview string at `0x11728C3`. Wrapper fields are at
`CraftingSlotManager.cs:1689`, `:1816`, `:1831`, `:1876`, `:1891`, `:1906`
and `:2056`. The installed `GameAssembly.dll` hash was rechecked unchanged
for this follow-up.

`ForgeSummaryReader` requires the live navigation owner to be that exact native
manager and the inspected GameObject to be its current native matrix rectangle.
Every field must be visible and belong to that manager. `canForge` also gates
which group can contribute: retained preview fields cannot be spoken during a
blocked state, and retained failure text cannot be spoken after recovery. All
content is read afresh, without native actions or model construction. The
existing `TryReadDetails` path adds this summary after selected-control details;
the brief path never invokes it. No extra localization keys are needed.

Rich-text styling is cleaned, but any fragment containing an unresolved TMP
`<sprite>` tag is excluded whole. The installed glyph template contains symbolic
content such as `<sprite=1> and maybe`; stripping its icon would destroy its
meaning. Symbol interpretation, min/max forging-cost values and additional
critical-success presentation remain outside this bounded reader.

Current file SHA-256:

| Installed file | SHA-256 |
|---|---|
| `GameAssembly.dll` | `FC83F941396282C0E489ED44FF4375130FB74F3C5D8CDF983696A9EFBDC9019A` |
| `resources.assets` | `D4F82D96C854BE34D6CEC4CECF9533A2D3AE9DA8B9904395FFC63FC91BF4C49D` |
| Forge bundle | `3ECD187AF8A775E3E33A6870EF6E10657E66634D9F16D2C13279E06CF46AE478` |
| Crafting-material bundle | `92464BFFBA35D550EAB3384D3524026B286F146B4EE8313EB5B792EA201B45D8` |

Read-only extraction scripts/evidence live in
`%TEMP%/LeAccess-specialized-controls-20261003/`. Bundle MonoScript identities
came from the current installed `monoscripts.bundle`; resource identities came
from current `globalgamemanagers.assets`. Matching current bundle TMP schemas
decoded the stripped resource text/input records. Native disassembly used the
current method map in `%TEMP%/LeAccess-hazards-25690669-20261003/`.

## Validation and limits

`dotnet run --project tests/LeAccess.MenuReading.Tests` passes **296 checks**,
including **53 prior checks** in `CraftingTests.cs` and `FilterRangeTests.cs`,
**35 selector checks** in `CraftingSelectionTests.cs`, and **30 new manual
forge checks** in `ForgeSummaryTests.cs`. Source-linked scratch
baselines independently reproduce missing native selector focus and missing
localized material speech with each integration branch removed. Repository
source remained intact during those baseline runs.
The new source-linked manual-preview integration regression also failed before
the production summary fix. Its checks cover ready/blocked/recovered states,
the hidden forge action, live action inspection, exact selection changes,
visible/foreign/blank labels, unresolved symbols, native localized reasons,
tooltip preservation and lifecycle/foreground gates.
Checks include action distinction, disabled wrapping, native descriptions,
updated/reused rows, markup-only/hidden/foreign labels, exact button/range/input
ownership, native lifetimes, localized text, empty fields and popup ownership.
`dotnet build LeAccess/LeAccess.csproj -p:SkipPluginDeployment=true` passes with
zero warnings/errors against the refreshed installed interop.

The new selector checks additionally cover native matrix rank/bounds, missing
cells, mismatched rectangles, immediate focus changes, quantity updates,
empty stacks, exact container identifiers, ambiguous types, disabled native
behaviours, localization, hidden/foreign labels, foreground popup ownership,
and brief versus manual tooltip population.

Rune/glyph live focus, native interface casting and spoken output remain
unverified in gameplay. No guessed matrix indexing, item matching, selection
or model creation was added. Symbolic preview content, forging-cost ranges,
additional critical-success presentation, advanced affix comparison/combined-tier fields, sealed-affix dropdown, and other
filter condition types also remain outside these fixes. Full forging/filter
workflows, gamepad focus updates and listening still require gameplay checks.

## Controller forge (2026-10-05)

User request: make the forge usable entirely. Static analysis, build 25711439, corrected the earlier
notes: `CraftingSlotManager.selectables` holds the cells' *components*; the grid the game focuses is
`activeNavigationElements` (RectTransform[7,5], [column,row]). The old reader cast `selectables` to
`RectTransform`, always null, so the forge was silent. Most cells focus through
`ShowCircleFromRect` @0x10295B0, which never calls `ControllerFocusCursor.Show`, so only polling hears them.

- **Grid** (`.ctor` @0x117E170, prefab Odin data): y0 materials toggle (0), help (6); y1 prefix 1
  upgrade (0), prefix 1 (1), item `CraftingMainUI` (3), suffix 1 (5), its upgrade (6); y2 the same for
  prefix/suffix 2 with the glyph `CraftingSupportUI` at (3); y3 shard/modifier `CraftingModifierUI` (3)
  and rune `CraftingRuneButton` (4); y4 forge button (3). Native moves (@0x116E570 etc.) step to the
  next non-null cell, wrapping; they never leave the grid. `OnOpen` sets (3,3), but the grid only
  gets focus through L2 (see below).
- **Cross** (`HandleSubmitInput` @0x116D5F0): affix row → materials panel filtered to that slot
  (only with `addAffixButton` active); item → pick up/place; glyph/rune → their flyout; shard slot →
  materials panel; upgrade → that affix's shard into the shard slot, focus to Forge; Forge →
  `Forge()` (only the Rune of Corruption asks to confirm). Square on the item returns it to the
  inventory; in the inventory, Square sends an item to the forge. Circle closes everything.
- **Result**: `OnCraftingAttempted` @0x116FA30 (offline and online) logs "[Forge Craft] Result" and
  then fills `singlelineFractureChanceTMP`, `minorFractureChanceTMP`, `lostForgingPotentialTMP`,
  `typeOfForgeTMP`, `criticalChanceOutcomeTMP` synchronously. Hooking it is avoided (structs by value),
  so `ForgeResultLog` (a BepInEx log listener) notes that line and the labels are read next frame.

Mod behaviour (`Forge/ForgeCellReader`, `ForgeAccessibility`): polled reading of the selected cell —
slot and affix (name, tier, shards, "Mejora a la izquierda/derecha", what Cross does), upgrade, item
with forging potential, glyph, shard/rune, runes, Forge with the game's wording and cost range or its
refusal reason, materials, help. Opening says "Forja" and the item first; right stick up adds the
forge summary; each craft speaks "Resultado: …", leading the new cell when the game moved focus.
The generic announcer stays silent for the grid. The materials panel and flyouts keep their readers;
`CraftingSelectionReader` now reads `activeNavigationElements`. Forge suite 28 checks, menu 307;
listening pending (the whole flow, the result wording, the corruption confirm).

## Forge from the inventory (2026-10-05)

Live test: "Forja" from the radial menu opened the inventory; nothing about the forge was said, and
Square on an item seemed to do nothing. Static analysis, build 25711439 (V = verified):

- V Radial "Forja" (@0x1085810) and the keyboard both call `UIBase.CraftingKeyDown` @0x17F7300 →
  `TogglePanel<ForgePanel>`. `ForgePanel.OnOpen` @0x2057040 initialises the grid and opens the
  inventory. `PanelSystem.OnPanelOpen` focuses only a panel whose `ShouldFocusOnOpen` is true:
  the forge only during the Forging Basics tutorial, otherwise the **inventory** keeps focus.
- V **L2** (`leftShoulder2`, `InventoryMap.ProcessInput` @0x1037860, `TryFocusInDirection(-1)`)
  moves focus to the forge panel; **R2** back to the inventory. `ForgePanel.OnSelected` @0x2057140
  selects the `CraftingSlotManager` (grid, index (3,3)), or `_main` (`CraftingMainUI`, an
  `ItemContainerUI`) when the cursor holds an item. The action bar shows "L2 Forja".
- V Square in the inventory: `HandleSubmitInput(X_SQUARE)` → `ProcessDropSubmit` @0x1427900 →
  `PanelSystem.TryQuickMove` into the forge's main, modifier or support slot. The prompt only checks
  the item type (`CraftingMainItemContainer.CanReceiveItem` @0x13CAE40), not whether the slot is
  occupied; a refused move changes nothing and the game says nothing. After a move, focus stays in
  the inventory on the nearest item. Holding Square is the inventory's "transfer materials".
- V Crafting works anywhere once unlocked (no town/NPC check): locked → "The Forge is not unlocked
  yet"; during Forging Basics it opens only in the Osprix Warcamp.

Mod behaviour (`Forge/ForgeInventoryNotices`): when the forge opens, the inventory's first brief is
led by "Forja", its item (or "Sin objeto…") and "L2 entra en la forja; R2 vuelve al inventario".
Square that fills a forge slot leads the next brief with "<item> en la forja"; Square that leaves
the item in place with an empty cursor speaks "No se movió a la forja" (with the forge's item when
it holds one). Focus on `CraftingMainUI` is read by the forge reader (item and "Confirma para dejar
aquí el objeto que llevas"), not as an inventory slot. Listening pending.

## Crafting materials from the inventory (2026-10-07)

User report: shards, glyphs and runes cannot be read from the inventory. Steam build **25764382**
(updated that day; the materials bundle `assets_302b3eb53d6ca3c0.bundle` changed with it). V = verified.

Log of the 2026-10-07 session: the equipment's way up reaches `openCraftingInventoryButton` ("Materiales de
fabricación"); Cross opens the panel on the filter "cualquier afijo" (5429–5442, 12270–12316). Shard rows
`ShardButton(Clone)` spoke only the affix name ("Vida"); the section stops spoke their object names
("Available Affixes Holder", "Incompatible Affixes Holder"); Up from the filter reached "Runa de creación.
Cantidad: 0" (12285), but nothing said the runes and glyphs are there.

- V `CraftingMaterialsPanelUI.HandleNavigateInput` 0x178CE20, on the filter row (NavigableID 21): Up →
  `TryToSelectNavigableFromIDAndSetIndex(CRAFTING_RUNES_AND_GLYPHS_PANEL 57, (5,1))`; Down → shard list
  (CRAFTING_MATERIALS_SCROLL_VIEW 23) only when `IsAnyShardAffixAvailable`; Left/Right walk the three filter
  `TabUIElement`s (`prefixesOnly`, `allAffixes`, `suffixesOnly`; current one in `currentlySelected`), Left
  of the first reaches the search field (22). Up from the list's top returns to the filters.
- V Row prefab `ShardButton`: the root holds `ShardAffixListElement` and `TooltipAffix`; its `Button` is the
  child `Button`. `ShardAffixListElement.OnSelected` 0x17D2140 shows the focus cursor on the root, so the
  old "inside the button" check failed and the generic reader took the first label.
- V `BuildView` 0x17D1320 draws the count with `FormattingExtensions.WithBrackets` ("[12]").
- V `OnSelected` also calls `TooltipAffix.OnPointerEnter` 0x17F0410: a shard item (base type 101, subtype
  = affix) opened through `TooltipSystem.OpenItemTooltip` with slot `AFFIX` and the row root as opener.
- V The section stops are the `PooledVerticalLayoutGroup` holders (Selectables in the list); their headings
  are `unusedAffixesHeader`, `appliedAffixesHeader`, `incompatibleAffixesHeader` (keys
  `Crafting_Materials_Shards_Affix_List_Available/Applied/Incompatible`). Section lists hold filtered-out
  entries too (`OnFilterUpdated` 0x178D890 flags them), so no count is spoken.
- V Runes and glyphs: `CraftingRunesAndGlyphsPanel` rows `_firstSelectablesRow` (6 runes, 3 glyphs) and
  `_secondSelectablesRow` (6 runes, 2 glyphs) of `MaterialContainerUI`; no `InfoForTooltip`, so manual
  details added nothing. `ItemTooltipContentBuilder.GetDescription` 0x1FEC390 reads
  `Localization.GetText("Item_SubType_Description_{base}_{sub}", ItemList.get().GetNonEquipmentSubItem(base,
  sub).description)`, which needs no item in stock.

Mod behaviour (`CraftingControlReader`, new `CraftingMaterialsPanelReader`, `CraftingMaterialReader`):

| Stop | Spanish |
|---|---|
| Filter | "cualquier afijo, filtro seleccionado. Arriba: runas y glifos; abajo: fragmentos de afijo" (the game's no-shard notice joins when shown) |
| Section stop | the game's localized heading (English prefab text "available to be applied"); never the object name |
| Shard row (root or button) | "Vida. Prefijo. Fragmentos: 12"; manual details add the improvement type and the affix tooltip the game opened for that row |
| Rune or glyph (panel and forge flyouts) | unchanged "Runa de creación. Cantidad: 0"; manual details add the game's description |

Tests: 16 new checks in `tests/LeAccess.MenuReading.Tests` (323). Listening pending: the shard tooltip
text through the older tooltip reader, the localized headings, and descriptions for every rune and glyph.
