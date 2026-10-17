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

"Right stick shows dexterity, intelligence and so on, but not life, mana or regeneration." First fix looked for
`CharacterStatDisplayLineItem`s with `SP.Health`/`Mana` and found none (`[Character sheet] no label or value on screen
for: ... ResourceStats`), online and offline. Replaced by the sections below.

## Sections and life from the prefab (2026-10-17, second report)

User: life and mana still missing; read the sheet in sections, right stick up for the next ("stats: life, mana, mana
regen, int, dex", then "critical chances", then "armour, dodge"), down for the previous.

Prefab evidence (read-only UnityPy dump of `CharacterStatsPanel` in `assets_d25ec10094c4285d.bundle`, game closed;
runtime unobserved). `CharacterStatsPanel/Content` children in order:
- `Header` (level; `Xp` inactive), `AttributesPanel` (corrupted variants inactive);
- `MainStats`: `HealthHolder`, `HealthRegenHolder`, `ManaHolder`, `ManaRegenHolder` rows (label TMP + value TMP) and
  bare `MoveSpeed`/`MoveSpeedStat`. Each value TMP carries a bare `CharacterStatDisplay` (`property` Health 7,
  HealthRegen 17, Mana 8, ManaRegen 18, Movespeed 9; `minionStat` 0; no line item parent). This is why line-item
  searches never found life;
- `Resistances` (heading TMP `ResistanceHeader`, key `Resistances_Title`; seven `ResHolder` rows, the typed `*Stat`
  values are plain TMP, their stat displays sit on inactive `*Value` siblings);
- `Defenses` (heading `DefensesHeader`, key `StatsPanel_Defenses_Header`; rows Block, Armour (value + mitigation),
  Stun Avoidance and WardRetention (bare displays), Block Protection, Dodge (chance + rating); Potion Slots inactive);
- four tab pages `DamageStats` (only one active by default), `DefenseStats`, `MinionStats`, `OtherStats`, each a
  `ScrollRect` with one `DetailedCharacterStatsTab` and two side-by-side columns `CharacterStatsFirst`/`Second`
  (not pages). No headings inside; the game's groups are separated by `Space` objects (not always in
  `_characterStatSpacers`), and `_characterStatDisplayLineItems` omits some lines, so hierarchy order is used. No
  culling script: off-screen lines stay active. The typed line items (parry, glancing blow, endurance threshold,
  potion health, companions, minion power) live in the Defence, Minion and Other tabs.

Reading (`CharacterSheetSummaryReader.Sections`): the blocks' parent is the common parent of `PhysicalRes` and
`armour`. Sections, each spoken as "title, N of M, entries":
1. main stats (mod title `sheet.section.main`): level, every bare display row of the block holding the Health display,
   attributes;
2. the resistances block, 3. the defences block: game heading (first visible words before the values, outside their
   rows), then rows in screen order, several values of one row under its label ("Armour: 92, 17%");
4. onward: the active tab's visible line items, cut at each column and spacer, and inside those by stat kind: a run of
   2+ lines of one `property` starts a section, a shorter run joins the lines before it
   (`CharacterSheetSummaryText.Groups`). Damage tab, first column: speeds (with cast speed), critical chances,
   critical multipliers, then area, stun, and the mana-cost line; the second column's damage and penetration lines stay
   one section.

Opening reads section 1 (config `ReadSummaryOnOpen`); right stick up/down (`MenuDetailInput.ReadRequested` /
`PreviousRequested`) while focus is on the sheet step through the sections, no wrap, re-read afresh each push so values
and the open tab are current. It runs before the generic details chain, which it replaces on the sheet. Log:
`[Character sheet] section N/M: <text>`, and `no life line on screen` once when the Health display is missing.
Tests: `CharacterSheetSummaryTests` (Classes 160). Live check pending: labels as shown at runtime (the prefab texts are
English placeholders; the game localizes them), the section sizes, and that the right stick's own scrolling of the tab
does not get in the way.
