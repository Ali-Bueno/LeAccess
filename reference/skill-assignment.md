# Skill assignment, variants and learning state

Inspected installed Steam **1.5.1**, build **25672295**, on 2026-10-02, with the game closed.
The afternoon update replaced `GameAssembly.dll` and interop at 15:04/15:05 local;
earlier RVAs from build 25663618 must not be used with that binary. Current native
evidence was regenerated from `MethodAddressToToken.db` and installed interop.
These readers add no Harmony patches.

## Stable assignment and declared variants

The 15:23 session log reports `Ráfaga. No equipada` at lines 742/744 while its
action-bar selection later reports `Equipada: Square`. Exact pointer matching
between the available-skill definition and the equipped weapon variant was too
strict. Names and `playerAbilityID` are also unsuitable: installed NPC attacks
reuse player-facing names and IDs.

Installed `PermaLoad.bundle` contains base Flurry at path ID
`6789334992585611593`, Bow Flurry at `308069841593509950`, and Flurry Hit 1/2/3 at
`8091280544139542862`, `8067033514301248187`, `8025428553892283101`.
Bow Flurry and those three hits declare `overridePrimaryAbility = true` with the
same `customPrimaryAbility` reference key `316272423`. The base and bow assets
also declare each other as weapon-condition replacements. These constants are
evidence only; none are hardcoded in production.

Current `Ability.GetPrimaryAbility` at RVA `0x23D38A0` consults the declared
`AbilityRef.TryGetAbility`, then silently returns the variant itself if resolving
fails. `SkillIdentity` reads the same declaration but preserves unresolved
references as unknown. `SkillBindingReader` compares resolved primary pointers;
an unresolved reference or uninitialized `ComboAbilityList` cannot produce a
false “not equipped” claim.

### First-opening recovery (19:40 session)

The preserved `%TEMP%/LeAccess-skills-first-open-20261002/BepInEx-194015.log`
shows the first class-panel opening at lines 537–559: learned state and level
requirements are spoken, but every equipped/unassigned status is missing.
The next opening at 625–638 includes `Equipada: Square`, `Triangle`, `R1` and
`L2`, and unassigned states. The old log has no binding-readiness diagnostics;
it establishes the first-opening omission, not which internal guard failed.

Review reproduced a concrete initialization defect: `TryReadEquipped` rejected
the entire slot snapshot when any entry's primary reference was unresolved.
Thus one pending variant hid a different, already known equipped ability and
even prevented a known empty hotbar slot from being selected by the reader.
The UI already refreshes bindings every 0.2 unscaled seconds; waiting for a
second opening is neither required nor an appropriate new trigger.

Snapshot ownership and initialization checks remain. Primary resolution now
belongs to matching individual entries. A matching native ability pointer, or
matching resolved primary pointers, proves a positive assignment independently
of other pending slots. A negative assignment still requires complete identity
information. The selected hotbar slot uses its actual entry, independent of
other slots' primary metadata. No names or guessed IDs are used as identities.

Native evidence was rechecked against unchanged `GameAssembly.dll` SHA256
`502E32F1F31BC1979AC6C387FAEF1266A62AD29F5B573E6D9B51CB577A72F5F8`:
`AbilityRef.TryGetAbility` (`0x2424B00`) invokes `GetAbility` (`0x2424820`), which
consults `AbilityManager.GetAbilityFromKey` (`0x23F0860`) when its cached reference
is unavailable. That resolution is distinct from the already stored equipped
entry. `ComboAbilityList.HasInitialised` reads its separate initialization flag;
`initialise` (`0x2445EC0`) sets it before invoking combo initialization. Neither
initialization nor equipment mutation is invoked by the accessibility reader.

`[Skill binding]` now reports a changed readiness reason once, and `ready` once
when recovered: player/actor ownership, absent list, combo initialization,
missing slots and unresolved primary identity remain distinguishable. It does
not log the same pending condition on each refresh. This is needed to identify
any additional native gate if the user's next first opening still omits status.

Lifecycle tests cover first opening with unrelated pending metadata, in-place
resolution and combo initialization without closing, deduplicated speech and
diagnostics, second opening, and a close before resolution (no stale speech).
Static tests prove the corrected dependency; a fresh runtime first opening
still needs confirmation because the previous log did not identify its gate.

The source of equipped slots is the current player's
`Actor.GetAbilityList()`, checked against the current player object.
`ComboAbilityList.equippedAbilities` retains actual slot assignments; native
`setAbility` (`0x2446B90`) writes the supplied ability into this list. The HUD's
`AbilityBarIcon.UpdateIcon` (`0x23D2780`) obtains `getAbility(abilityNumber - 1)`,
which may display the current combo attack. `PlayerAbilityList.getEquippedAbilities`
(`0x245BE70`) can append the inactive human-form list while transformed, so its
concatenated positions must not be interpreted as current cast slots.

