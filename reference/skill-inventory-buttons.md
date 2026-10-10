# Manual tooltips, skill assignment and inventory feedback

## Current behavior — 2026-10-02

Navigation reads a brief name and current state. Descriptions, modifiers, costs,
DPS and other tooltip contents require **right-stick up**, from neutral. This
applies to inventory, skills, passive nodes, creation classes/masteries/realms,
quest details and ordinary menu controls with associated descriptions. Quests
retain their current objective and state in the brief announcement.

The gesture matches the local, unreleased Diablo 2 mod's
`src/interaction_reader.cpp:ReadDetail`: assigned Rewired gamepad template
`rightStick.value/valuePrev`, half normalized travel, upward dominant direction.
Holding or rotating the stick does not repeat until neutral. No native mapping,
scrolling or action dispatch is changed.

Every request resolves current ownership again. Hidden controls, foreground
modals, loading and mismatched tooltip identities cannot replay old content.
Manual reading acknowledges the brief selection so the next poll does not
interrupt it. Native tooltip expansion or a late completed tooltip no longer
triggers automatic skill narration. Manual skill inspection reads fresh visible
stats each time; automatic reading does not traverse the tooltip.

Missing visual item details use the [native presentation model](item-tooltip-model.md)
for the selected item. Automatic item navigation reads its localized name,
base type, equipped state for declared `ContainerID.EQ_*` slots, merchant price
and available native actions. No success is inferred merely from an empty cell.

## Follow-up on Steam 1.5.1 / build 25672295

The 15:23 session still had silent passive nodes, incorrect Ráfaga assignment,
missing item actions and unavailable shop tooltip models. The earlier fixes were
insufficient. See [passive evidence](passive-reading.md),
[skill identity and unlock state](skill-assignment.md) and
[merchant reading](merchant-reading.md) for the corrected sources.

`InventoryActionReader.ReadForItem` now enumerates the game's read-only
`ItemTooltipPromptHelper.GetActions` presentation model for the exact selected
item and slot. Its current native iterator is `0x2016AF0`: it constructs localized
PanelActionPrompt records and queries availability; the reader never invokes an
action callback. It filters `CanUse` and current-device `Controls`, respects
explicit native button overrides/hold semantics, and otherwise resolves current
action bindings through PromptsInfoProvider. Enumeration uses the IL2CPP generic
current-item interface plus nongeneric MoveNext, disposing the iterator.

Speech puts the button first, e.g. “Triángulo, Equipar. Cuadrado, Tirar”, using
actual mappings rather than assuming these defaults. Every new item repeats its
own actions. Context/price/action refresh is limited to once per 200 ms while
focus stays put; full property models still require right-stick-up. Selection and
slot ownership are checked before and after reading. Empty cells clear context.
Legacy tooltip prompts are excluded from the separate menu hint queue so those
actions do not repeat later or leak from uninitialized prefabs.

The skill brief includes learned/unlearned state and declared level/passive-point
requirements. Equipped comparison uses the game's explicit primary-ability
relationship, fixing bow/combo variants without matching localized names.

## Skill assignment

Historical build 25663618 evidence (session ending 13:01:22) showed Ráfaga alternating between
equipped and not equipped at lines 821–834. The HUD's current combo attack is
not the same source as the actual assigned skill:

- `AbilityBarIcon.UpdateIcon`, RVA `0x23CF100`, invokes virtual
  `getAbility(abilityNumber - 1)` at `0x23CF244`, taking the current attack.
- `ComboAbilityList.getEquippedAbilities`, RVA `0x498CB0`, returns the
  separate assigned list. `PlayerAbilityList.getEquippedAbilities` also appends
  the inactive human-form bar during transformation, so using its concatenated
  indices would give incorrect current buttons.
- `AbilitySelectionIcon.clicked`, RVA `0x24216F0`, gets the current player's
  actor/ability list, verifies the selected ability is available, and assigns
  it at `AbilityBarSelectorManager.abilityClicked - 1`. The sixth slot is
  reserved and does not accept this assignment operation.

The reader now follows `PlayerFinder.getPlayerActor().GetAbilityList()`,
uses `ComboAbilityList.equippedAbilities` for that type, and compares declared
primary ability pointers. Localized-name matches cannot establish identity. Missing
authoritative data stays unknown instead of announcing “not equipped”.

Slots 1–5 use named `EpochInputManager.InputActions.Ability1`–`Ability5`
members, never arithmetic on action IDs. The game's prompt provider supplies
current remapped buttons even when HUD icons are unavailable.

The selected hotbar slot announces its actual assigned ability or empty state
and button. The available-skill grid announces “Assign to” with the destination
from the live manager, requiring the same player and declared visible grid.
Manual details explain confirming the slot to open choices, then confirming a
choice to assign it. This reader does not equip or submit anything.

## Equipment rejection and action hints

In build 25663618, native `ItemContainersManager.TryQuickEquip` (`0x140A480`) routes failures
through `InventoryPanelUI.OnFailedManualEquip` (`0x17967F0`). That method
localizes the actual reason and calls `Notifications.FailedToEquipItem`
at `0x1796D70`. The existing notification reader observes displayed notices.

The caller previously suppressed notifications for the entire time that
inventory/skill/passive/quest readers had active content. It now yields only
while their speech is pending or another reader spoke that frame, preserving
modal/dialogue priority. Thus a visible rejection can be read while the menu
stays open. Native equipment rules and Triángulo/Y actions remain game-owned;
the prior log does not identify which rule rejected a particular attempt.

Legacy `MultiButtonsActionPrompt.IsPromptActive` checks device/navigation,
whereas `CanUse` controls actual availability and indicator color. The reader
now requires both. Modern `PanelActionUIPrompt` already requires its native
action's `CanUse`, visible owned label and current input mapping/overrides.

The log's 15 bogus Gladius actions also came from an unprepared legacy tooltip.
Installed `PermaLoad.bundle`'s `UITooltipItem` (4485364037099581161) starts
`tooltipActive=false`, but its `ItemTooltipPrompts` (79123104355113053) and
all 30 action objects start active. Many allow inventory navigation. The native
prompt constructor (`0x107DEF4`) also starts `CanUse=true`.
`TooltipItemManager.ProcessInitializationStage` sets current prompts at
`0x14907B0`. Its show-all, per-action update, and hide-inactive calls are
synchronous; an arbitrary delay would not establish ownership.
`InventoryPromptReader` now requires the completed, visible primary tooltip,
exact prompt owner, selected item identity, and opener inside the focused
container. Initial prefab, comparison, previous-item and empty-slot prompts are
excluded before their button bindings are read.

## Validation

Source-linked tests cover brief/manual separation, late/changed tooltips,
repeated deliberate reads, current-node point changes, stable combo assignment,
empty slots, current versus inactive transformation bars, remaps, rejected
ownership and current inventory status. See [STATUS.md](../STATUS.md) for the
final integrated counts/build. Real controller and listening validation remains
pending; no game was launched.

Inventory automatic and manual feedback now acknowledge content only when
`ScreenReader.TrySpeak` accepts output. A failed manual gesture retains only its
selection-bound intent and rebuilds current properties after recovery; changed,
hidden, collected or unready ownership and resets discard it. Automatic retries
yield to accepted output in the current frame. The inventory suite passes 219
checks, including 12 acceptance/retry regressions; see
[the centralized contract](screen-reader-lifecycle.md).
