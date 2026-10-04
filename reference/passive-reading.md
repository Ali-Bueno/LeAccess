# Passive node reading — Steam 1.5.1

Originally implemented against build 25663618; rechecked against installed Steam
1.5.1 build 25672295 after the 15:23 user log still showed silent passive nodes.
No game was started and no native hooks were added.

`PassiveSelectionReader` follows `NavigationManager.currentlySelected` when
it is a visible `PassiveTreeNavigable`, or the visible modern
`FullScreenPanelPassiveTree` whose exact `ActiveChildNavigable` is that tree.
It casts the erased navigation array to
`Il2CppSystem.Array`, validates rank and actual dimensions, and reads
`[currentColumn,currentRow]`. Native `PassiveTreeNavigable.SelectNode` RVA
`0x105D990` and `ShowTooltip` RVA `0x105DCE0` confirm this coordinate order.
`ShowTooltip` obtains the selected GameObject's `SkillnodeTooltip`; the reader
uses its `node` only when it belongs to that same GameObject. Native
`SkillnodeTooltip.OnPointerEnter` RVA `0x17EC2D0` reacquires `GetComponent<SkillTreeNode>`
if its stored node is absent or belongs to another object. The reader follows
that same-object lookup without writing the native cache, and rejects nonpassive
nodes, unavailable trees and stale wrappers. Array entries can be GameObjects
or Components; Components resolve through their own GameObject.
No native addresses or offsets are embedded in the implementation.

The current `LE.UI.Tooltips.TooltipSystem` comes directly from
`UIBase.instance.TooltipSystem`, without scene scans. Its visible, enabled,
layout-ready tooltip's bound `_info` must equal `_activeNodeTooltipInfo`, and
both `ActiveNodeTooltipNode` and the bound info's `Node` must match the selection.
Old, disabled or unfinished tooltips cannot
supply another node's text. The fallback is a cached
`NodeTooltipContentBuilder.GetNodeTooltipInfo(node,false)`; it does not open a
tooltip or change selection. Current native builder RVA `0x200D580` obtains the
localized name (`0x200D7BA`), writes it at `0x200D7CC`, populates description,
point-bonus description, modifiers and requirements, then returns its own model.
Fallback rebuilding occurs on selection,
allocated/max points, requirements-met state, tree unspent points or game locale
changes; held selection does not rebuild lists each frame.

Navigation speaks localized name, allocated/max points and requirements-met state.
Right-stick-up calls `ReadCurrentDetails()` for description, stat modifiers,
point-bonus description/modifiers and requirements. These use `NodeTooltipInfo`
and the game's `ToModifierString`/`ToRequirementString`, preserving its localization
and scaling logic. `NodeTooltipRequirementInfo.IsMet` supplies explicit spoken
met/unmet state: native `ToRequirementString` otherwise represents
this distinction with rich-text color, which speech cleaning removes.
Only the points template and requirement-state labels/wrappers are mod translations.
Requirement status does not imply that the player has unspent points or can afford
a respec. Manual delivery acknowledges the current brief so polling cannot
interrupt the requested details on the next frame.

`PassiveAccessibility` uses two stable `ScreenContentTracker` observations,
interrupts on delivered selection changes, and avoids repeat speech while held.
Loading, loss of application focus, modal content, dialogue and controller radial
menus have priority. Owned node focus bypasses generic control reading. Pending
speech remains queued when another announcement already used the frame.
Leaving the panel releases reader caches; reopening refreshes fallback data even
when the selection's counts have not changed. Reset also releases state on teardown.

The earlier 13:01 runtime log contained focus events for `Sprite` under
`PassiveMasterySkill` (lines 1433–1475), with no `[Passive]` delivery. Native
`FullScreenPanelPassiveTree.HandleNavigateInput` (current RVA `0x201FD90`) delegates to
`ActiveChildNavigable`; its getter RVA `0x4AFE10` reads the same field. Requiring
the outer selection itself to be `PassiveTreeNavigable` missed this current
fullscreen route. `PermaLoad.bundle` contains the enabled fullscreen component
at path ID `3378973033484829089`. The fix follows its declared active child,
without hierarchy assumptions or scanning for inactive trees. A focused header,
search field, or other child does not reuse a previously selected passive.

The 15:23 log still contains selected `Sprite` events under `PassiveMasterySkill`
(lines 1598 and 1611) with no passive delivery. The follow-up found a concrete
fallback bug: `new NodeTooltipInfo(...).Refresh()` never fills `Name` or
`Description`. Constructor RVA `0x200F900` sets only node, alt flag and lists;
`Refresh` RVA `0x200F290` calls only the modifier and requirement refreshers.
The old fallback therefore always failed the nonempty-name guard. Its test double
incorrectly synthesized `Name` from the node and concealed this. Tests now model
the empty constructor and separate populated builder explicitly.

