# Menu reading audit — Steam 1.5.0.1

## Guide and menu sweep (2026-10-07, log 16:16)

Fixed: the [game guide](guide-reading.md) article, its category states and the hint tooltip;
stash tabs and folders now say name, item or tab count, selected/priority state and
folder expansion (`Items/StashTabReader`; the generic path gave only the label).

Checked in the log, no change needed: dialogue responses, quest reward confirm, social
headers and tabs, party invite, pause/main menu buttons, keyboard keys, MTX tab.

Remaining, not verified by log or prefab: the transient "Ability Selection Icon" object-name
speech when the skill picker grid first focuses (skill agent area); "Available/Incompatible
Affixes Holder" names (forge shard list, crafting area); non-modal endgame panels
(Monolith, Factions, Observatory, Nemesis, Circle of Fortune, character stats) have no dedicated
reader and no log evidence yet; the stash tab/folder wording needs a listening check.

## Endgame panels (2026-10-08)

Monolith, blessings, echo rewards, Sanctuary of Eterra, entrances, boss votes, Nemesis, Observatory, Ladder and Morditas panels were not discovered as windows or lacked state; see [endgame-panels](endgame-panels.md). Listening pending.

## Native/prefab follow-up (2026-10-02)

The latest user report led to verified fixes for [creation tabs and class
descriptions](creation-menu-reading.md), [explicit name-keyboard activation](character-name-reading.md),
[map-owned quest selection](quest-reading.md), [modern portal labels](map-landmarks.md),
[manual right-stick item/skill details](skill-inventory-buttons.md),
[ability stat labels](skill-details.md) and [additional ambient/player subtitle
events](npc-subtitles.md). These follow-ups supersede the earlier audit's affected
gaps below. Native and host tests establish the sources and guards; controller
listening remains pending. No game was launched during this investigation.

## Listening follow-up: repeated HUD controls (2026-10-02)

The later session (log last written 09:46:33) repeatedly spoke the persistent
HUD bar: Details, Compare, Forge Details, Pick Up and More Information. Lines
448/451 and 597/598 switch the same actions between controller and keyboard
bindings. The old gameplay allowance made these prompts eligible even without
an owned menu selection.

`ButtonPromptAnnouncer` now resets its announcement state and returns while the
live player owns gameplay input. Picking up items, recreating HUD hints and
using keyboard screen-reader commands during exploration cannot announce that
bar. In menus, prompts must belong to the selected hierarchy or explicitly list
the current navigable. Their current controls remain available, but the tracker
remembers the action label: switching devices or remapping alone does not make
the same action new speech. Leaving gameplay for a menu rearms its hints.
Tutorial controls and item-owned inventory actions keep their separate readers.
Native input selection and mappings are never modified.

The source-linked input suite passes 76 checks, including repeated pickups,
controller/keyboard round trips, gameplay hints with explicit navigable ownership,
new menu actions and closing/reopening a menu. The 44 menu and 27 announcement
checks also pass. Listening remains required, particularly using NVDA keyboard
commands while moving with the controller.

## Listening follow-up: tutorial controls (2026-10-02)

The user confirmed skill descriptions work but could not hear equipped status,
button assignments, tutorial controls or inventory actions. See
[skill/inventory bindings](skill-inventory-buttons.md) and
[visible NPC subtitles](npc-subtitles.md) for the corresponding changes.

`ButtonPromptAnnouncer` previously read only `_promptDescription`; the real log
contained "Coger objeto", "Comparar" and "Más información" without controls.
It now pairs that visible owned description with `MultiButtonsActionPrompt.GetButtonsInfo()`.
Native RVA `0x107CC60` selects actual device overrides or the live prompt provider.
`PromptsInfoProvider.GetButtonsInfo` (`0x171B420`) splits Rewired's current element
name into a button combination. `InputBindingText` retains that combination and
hold semantics; a missing modifier suppresses the incomplete combination.

