# Quest journal reading

Initial implementation: 2026-10-02 for Steam 1.5.0.1, build 25663618.
History/context follow-up: installed Steam 1.5.1, build 25672295. Game remained
closed; listening validation of the follow-up is pending.

## Current objectives versus earlier objectives

The preserved `BepInEx-152324.log` identifies one selected mission, **Al campamento
de los guardianes**, at lines 603 and 624: `En progreso` and `Ábrete paso a través
del bosque en llamas`. The manual read at line 635 appends five completed earlier
objectives of that mission, without first introducing them as history. That
explains the reported impression of several unfamiliar quests. The log does not
show the reader selecting or announcing other quest assets in this sequence.

Navigation now places the authoritative quest state after the selected title and
introduces active rows as **Current objectives**. Completed or failed objectives
stay in manual details under **Objective history for this quest**, with each row's
completion/failure state. The rendered fallback applies the same separation:
checked rows never become current navigation objectives. A completed quest cannot
reintroduce a retained active row as an unfinished task. History-only text changes
remain silent until the user requests details.

Current installed `QuestInformation.getObjectiveStateText`, RVA `0x17C3D70`,
uses `GetObjectiveActive` for both Active and Completed and `GetObjectiveFailed`
for Failed; NotStarted/NoLongerRelevant return no display text. Therefore the
formatter's text alone cannot distinguish active work from completed history.
`QuestModelReader` uses the actual enum, groups the history explicitly, and keeps
all selected quest and objective-step ownership checks. The native projection
`StatefulQuestList.getObjectiveStatesForQuest` is at `0x14CA0E0` in this build.
Addresses farther below document the earlier build and must not be reused as
1.5.1 addresses. Production uses typed APIs, not these RVAs.

`QuestAccessibility` follows the current navigation component or its active
selectable to a `QuestButton`, `QuestButtonGenerator`, or `QuestPanel`, and follows
the declared `WorldMapPanel._mapPanel.questPanel.buttonGenerator` chain while
the map owns quest navigation. It caches
the declared generator and uses it only while enabled, visible navigation belongs to that
generator, panel, or its declared `QuestInformation`. There are no scene-wide
searches or new native hooks. Active/pending flags are cached during Update;
ordinary consumers do not rebuild quest content.

The generator's `_selectedButton_k__BackingField` establishes current selection.
The button must belong to that generator and its visible, expanded quest panel.
Navigation announces the displayed title, the local player's current quest state,
chapter/pin state and clearly labeled current active objectives. Right-stick-up calls
`ReadCurrentDetails()` for the full step description, objectives and owned reward
rows. The native model projection described below supplies state/objectives even
when the information widget has not rendered them. Rendered fallback detail
requires matching quest pointers in `QuestInformation.quest`
and `previousQuest`, a matching displayed title, and visible owned content.
Objectives and rewards come from the declared typed lists and must belong to
their declared holders. Hidden, culled, transparent, collected, or foreign rows
are excluded. A visible objective check contributes a localized completion label
in manual history.
Game-supplied names, descriptions and reward text are preserved.

`QuestAnnouncementState` settles two identical observations, deduplicates unchanged
content, and discards pending speech when selection disappears or changes. The
reader supplies an explicit summary and current-objective payload: temporarily hiding
details for the same selection does not repeat its summary, and restoring
unchanged details stays silent. Changed summary/pin/completion while details are
hidden speaks only current summary; changed visible objectives still speak. Pin,
completion and rendered-content changes can speak while selection stays still.
Popups, dialogue, controller menus, loading and lost application focus suppress
the reader. Focus claiming is limited to the selected quest button and its exact
declared `buttonRectTransform` (the `MainButton` seen in the log), so tabs, filters
and other nested controls retain ordinary focus announcements. Manual delivery
acknowledges the current brief to prevent interruption by the following poll.

## Native ownership evidence

The follow-up controller audit found a missed navigation owner: the map itself
handles quest changes. `WorldMapPanel.HandleNavigateInput` RVA `0x207DF40`
delegates to `_mapPanel`; `MapPanel.HandleNavigateInput` RVA `0x1760920` calls
`QuestPanel.SelectNextQuest` / `SelectPreviousQuest` without selecting a descendant
of the quest panel. The old ancestor-only check therefore rejected native map
navigation even with a correct selected quest. The reader now accepts these typed
map links, checks enabled/visible map owners and reciprocal generator/panel
identity, and retains all selected-content guards. A cold `QuestInformation`
selection can resolve the registered `WORLD_MAP_PANEL` only when that map's
generator explicitly references this exact information component. This avoids
depending on a previous quest-button focus or a particular UI hierarchy.

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

