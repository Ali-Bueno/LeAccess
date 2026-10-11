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