Installed `PermaLoad.bundle` `MasterTutorialList` (path ID `2538356036154949977`)
contains stable `Tutorials_*_Text` notice keys. Native
`TutorialManager.UpdateTooltipText` (`0x16EB8A0`) localizes the notice body without
adding a separate control instruction. The user's potion and evade messages
indeed omitted controls. `TutorialTextReader` preserves the displayed body and
adds current mapped controls for the identified potion, evade, portal, abilities,
inventory, passive/skill, quest, crafting, loot filter and social notices.
Names are resolved from the named `EpochInputManager.InputActions` member through
`ReInput.mapping.GetAction(id).name`, then the same prompt provider. Menu tutorials
without a direct controller binding may supply the actual radial-menu binding;
movement prose and unknown notice types are preserved. Visible native prompts
inside the tutorial also retain their action/control pair. No default key or
button is assumed, and no input setting is changed.

Source-linked screen-content tests pass 71 checks, including visible potion and
evade controls, remapping, device change, radial fallback, unknown tutorials and
hidden prompts. Native input decoding and prompt ownership now pass 76 separate
source-linked checks; the 44 menu checks also pass.
Audible results of these follow-up changes still need user verification.

The 2026-10-03 [announcement lifecycle audit](announcement-lifecycle.md) adds
passive tutorial deferral behind active dialogue and earlier same-frame speech.
It preserves blocking modal priority and rereads the latest visible tutorial
before delivery. Screen-content coverage now passes **211 checks**, including
both actual announcers in the game's update order and blocking-modal release.
Rejected output keeps only current owned visible content pending; recovery
rereads the source and acknowledges accepted transport output.

Read-only API/code audit, 2026-10-02. Game build 25663618; no game launch.
Implementation follow-up: [passive nodes](passive-reading.md),
[quest journal](quest-reading.md) and [name errors](character-name-reading.md)
now have dedicated readers and host tests. The findings below preserve the
pre-implementation audit; listening validation remains pending.
References below are paths and line numbers in the current repository. The
`last epoch code/` sources are generated interop wrappers: API existence is
evidence, but scene hierarchy, original method behavior and controller reachability
remain unverified unless explicitly confirmed by the user. Item tooltips are outside this audit.

## Prioritized findings

### 1. Passive nodes need a dedicated selection reader

Known feature gap: `STATUS.md` lists full passive-node navigation as unfinished.
`LeAccess/Accessibility/Skills/SkillSelectionReader.cs:28` handles
`SkillsPanelManager`, and `:33` reads its specialization nodes. This is not the
passive-tree navigation API. Generic selectable labels may speak, but do not
establish passive node identity, allocated points or requirements.

Game evidence: `last epoch code/PassiveTreeNavigable.cs:121` exposes an erased
`_navigationElements` array, with `_currentRow` (`:136`) and `_currentColumn`
(`:149`). It has `OnSelected(Vector2Int)` (`:281`) and `SelectNode()` (`:345`).
The current panel wrapper exposes `PassivesPanel.Manager`
(`last epoch code/LE.UI.PanelSystem/PassivesPanel.cs:197`), and that manager exposes
`activeTree` (`last epoch code/PassivePanelManager.cs:1361`).

Opportunity: follow the game's current cell and typed node/visible owned tooltip,
then speak name, points, description and requirements. First resolve the erased
array's actual element type and confirm the current panel uses this navigable;
do not assume the specialization reader's node type fits it. Respec confirmation
also deserves a test: the manager exposes `OpenRespecModal(canRespec, title,
message, confirmButtonText, onConfirm)` (`:1889`). Existing generic popup reading
may already cover it; API availability alone does not prove a missing announcement.

### 2. Quest journal has useful structured state beyond button labels

Known feature gap: quest journal reading is listed in `STATUS.md`; no dedicated
quest reader was found in `LeAccess/Accessibility/`. The removed quest audio ping
is a separate feature and should remain removed.

