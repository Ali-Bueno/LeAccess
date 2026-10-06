# Equipment comparison reading (2026-10-04)

User report: the comparison text was not read. The primary item readers deliberately
exclude it, and nothing else read it.

## Native behaviour (build 25690669)

- `TooltipSystem.IsItemComparisonEnabled` → `TooltipItemManager.IsComparisonEnabled`
  (0x14938E0): `hasCompareAction` and `EpochInputManager.GetActionByNamePressed(...)`.
  The comparison exists only while the compare action is held.
- `TooltipSystem.ShowItemTooltipComparison` (0x2016A20) builds an
  `ItemTooltipComparisonInfo` from `_activeItemTooltipInfo` and
  `_activeComparisonItemTooltipInfo`, then calls `ItemTooltip.ShowComparison` on
  `_itemTooltip` and on `_itemTooltipComparison` (the equipped item).
- `ItemTooltip.ShowComparison` (0x1FDAAF0) marks each modifier line through
  `ItemTooltipModifier.SetIcon` with `Compare_Added/Removed/NoChange/Increase/Decrease`;
  the combined section uses `Compare_CombinedIncrease/Decrease`. The icons are visual
  only; `OnDataUpdate` draws `Info.IconSprite` with an inlined copy of `SetIcon`.

## Mod behaviour

`ItemComparisonIcons` records `SetIcon` overrides per modifier and drops them in an
`OnDataUpdate` postfix, otherwise using `Info.IconSprite`. `ItemComparisonReader`
requires both tooltips visible, the comparison widget's info equal to the active
comparison info and the main tooltip's item equal to the focused inventory item. It
speaks: "Comparado con {equipped name}", "Diferencias: Sube/Baja ..." from
`_combinedDifferencesSection`, then "Cambios: Nuevo/Se pierde/Sube/Baja ..." for marked
lines of both tooltips (unchanged lines omitted), or "Sin diferencias".
`InventoryAccessibility` speaks it once each time the comparison appears (two stable
frames), prefixed with the item brief if that was not yet spoken.

Tests: 6 comparison checks in `tests/LeAccess.Inventory.Tests`. Listening pending:
which button compares on the controller, whether "Sube/Baja" matches the shown arrows,
and length on items with many affixes.

## Controller modes (2026-10-05)

User report: with the controller nothing is read when comparing; the game uses L3 and R3 for
details. Static analysis, build 25711439:

- `TooltipItemManager.GetDisplayMode` @0x148E520 is the only mode decision, each tick from
  `TooltipSystem.OnUpdateTick` @0x20159A0, which reopens the tooltip when the mode changes. Actions
  (Rewired, map category `ItemToolTip`, both **held**): `TooltipCompareItems` (id 137) = **L3** /
  Ctrl, `TooltipModExplanations` (139) = **R3** / Alt; `TooltipModValues` (L1) only labels prompts.
  With a controller and a selected navigable, compare = held L3 XOR the auto-compare setting.
- Modes (`TooltipMode`): nothing → DefaultInfo; L3 → CompareDefault; R3 → Explanation; L3 then R3 →
  CompareModRanges; R3 then L3 → DefaultModRanges (the "ranges instead of description" setting
  swaps R3's meaning). `ItemTooltipModifier.OnTooltipModeChanged` @0x200A0F0 shows `_explanationText`
  only in Explanation and `_rangeText` only in the two range modes.
- Pad and mouse share `TooltipSystem`/`ItemTooltip`. `OpenItemTooltip` @0x2015DC0 stores
  `_activeItemTooltipInfo` and, in the compare modes only, `_activeComparisonItemTooltipInfo`; the
  comparison tooltip's `_topTag` reads "equipped in <slot>".

Why it was silent (first guess, superseded below): the reader required both tooltips and every line
to pass the on-screen geometry test; and it never ran for R3. The reader now keys on the mode of `_itemTooltip._info` (which must be
`_activeItemTooltipInfo`, for the focused item), uses `activeInHierarchy`/enabled labels and
ignores clipping. Compare modes speak the comparison (plus ranges in CompareModRanges), with the
equipped item's tag; Explanation speaks "Explicaciones: line: explanation; …"; DefaultModRanges
"Rangos: line (range); …". Each held mode speaks once; changing or re-pressing speaks again.
Inventory suite 274 checks; listening pending (auto-compare setting, very long tooltips).

## Older tooltip (2026-10-05)

Live test after the change above: L3 and L3+R3 still silent; the log had no comparison line at all.
Static analysis, build 25711439 (V = verified in disassembly):

- V `TooltipSystem.OpenItemTooltip` @0x2015DC0 checks `Features.Visual.ItemTooltipV2.Enabled` first;
  when false it only opens the older `TooltipItemManager`/`UITooltipItem` and never touches
  `_itemTooltip`, `_activeItemTooltipInfo` or `_activeItemTooltipParameters`. `VisualFeatures..ctor`
  @0x10F4890 builds that feature disabled and no native caller enables it (inferred: the older
  tooltip is the live one), so the V2-only reader failed its first gates in every mode.
- V `GetDisplayMode` @0x148E520: R3 is read from the gamepad template directly (`AltPressed`
  @0x10BD510); L3 is `Player.GetButton("TooltipCompareItems")` (held). With a controller and a
  selected navigable, compare = held L3 XOR `autoCompareEnabled`: with the auto-compare setting on,
  holding L3 *hides* the comparison. L3 then R3 → CompareModRanges; R3 then L3 → DefaultModRanges.
- V Both tooltips keep the container entry's own `ItemDataUnpacked` (`OnItemHovered` @0x2014E30),
  so identity is pointer equality; `IdEquals` only compares id bytes (can match empty ids).
- V The tooltip's native opener is `TooltipSystem.gameObject`, not the container, so
  `ItemTooltipReader.MatchesParameters` never matches inventory items; manual details come from
  `ItemTooltipModelReader`.
- V V2 with nothing equipped in that slot keeps the compare mode and hides the comparison tooltip.
- Older tooltip: the manager reopens in stages, one per frame (`activeInitializationStage`);
  `activeParameters` hold item and mode; `UITooltipItem.compareContent` is the equipped column
  (`compareNameText`, `comparePrefixes`, …) and `comparisonText` / `comparisonGained*` /
  `comparisonLost*` the swap summary. Affix rows are filled from `(string, bool)` tuples, so the
  explanation and range modes rewrite the rows themselves (inferred from `UITooltipItemAffix.Init`).

Mod behaviour: `ItemComparisonReader` tries V2, then `LegacyItemComparisonReader` (finished stage,
same item, mode). Compare speaks "Comparado con <name, type>" and the game's summary, or the
equipped item's rows when there is no summary; nothing equipped → "No hay nada equipado ahí para
comparar"; explanation and range modes read the rows ("Sin explicaciones…"/"Sin rangos…" when
empty). `[Inventory comparison state]` logs each change of tooltip source and mode (ignoring
loading frames) so the next live test shows which tooltip answered. Listening pending: whether the
legacy summary texts are filled, and the row text in R3 mode.
