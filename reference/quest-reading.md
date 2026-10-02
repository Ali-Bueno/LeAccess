# Quest journal reading

Implemented 2026-10-02 for Steam 1.5.0.1, build 25663618. Game remained closed;
controller reachability and listening validation are pending.

`QuestAccessibility` follows the current navigation component or its active
selectable to a `QuestButton`, `QuestButtonGenerator`, or `QuestPanel`. It caches
the declared generator and uses it only while enabled, visible navigation belongs to that
generator, panel, or its declared `QuestInformation`. There are no scene-wide
searches or new native hooks. Active/pending flags are cached during Update;
ordinary consumers do not rebuild quest content.

The generator's `_selectedButton_k__BackingField` establishes current selection.
The button must belong to that generator and its visible, expanded quest panel.
The reader announces the button's displayed title/chapter, completion, and pin
state. Detail content requires matching quest pointers in `QuestInformation.quest`
and `previousQuest`, a matching displayed title, and visible owned content.
Objectives and rewards come from the declared typed lists and must belong to
their declared holders. Hidden, culled, transparent, collected, or foreign rows
are excluded. A visible objective check contributes a localized completion label.
Game-supplied names, descriptions and reward text are preserved.

`QuestAnnouncementState` settles two identical observations, deduplicates unchanged
content, and discards pending speech when selection disappears or changes. The
reader supplies an explicit summary and owned detail payload: temporarily hiding
details for the same selection does not repeat its summary, and restoring
unchanged details stays silent. Changed summary/pin/completion while details are
hidden speaks only current summary; changed visible objectives still speak. Pin,
completion and rendered-content changes can speak while selection stays still.
Popups, dialogue, controller menus, loading and lost application focus suppress
the reader. Focus claiming is limited to the selected quest button itself so tab,
filter and nested pin controls retain ordinary focus announcements.

## Native ownership evidence

Current installed `MethodAddressToToken.db` and `LE.dll` metadata identify the
following original `GameAssembly.dll` bodies. Read-only Capstone disassembly
confirmed their relationships; runtime code uses typed interop fields rather
than these addresses or offsets.

- `QuestButton.SetActiveTab`, RVA `0x17B55B0`: selecting a tab stores the button in
  its generator's selected-button field.
- `QuestButton.clicked`, RVA `0x17B6100`: writes its quest into the generator's
  declared information widget, then calls `QuestInformation.updateInfo`.
- `QuestInformation.updateInfo`, RVA `0x17C3750`: obtains localized quest name
  and step description; removes/rebuilds objectives and rewards for that quest.
- `QuestInformation.OnUpdateTick`, RVA `0x17C29C0`: when quest differs from
  previousQuest, copies quest to previousQuest before invoking updateInfo.
  This is a last-refreshed-selection sentinel, not an independent completion
  signal. Matching displayed identity and settled observations are also required.

Analysis used the existing metadata-map/disassembly workflow, copied into
`%TEMP%/LeAccess-quest-native-20261002`. No game process was started.

## Validation and limits

`dotnet run --project tests/LeAccess.Quests.Tests/LeAccess.Quests.Tests.csproj`
passes 77 checks against source-linked production readers/state and small game
doubles. Cases cover structured content, stale quest/title/sentinel rejection,
foreign/hidden rows, pin/completion changes, switching while pending, duplicate
speech, lifecycle and priority gates, unavailable speech, English/Spanish
mod-owned state labels, detail hiding/restoration, hidden state changes,
cross-quest detail provenance, disabled navigation, and preservation of game
title text. A `[Quest]` diagnostic is logged once per delivered announcement.
Main project builds
without warnings/errors with `-p:SkipPluginDeployment=true`; the final integrated
build was installed with its hash recorded in `STATUS.md`.

The selected button's visible title is required; no scene text or asset-name
guess supplies missing identity. If details are hidden or lag behind selection,
the reader announces the current title/state and waits for safely owned details.
A cold opening focused directly on an information widget outside all discoverable
panel/generator hierarchies needs a subsequent quest-button focus to establish
the cached owner. Actual scene hierarchy still needs controller testing.

QuestPanel's passive-point/idol counters describe campaign totals, so they are
not presented as selected-quest rewards. Rewards use the information widget's
per-quest reward rows. This feature introduces no quest audio ping.