Native evidence was regenerated from the installed interop at
`%TEMP%/LeAccess-passive-native-20261002-1523`: Steam updated the assembly at 15:04
and regenerated interop at 15:05, so earlier build-25663618 RVAs must not be reused
against this executable. Production reads typed API members rather than addresses.

`[PassiveStatus]` reports changed rejection stages or the successful content source,
at most once every two seconds and only within a passive context or on actual
passive-node focus. It identifies navigation, array/rank/index, cell, same-object
node, tree, builder and name failures. Stable failures do not repeat. These logs
are needed to distinguish any remaining runtime failure from this proven model bug.

Validation: 82 source-linked checks exercise the actual reader and announcer:
coordinates, allocated points, cached refresh, absence of scans, owned focus,
stable/deduplicated/interrupted delivery, priority gates, bounds/rank, visibility,
collected wrappers, nonpassive nodes, tooltip bound/active identity/layout,
disabled nodes/tooltips, reopen cache refresh and requirement state. The new
cases cover modern panel ownership, hidden/disabled/foreign/missing active
children, brief/manual separation, requirement changes, manual reads before
pending brief delivery and priority/close guards. Follow-up cases cover the empty
native constructor, populated builder, missing/foreign tooltip-node caches without
mutation, Component cells, locale-dependent static fields, failed builder/recovery
and bounded failure-stage diagnostics.
The follow-up builds with zero warnings/errors using `SkipPluginDeployment=true`;
it was not launched or deployed by this feature investigation. No new dependency.

The 2026-10-03 review found that a fallback built before its native name became
available was cached permanently until points, locale or selection changed.
An unnamed fallback is now discarded and retried at the existing 0.2-second
inventory/skill refresh cadence. A regression first failed with a name arriving
later on the same node; recovery and bounded retries now pass in the 85-check
passive suite. Production builds without warnings/errors; listening remains pending.

The additional service audit reproduced a retry bug that the reader-only test
missed: `PassiveAccessibility.Update` reset the reader after every pending name,
discarding its retry deadline. Pending empty/null models now retain that deadline
while speech ownership is released; closing the tree still clears the cache.
Before speaking, automatic/manual output rechecks the current foreground and
node identity after model construction. The source-linked suite passes 95 checks,
including six foreground/selection changes during native-model construction.

Controller listening remains pending: open Passives, move between nodes, compare
name/points/requirements with the shown tooltip, allocate a point and switch
mastery tabs. Check that reopening reads the current node and that respec/modal
content interrupts the node reader. The reader does not implement new controller
navigation or synthesize point spending. Dynamic requirements changed by outside
systems without the observed node/tree state changing need runtime confirmation;
the displayed owned model remains preferred when available.

The output-acceptance follow-up acknowledges passive summaries only after
`ScreenReader.TrySpeak` succeeds. Rejected or unavailable manual output retains
only the current node's request intent, then rebuilds details after recovery.
Hidden, collected, unready or changed ownership and resets discard it; automatic
retries yield to accepted output in the current frame. The passive suite passes
107 checks, including 12 acceptance/retry regressions. The
[centralized contract](screen-reader-lifecycle.md) records the managed-only test
boundary and native acceptance semantics.

## Trees, mastery and the skill-unlock strip (2026-10-04)

User report: no way to tell whether points follow the chosen mastery, and "Sprite" spoken.
`PassiveTreeReader` adds, from the fullscreen panel's own fields:

- **Opening the panel:** every tree with its points, base class / chosen mastery /
  locked, and "Sin especialización elegida" when `_localTreeData.chosenMastery` is 0.
  Points come from each tab's visible `TabButton._assignedPoints`, else
  `_localTreeData.masteryLevels[i]`; locked from a visible `_lockedPoints`.
- **Switching trees:** the visible `_masteryNameText`, `_pointsAllocatedText`, role and
  `_pointsAvailableText` precede the first node. Native `UpdatePoints` (0x2024350) writes
  `masteryLevels[_currentMasteryIndex]` into `_pointsAllocatedText`, and `GetActiveMastery`
  returns `_currentMasteryIndex`, so tab index equals mastery index (0 = base class).
- **Node threshold:** a node of the displayed page whose `masteryRequirement` exceeds the
  tree's points adds "Requiere N puntos en este árbol, tienes M".
- **Unlock strip** (`PassiveTreeUnlockPanelNavigable`, the old "Sprite" under
  `PassiveMasterySkill`): `ActiveAbilityTooltips[SelectedIndex]` gives the skill name and
  description. State mirrors `MasteryBarDisplay.ApplyUnlockedAbilityStates` (0x17C1B70):
  `points >= requirement` unlocked, `requirement <= maxPoints` reachable, else beyond the
  tree's maximum; points/max are the bar's `_lastFillPoints`/`_lastFillMaxPoints`.