The 13:01 runtime log, lines 1515–1527, records only
`Al campamento de los guardianes. I. Fijada`, then repeated `MainButton` and
`QuestHeader` focus. It does not identify which displayed-content guard failed.
The follow-up therefore retains those guards and adds `QuestModelReader`:

- `Quest.getLocalStatefulQuestList` RVA `0x14A9CA0` obtains the local player's
  appropriate list; `getStatefulQuest` RVA `0x14CABD0` searches it without creating
  or advancing a quest. The returned quest must match the exact selected asset.
- `QuestInformation.GenerateObjectives` RVA `0x17C18B0` uses
  `StatefulQuestList.getObjectiveStatesForQuest` RVA `0x14C9AD0` and
  `QuestInformation.getObjectiveStateText` RVA `0x17C35D0`. The reader uses these
  same read-only projection/formatting methods, preserving the game's omission
  of not-started and no-longer-relevant objectives. Each objective and its step
  must explicitly belong to the selected quest.
- `getNameForState` RVA `0x17C34A0` formats the actual local state. Both formatter
  methods are stateless and safe to call on the declared information component
  even when its displayed quest is stale or hidden; no UI field is changed.
- `Localization.Quests.GetStepDescription` formats the exact current step of that
  local quest for manual details. Active objectives form the navigation payload;
  prior completed objectives and the longer description stay in manual details.
  Rendered reward rows still require all original visible ownership checks.

The native functions allocate presentation lists/strings but do not pin, select,
advance, complete or open quests. The reader does not call `updateInfo` or
`GenerateObjectives` and does not expose unselected quest assets.

`dotnet run --project tests/LeAccess.Quests.Tests/LeAccess.Quests.Tests.csproj`
passes 124 checks against source-linked production readers/state and small game
doubles. Cases cover structured content, stale quest/title/sentinel rejection,
foreign/hidden rows, pin/completion changes, switching while pending, duplicate
speech, lifecycle and priority gates, unavailable speech, English/Spanish
mod-owned state labels, detail hiding/restoration, hidden state changes,
cross-quest detail provenance, disabled navigation, and preservation of game
title text. Follow-up cases cover local model identity, native state changes,
hidden/stale information, current versus completed objectives, foreign steps,
native hidden-objective states, collected/missing models, declared main-button
focus, manual reads, no subsequent interruption and no automatic long description.
History regressions cover its explicit introduction, multiple completed/failed
rows, foreign quest rejection, completed quest with a retained active row, both
model and rendered-fallback history changes staying silent, and current-objective
changes continuing to announce. The rendered fallback can announce a newly
available quest state after completion changed while content was hidden; it still
cannot include completed objectives as current tasks.
A `[Quest]` or `[Quest details]` diagnostic is logged once per delivery. The
follow-up main project builds without warnings/errors using
`-p:SkipPluginDeployment=true`; this feature investigation did not deploy or launch.

The selected button's visible title is required; no scene text or asset-name
guess supplies missing identity. If details are hidden or lag behind selection,
the native model can still supply the selected quest's current state, active
objectives and step description; visible rewards wait for safely owned rows.
A cold information widget must have a discoverable declared owner through the
registered map or panel hierarchy. The new regressions cover map-owned navigation,
separate information roots, disabled/foreign/missing owners and hidden journals.
Actual controller/listening behavior still needs testing.

QuestPanel's passive-point/idol counters describe campaign totals, so they are
not presented as selected-quest rewards. Rewards use the information widget's
per-quest reward rows. This feature introduces no quest audio ping.

## Output acceptance follow-up

The journal reproduced lost output when its state marked a candidate spoken
before the native reader accepted it. Candidates now remain pending until
`ScreenReader.TrySpeak` returns true. A failed manual read retains only the
selection's request intent and rebuilds current description/objectives/rewards
after recovery; changed, hidden, collected, unready or blocked ownership clears
it. Automatic retries yield to accepted same-frame output. Foreground and selected
quest identity are rechecked immediately before delivery. Existing deliberate
manual gestures still interrupt, and accepted manual output suppresses brief
duplicates. The source-linked journal suite passes **136 checks**, adding 12
rejection, fresh-recovery and ownership regressions. Tests used managed speech
stubs; the main no-deploy build has zero warnings/errors. Listening remains pending.
