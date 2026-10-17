# Character sheet stat lines

Focused stat lines (`CharacterStatDisplayLineItem`, e.g. `ThrowingCriticalStrikeMultiplier`
under `CharacterStatsFirst`, seen in the 2026-10-02 play log) read "Name. Value": the value
is the line's typed `_statValueDisplay` label and the name is the line's other visible
label, both as displayed (localized by the game). A line without a visible name or value
falls back to the generic element reader. Code: `LeAccess/Accessibility/CharacterSheet/
CharacterStatLineReader.cs`; tests in `tests/LeAccess.Classes.Tests/CharacterTileTests.cs`.

Live check pending: that controller focus lands inside the line item and the label order.

## Upper half summary (2026-10-10, user report)

The attributes, resistances, armour, dodge, block and damage are plain `TMP_Text` fields of `CharacterSheet`, not
`CharacterStatDisplayLineItem`s, so controller focus never reads them (the line items in `statsGridNavigables` are
the only focusables found in the interop stubs; the attribute level-up button is a separate control). Instead
`CharacterSheet/CharacterSheetSummary` reads `CharacterSheet.instance`:
- once when the sheet opens, queued behind the panel title (config `[Character sheet] ReadSummaryOnOpen`, default true);
- again on the menu details input (right stick pushed up, `MenuDetailInput.ReadRequested`) while focus is on the sheet,
  as the last step of `MenuAccessibility.ReadSelectedDetails`: a focused element with details of its own keeps them.
Each value's label is the nearest visible digit-free text before it in its row (else the first after), up to three
containers up; attributes use only the active corrupted/uncorrupted variant. Fields with no label and no words are
skipped and named in the log: `[Character sheet] no label or value on screen for: ...`. The summary is logged as
`[Character sheet] <text>`. Pure assembly: `CharacterSheetSummaryText` (tests in `CharacterSheetSummaryTests`).
Live check pending: that labels pair correctly (armour and armour percent share a row), and which fields get skipped.

## Life, mana and every stat line (2026-10-17, user report)

"Right stick shows dexterity, intelligence and so on, but not life, mana or regeneration; it must read every stat on
that screen." `CharacterSheet` has no plain field for life or mana (interop stub; only `UIGlobeHealth` and `ManaBar`
have health/mana text fields), so they are `CharacterStatDisplayLineItem`s, whose `_characterStatDisplay.property` is an
`SP`. The 2026-10-17 log's focus walk met only `CharacterStatsFirst`/`CharacterStatsSecond` lines (attack speeds,
criticals, damage), none for life or mana. Now the summary takes every active line item under the sheet's panel:
- right after the level, the lines whose property is `SP.Health`, `HealthRegen`, `Mana` or `ManaRegen` (minion
  stats excluded), on opening and on request;
- after block, the sheet's typed line items (parry, glancing blow, endurance, potion health, companions, minion power);
- on request only (right stick up), every other visible line item in hierarchy order, each read once.
When no life or mana line is on screen the log names `ResourceStats` among the skipped fields. Live check pending: where
the game shows life and mana, and whether the request summary is too long.