The overview speaks again only after the panel closes; modals inside it keep the tree
context. Passive suite: 123 checks. Listening pending, in particular what
`_lockedPoints` means to players and whether unsaved allocations change tab points.

## Controller panel, reading order and tree switching (2026-10-04)

User report: switching tabs cut off the tab's announcement; the order "skips nodes" and gives no
sense of first, middle or last. The 00:46 log ran the tree build above (`ReadOverview` is in the
installed DLL of that hour) yet never spoke a tree or the strip: with a controller the game opens
the **old `PassivePanelManager` panel**, whose banner, strip and trees `NavigationManager` selects
directly, not `FullScreenPanelPassiveTree`. Native evidence, build 25690669:

- **Grid:** `PassiveTreeNavigable._navigationElements` is `SkillTreeNode[11,5]`, authored by Odin in
  each mastery page prefab (the ctor @0x105e1e0 only allocates it). Column 0 is left; row 0 is the
  **bottom** (Up adds 1). Columns follow point requirements (`UnlockPanel.getPassiveTreeIndex`
  @0x105f1a0). Cells can be empty; nothing prevents a node in two cells.
- **Native moves** (`HandleNavigateInput` @0x105c980; `VerticalMoveToClosestNode` @0x105df10,
  horizontal mirror @0x105ca30): a press keeps scanning the *same* column (vertical) or row
  (horizontal) to the edge and only falls back to nearby lines when that line is empty, so closer
  nodes in neighbouring rows/columns are skipped. Borders (`IsOutBorder` @0x105ccf0, old panel):
  below row 0 → skill strip at (column, 0); above row 4 → top buttons (or the lower respec row);
  left of column 0 → banner (its right diamond); right of column 10 wraps to column 0.
- **Re-entry** (`OnSelected()` @0x105d3e0): from the top buttons → rightmost node of the top row;
  from the strip (`selectSkillTreeWithIndexAndDirection`) → the cell under the skill's requirement;
  otherwise the remembered cell, or top of column 0. No auto-repeat: d-pad steps on release, the
  stick once per flick (`PassivesMap`).
- **LB/RB** (`PassivesMap` MoveNext @0x103b750) in one frame selects the banner (focus 1: current
  diamond), rotates focus (focus 2: next diamond), then submits; `MasteryBanner.Clicked`
  @0x17a22e0 sets `PassivePanelManager.activeMastery` and loads the page asynchronously. The page's
  navigable `OnEnable` selects the tree and its first node interrupts the diamond speech; the
  tree is then selected once more after `UniTask.Yield`.

Mod behaviour:

- `PassiveTreeNavigation` prefixes `HandleNavigateInput`/`HandleLeftStickInput`: the grid becomes one
  list (columns left to right, each top to bottom; duplicates and inactive nodes skipped). Down/Up =
  next/previous node across columns; Right/Left = top node of the next/previous column. List ends
  place the cursor on the matching grid border and take the game's own step, which leaves (Up → top
  buttons, Down → strip, Left from the first column → banner); Right on the last column repeats the
  node. Selection mirrors native: hover exit on the old node, set column/row, `SelectNode()`. The
  prefixes never run the original body: called through its detour it crashes
  ([dump](runtime-feedback.md#passive-navigation-crash-detoured-short-branch-prologue-2026-10-04)),
  so they replay its dispatch to `VerticalMoveToClosestNode`/`HorizontalMoveToClosestNode`.
- `OnSelected` postfixes (old panel, controller only): from the top buttons land on the first node,
  from the strip on the last. Other entries keep the native landing.
- Nodes and strip skills end with "N de M".
- `PassivePanelReader` gives the old panel the same context as the fullscreen one: overview on
  opening, "tree, points, role, unspent" before the first node of a newly shown tree, unmet node
  thresholds, strip unlock state from the shown tree's points. Name: `MasteryReader.ReadName`
  (diamond `MasteryButtonTooltip`); points: visible diamond `levelTMP`, else
  `Actor.localTreeData.masteryLevels`; chosen mastery: `localTreeData.chosenMastery`; tree index:
  the `CharacterTree.masteries` page containing the navigable; unspent: `activeTree.unspentPoints`.
- Banner diamonds are owned focus (silent at focus time). While the banner is selected and the
  page of `activeMastery` exists but is inactive, nothing speaks; the new tree then speaks once with
  its first node. A deliberately focused diamond reads its tree (plus "Árbol mostrado"); details read
  the mastery description. The scrollable description stays with the generic reader.

Passive suite: 153 checks (order planning, moves/exits/stay, stick, re-entry gates, banner, switch
silence, controller strip, empty selection, reopen). Patch audit: 70 declarations / 69 targets, no shared bodies.
Runtime unverified: one navigable per page, `levelTMP` being a bare number, lock-icon meaning,
the top-buttons/strip previous-ID values seen by `OnSelected`, and how the fullscreen panel's own
exits feel with the list order (its exits are left native).
