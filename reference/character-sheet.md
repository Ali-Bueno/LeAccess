# Character sheet stat lines

Focused stat lines (`CharacterStatDisplayLineItem`, e.g. `ThrowingCriticalStrikeMultiplier`
under `CharacterStatsFirst`, seen in the 2026-10-02 play log) read "Name. Value": the value
is the line's typed `_statValueDisplay` label and the name is the line's other visible
label, both as displayed (localized by the game). A line without a visible name or value
falls back to the generic element reader. Code: `LeAccess/Accessibility/CharacterSheet/
CharacterStatLineReader.cs`; tests in `tests/LeAccess.Classes.Tests/CharacterTileTests.cs`.

Live check pending: that controller focus lands inside the line item and the label order.