Game evidence: `last epoch code/QuestButton.cs` exposes `quest` (`:95`), visible
`tmp` title (`:140`), `chapterText` (`:200`), `completed` (`:314`),
`isHighlighted` (`:327`), `pinned` (`:340`) and `pinUnpinQuestTMP` (`:413`).
`QuestViewObjectiveUI.text` (`last epoch code/QuestViewObjectiveUI.cs:22`) and
`Initialize(objectiveText, ticked)` (`:65`) identify rendered objective content.
`QuestPanel` exposes reward counters (`:161`, `:176`), hidden state (`:191`),
next/previous selection (`:324`, `:335`) and focus placement (`:346`).

Opportunity: selected quest title/chapter/state, owned visible objectives and
reward counters, with updates when pinning or completion changes. Confirm the
objective ownership hierarchy before combining text; global quest text scans can
mix the previously selected quest with the current one.

### 3. Character name validation is not covered by generic field focus speech

`LeAccess/Accessibility/Announcements/ElementAnnouncer.cs:207` reads a focused
TMP input's text and placeholder, including empty state. This establishes focus
reading, not typed-character feedback, submission feedback or error ownership.
`MenuAccessibility.cs:80` skips an unchanged selected control during ordinary
focus polling; edits alone do not guarantee another focus announcement.

Game evidence: `CharacterNameInputValidator` has `confirmName` (`:35`), `errorUI`
(`:50`), `input` (`:65`) and `SetErrorText(string)` (`:168`).
`LocalCharacterSlots` independently exposes `inputTextTMP` (`:925`) and
`errorTextTMP` (`:940`). These provide owned error labels without ancestor guessing.
Opportunity: announce a newly visible/changed validation error, preserving the
user's normal screen reader editing behavior. The remaining character input
failure is known from status, but its exact controller failure needs testing.

### 4. Verify existing coverage before expanding dialogue/settings/radial menus

Dialogue already reads full subtitles and speaker names
(`LeAccess/Accessibility/Dialogue/DialogueAnnouncer.cs:108`), and limits response
ownership to a visible response panel (`:154`). Test response focus and return
from dialogue rather than assuming dialogue is absent.

Current settings component names are already recognized by
`LeAccess/Accessibility/Announcements/TextDetector.cs:152`: BoolSettingUI,
NumericSettingUI, SpinnerSettingUI and EnumToggleSettingUI. Unity toggle setters
are patched (`LeAccess/Accessibility/Patches/HarmonyPatches.cs:84`, `:96`).
Focus polling alone skips unchanged selection (`MenuAccessibility.cs:80`), so
same-control value feedback deserves testing for each custom widget. Recognition
of a setting label does not prove live value-change coverage.

The neutral-stick radial menu silence was reported by the user and addressed;
listening validation remains pending (`reference/steam-gameplay.md`, Controller
menu section). Classes/masteries similarly have typed readers now, but old
ClassTab hierarchy and carousel description availability remain unverified.
Existing-character name/level/class coverage also remains a status gap; do not
infer it from generic character-container detection.

## Controller checks for tomorrow

1. Open Skills, navigate assigned slots and specialization nodes, spend a point,
   reopen the panel, and verify the new selection never reads the prior tooltip.
2. Open Passives; move across several nodes and mastery tabs. Record whether name,
   allocated points, requirements and description speak. Inspect a respec dialog
   without confirming a destructive change.
3. Open the quest journal; change quests, pin/unpin one and inspect objectives.
   Record missing chapter/state/objective/reward speech separately from titles.
4. At character selection, navigate two existing characters; check name, level
   and class. At creation, inspect the name field, edit it and submit an invalid
   name; record both controller editing behavior and validation speech.
5. Change one toggle, numeric slider and spinner while keeping focus stationary.
   Test dropdown expansion and collapsed value. Verify only the changed control speaks.
6. Open the radial menu with a neutral stick, select a sector, close it; speak
   with an NPC, navigate responses, and return to gameplay. Check interruption,
   duplicate speech and restored focus. Include Start → Continue regression.

Do not add native hooks from this audit alone. Prefer current selection polling
and owned rendered text; any new hook still requires the repository's native
target audit and implementation tests.
