# Dynamic dungeon, echo and encounter instructions

Added 2026-10-03. Source-linked tests pass; audible controller and online
validation remain pending. No game or audio device was opened.

The stateful quest reader does not represent every dungeon/Monolith objective,
enemy-modifier row or Morditas encounter instruction. The separate
`DynamicObjectiveAccessibility` reader samples their actual visible widgets at
the existing 100 ms quest interval, without mapping description indices or
guessing objective/completion state.

`QuestTrackerUI.questsDisplayed` supplies the exact rendered quest rows. A row
must have `TracksMonolithObjective` or `TracksMonolithModifiers`, no stateful
`quest`, and belong to the initialized tracker's owned `questHolder`. The
tracker's `currentScene` must match the active scene. Speech combines owned
visible `questName`/`chapterText` with its owned `objectivesDisplayed` labels
in visual reading order. Regular campaign rows and retained generic widgets
are excluded. Actual authored text, counters and modifier values are retained.

For Morditas, the native singleton's `_encounterScene` must be the current
loaded scene. Its visible `_currentObjective` must belong to that tracker and
also appear in `_activeObjectives`. The reader speaks its owned current label
and optional owned `questHeader.questName`. Old objectives and invisible
counter widgets are not included. Numeric kill/charge widgets are currently
omitted because a standalone number does not supply an authored spoken label.
While the [Rage of Morditas reader](rage-of-morditas.md) follows an encounter
(2026-10-19), it speaks the instructions, counts and timer itself and this
reader skips the Morditas tracker.

Only matching text samples settle. Visible changes coalesce while gameplay
speech is blocked; expired or foreign widgets clear their pending content.
Hidden, disabled, faded, culled, collected and blank labels cannot supply an
instruction. Disappearance alone never announces completion or safety.

The reader follows the existing `[Quests] AutomaticUpdates` setting and
`MinimumUpdateIntervalSeconds`. Both automatic quest readers share the same
speech deadline, preventing a dynamic instruction from speaking 100 ms after
a campaign update, or vice versa. Resetting a dynamic tracker preserves that
shared deadline. Output is passive through the existing project speech sink;
no game input, network state, hook or runtime dependency is added.

When both streams have settled eligible content waiting, delivery alternates:
after a stateful update, a waiting dynamic instruction gets the next shared
turn; after dynamic delivery, stateful updates can take the next. Hidden,
unready or changing provisional widgets cannot reserve a turn indefinitely.

Queued content and speech priority are distinct. `HasPendingContent` retains
updates for later, while `HasPendingAnnouncement` requires current gameplay
input, focus, valid player/character, current scene and no typing/menu/dialogue
ownership. Eligibility refreshes even between samples. Menus can therefore
read native equip-rejection notices while quest instructions remain queued.
Speech cooldown and a higher-priority gameplay announcement still retain the
eligible queue's priority.

## Installed asset evidence

Read-only UnityPy inspection of installed build **25690669** `PermaLoad.bundle`
verified the following identities (asset IDs are evidence only, not runtime
constants):

- `QuestTrackerUI` component `5101117933435074369` belongs to `QuestTracker`.
  Its `questHolder` GameObject `1186480126604694963` is the child `Quest Holder`.
- `RageOfMorditasQuestTrackerUI` component `2337340558997788710` belongs to
  `RageOfMorditas_HuntHUD`, below `QuestTracker`. Its `questHeader` component
  `8964041983792943310` is owned by that HUD.
- Authored `QuestTrackerQuestUI.objectiveObject` references external
  `QuestTrackerObjective` prefab GameObject `6954162029034353963`; it is not a
  live objective holder. The reader instead checks the actual runtime list's
  objective ancestry under each quest row.

Bundle SHA256:
`5253157ee4e98dc791962ea7006f7aea6e07643d2f2ed7afb8e35b76e6b442a9`.
The updated installed interop compiles the member calls. Old build-25672295
native-address tables were not used for this reader.

`LeAccess.DynamicObjectives.Tests` passes **90 checks**, linking both actual
quest services for cross-reader pacing. Coverage includes exact flags and
reciprocal hierarchy, external prefab identity, visible progress, empty and
foreign labels, encounter scene/current membership, menus, typing, hidden
content expiry, lifecycle, pending eligibility, shared deadlines and fairness
with both streams continuously pending. Existing automatic quest tests pass
**116 checks**, including absent/collected providers and unready-source recovery.
The main build with deployment
disabled has zero compiler warnings/errors.

Runtime remains necessary to confirm dynamic row creation, the native flags
used by each dungeon type, actual fade timing and online listening. Modifier
text is evidence of the displayed modifier; it does not establish the location
or timing of an environmental danger.

The output-acceptance follow-up reproduced lost instructions and a spent shared
cooldown after rejected native output. Dynamic candidates are now acknowledged,
and their shared pacing/fairness turn recorded, only after `ScreenReader.TrySpeak`
accepts the combined current text. Rejection retains current pending eligibility;
fresh replacement text must settle and hidden/expired widgets are discarded.
The 12 added source-linked regressions cover unchanged and changed recovery,
expiry, shared-clock preservation and campaign/dynamic turn fairness. No saved
failed text is replayed; current native labels are read at the existing poll cadence.