Slot-to-action mapping uses the game's five named `InputActions.Ability1` through
`Ability5`, then the existing live `InputBindingText` provider. Matching owned HUD
prompts remain supported, but a missing/hidden HUD registry does not erase actual
assignment. `TryReadSlot` returns success with null for a known empty slot;
unavailable or invalid slots return false.

## Assignment menu context

`AbilityBarIcon.clicked` (`0x23D2D00`) writes its one-based `abilityNumber` into
`AbilityBarSelectorManager.abilityClicked`, then opens the available-skill menu;
the reserved sixth slot is excluded. `AbilitySelectionIcon.clicked`
(`0x2424D70`) validates membership in available skills, obtains the player actor's
ability list, and calls `setAbility` with `abilityClicked - 1`. Native cancel
closes the flyout through `AbilityBarSelectorManager.CloseMenu`; it does not equip.

`SkillAssignmentReader.TryReadContext` returns `Hotbar` or `AvailableSkills`,
the destination slot, current occupant and actual binding. Available-grid context
requires the current player's open manager and containment in its exact
`_gridNavigable`. This supports speech for both occupied and empty destinations.
The current selected ability still supplies the name and manual tooltip.

## Learned state and requirements

Current `AbilityPanelIcon.Init` (`0x2420FB0`) sets `locked`, fills
`UnlockReqTMP`, and selects a requirement group using `AbilityUnlockType`.
`SetAbilityLockedState` (`0x24214F0`) updates the learned/locked frames and hides
the requirement holder for learned skills. Hiding the holder does not erase the
assigned requirement. `CharacterClass.getAbilityUnlockString` (`0x1691E00`)
returns the matching `AbilityAndLevel.level` as a number, not a full label.

`SkillLearningReader` reads the selected icon's `locked` state and owned numeric
field even when that holder is hidden. Base-class values are required character
levels; mastery-point values remain passive-point requirements; mastery selection
is announced without inventing a level. Initialized current-player
`KnownAbilityList.abilities` provides learned state outside class icons, matching
the same declared primary identities; its current character class's
`unlockableAbilities` supplies base-class required levels there. A proven current
slot/flyout takes precedence over a stale known-skill cache. Empty/unavailable
known data stays unknown unless the current assignment proves usability.

Navigation announces name, equipped button, learning state, requirements and
assignment destination. Descriptions/stat bodies remain manual right-stick-up.

## Validation

`tests/LeAccess.Skills.Tests` links production sources and passes **171 checks**.
Relevant cases cover declared bow/melee/hit primary relations, same-name unrelated
abilities, unresolved references, incomplete combo initialization, transformed
lists, remapping, missing HUD, empty destination, exact grid owner, learned/locked
changes, hidden owned required levels, mastery points and manual-only descriptions.
Main build passes with `SkipPluginDeployment=true`. Runtime listening still needs
the user's check: Flurry in the class panel, available grid and action bar should
agree on the assigned button; learned/locked skills should state their true
requirements, and right-stick-up should remain the only description trigger.

## Square carries the highlighted skill (2026-10-04)

User report: "I press Square to assign the skill to a button, but on the button the assign screen
opens again." Native flow, build 25690669: in the Skills panel Square is `SubmitType.X_SQUARE`,
the game's "Cambiar habilidades de la barra de acción" (UI key `Controller_Menu_Prompt_SwampActions`).
`SkillsPanelManager.HandleSubmitInput` @0x2491b30 calls `UIBase.closeSkills(true)` and selects the
HUD action bar (`NavigableID.ABILITY_PANEL`, index 0) without reading the highlighted skill; no
pending skill exists anywhere. Cross on a bar slot (`AbilityBarIcon.clicked` @0x23d39d0) always runs
`AbilityBarSelectorManager.OpenMenu` @0x23e8fd0, which selects the flyout list
(`ABILITY_PANEL_FLYOUT`, a `UnityDrivenGridNavigable`) on its first skill; Cross there assigns and
returns to the slot. Square/Triangle do nothing on the bar or the list (`AbilitySelectionMap`).

`SkillSwapCarry` (prefix on `SkillsPanelManager.HandleSubmitInput`, safe prologue) remembers the
highlighted skill on Square. While the bar is selected its slots add "Confirma dos veces para
asignar X". When the flyout is selected and its `_selectableChildren` are filled, it calls the
list's own `TrySelectElementWithIndex` on the matching `AbilitySelectionIcon`, before the first
entry is spoken, so Cross then assigns it. Any other selection drops the carried skill; a skill not
in the list leaves the game's focus. Skills suite: 211 checks.
