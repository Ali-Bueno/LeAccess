# Equipped skills and inventory action buttons

## Manual details (2026-10-02)

Right-stick up now rereads the selected inventory item or skill, including skill
tree nodes, without requiring a selection change. This mirrors the local D2R
mod's `src/interaction_reader.cpp:ReadDetail` gesture. `MenuDetailInput` reads the
assigned Rewired gamepad template's `rightStick.value` and `valuePrev`; the
half-travel threshold matches D2R's explicit gesture policy on normalized axes.
Holding or rotating the stick does not repeat speech until it returns below the
threshold. Native input mappings and native scrolling are unchanged.

The existing readers resolve ownership again on every request: hidden menus,
modals, loading, empty slots and mismatched tooltips cannot replay the previous
selection. A skill request refreshes bindings and the displayed combat-stat
snapshot; ordinary focus reading continues to suppress fluctuating DPS. Both
readers acknowledge the manual read to avoid an immediate automatic duplicate.
Input and source-linked reader regressions cover those behaviors. Real controller
and listening checks remain required; the game was not opened for this change.

When an inventory tooltip cannot be read, explicit inspection falls back to the
[native item presentation model](item-tooltip-model.md). This includes the item's
localized properties and requirements without opening UI. Ordinary focus polling
does not build models. Its brief-name fallback stays acknowledged so it cannot
interrupt the manual body on the next frame. Failed manual reads give localized
unavailable feedback. Validation: 72 inventory service, 50 item-model, 109 skill
and 36 combined vitals/menu-input checks pass.

## Equipped status and actions

Skill announcements now distinguish equipped skills from available skills and
say the current assigned button after the skill name. Matching uses the current
player's `AbilityBarIcon.all`, including `correspondingAbility` for transformed
skills. An unavailable HUD produces no unassigned claim. Equipped status remains
readable even if button information is temporarily unavailable.

The old binding reader depended entirely on typed child prompts. It now also
resolves a glyph-only icon's native `abilityNumber`: `AbilityBarIcon.UpdateIcon`
(installed RVA `0x23CF100`) reads the equipped ability at `abilityNumber - 1`.
Slots 1–5 map to the named `EpochInputManager.InputActions.Ability1`–`Ability5`
actions, then `ReInput.mapping.GetAction(id).name` supplies the actual action
name. The enum IDs are not assumed consecutive. Unsupported numbers remain
unknown. Existing child prompt ownership and cast-action validation remain.

Shared `InputBindingText` reads the game's current `PromptsInfoProvider` mapping;
there are no fixed Xbox slot/button assignments. Binding groups retain their
`+` chord separators. The existing 0.2-second focused binding refresh observes
remaps and reassignment without replaying unchanged skill details.

Modern inventory item reading now includes its visible usable
`PanelActionUIPrompt` action labels and current buttons. This happens only after
`ItemTooltipReader` validates the active item, opener/container, readiness, and
bound tooltip-info identity. Hidden pooled prompts and unavailable actions are
excluded. Explicit controller/keyboard overrides take priority over ordinary
action mappings; hold actions retain hold speech.

Native `PanelActionUI.Refresh` (RVA `0xA6AD20`) chooses overrides using the active
device and otherwise calls the provider with `GetActionName()` and
`showKeyboardGylphs`. Its `BuildOverrideButtonsInfo` (RVA `0xA6A720`) only allocates
button data and resolves icons; the reader never refreshes, submits, or executes
an inventory action. Legacy MultiButtonsActionPrompt hints use the shared general
prompt announcer instead of another item-specific copy.

The focused source-linked suites pass **100 skill checks** and **64 inventory
checks**, including absent child glyph prompts, live remaps, transformed abilities,
explicit equipped status, device/override changes, hold actions, and stale pooled
tooltip rejection. The shared binding decoder has separate tests. The main
no-deploy build passes without warnings/errors. The game remained closed;
controller listening remains the validation boundary.

## Late tooltip rendering

The subsequent user log showed a base skill description followed by the same
selection's completed tooltip, which replayed the full name/description and
interrupted the first read. The first late tooltip presentation now appends only
new sentence fragments with `interrupt:false`, then acknowledges the complete
rich snapshot for deduplication. The name, equipped button and existing
description remain immediate; added definitions and stats are not discarded.

The existing native `UITooltipAbility.activeInitializationStage == Completed`
gate still rejects partially rendered content. No guessed tooltip delay or new
hook was added. The new delta behavior applies only to the first asynchronous
enrichment of the same already-spoken name/binding. Deliberate expansion, real
stable detail changes, remaps and reselection retain their existing behavior.
Combat-stat snapshots still freeze fluctuating DPS. Regression tests cover a
tooltip appearing several frames after base speech, incomplete initialization,
noninterrupting complete additions, punctuation/decimal preservation, hidden/
restored tooltips, explicit expansion and full rereading after reselection.
