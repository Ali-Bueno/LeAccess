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
