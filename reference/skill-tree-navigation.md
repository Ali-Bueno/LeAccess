# Skill tree navigation with a controller (2026-10-04)

User request: the same list navigation as the passive tree for the active skills' specialization
trees. Static analysis, build 25690669 (old `SkillsPanelManager` panel; the fullscreen
`FullscreenPanelSkillTree` opens only with the `visual.skills_panel_overhaul` feature flag, which
defaults to off):

- **Native move:** d-pad `HandleNavigateInput` @0x2491200 sets `currentlySelectedIdx.y = 0` and
  calls `ProcessLeftStickInput(vector)` @0x24935e0; the stick calls it directly. Inside an open
  tree (cursor not in the top 900/1080 of the screen, `inTopArea`) it picks, among
  `CurrentlyOpenTree.nodeList`, the node within 50° of the direction nearest a point 50 units ahead.
  No order exists, lines zig-zag and some nodes may never win. Only Up exits (to the
  specialization row) when nothing is found.
- **Blocked moves:** the cleared `y` stays 0, so A then acts on a specialization slot instead of the
  node (`HandleSubmitInput` @0x2491b30 clicks the node only when `y != 0`).
- **Opening:** `OnOpenSkillTree` @0x2492fc0 instantiates the tree into `CurrentlyOpenTree` but stores
  the *prefab's* `nodeList[0]` as `currentlySelectedNode`, so the cursor, tooltip and the mod's
  reader (which requires `node.tree == CurrentlyOpenTree`) start on a node outside the tree.
- **Native selection:** `currentlySelectedNode = node; FocusTreeNode()` (places the cursor, sets
  `y = 1`); `StartCoroutine(ShowTreeNodeTooltip())`.

Mod behaviour (`SkillTreeNavigation`, prefix on `ProcessLeftStickInput`, postfix on
`OnOpenSkillTree`; both prologues have no relative branch):

- The open tree's active nodes form one depth-first list from the root (`nodeList[0]`): each node
  is placed under the first node of its `requirements` that is in the tree, otherwise under the
  root; siblings read top to bottom, then left to right (world position). Each root child starts a
  branch. Since the grid request (below), Down/Up stay inside the branch and Right/Left go to the
  neighbouring branch at the same place; ends stay and repeat the node, restoring `y = 1`. Stick
  flicks use the dominant direction (`DirectionExtensions.ToDirection`).
- In the top area or with the tree closed the game's handling runs unchanged; B leaves the tree and
  LB/RB switch specialization trees as before.
- A prefab node maps to the open tree's node with the same `id` for reading and moving; after
  opening, with a controller, the open tree's node is focused the native way.

Skills suite: 204 checks. Runtime unverified: whether any node sits in the top area (the native
trap remains there), the layout and branch order as heard, and the feature flag value.

## Skills overview grid (2026-10-04)

User report (log 2026-10-04 15:05–15:18): the d-pad "jumps", reaching 30/35-point mastery skills from
the base class, and sometimes acts outside the menu. Static analysis, build 25690669 (the 15:18
Steam update to 25711439 moved every `SkillsPanelManager` body by +0x7a0 with identical sizes):

- **Layout:** `selectables`/`workingRects` are `[11,4]`, y=0 on top (prefab `rects`, Odin data in
  `assets_d07a5d00599ec412.bundle`). Row 0: tree back button, five `SpecialisedAbilityIcon` slots,
  respec/specialise/stop buttons (active only with a tree open). Rows 1–3 each hold base-class
  icons on the left (x0–4: two rows of `baseClassPanelIcons`, then `baseClassTreePanelIcons`) and
  one mastery on the right (x5–9 skills, x10 its mastery skill, slid left over hidden slots).
  `defaultNavigationIndex` = (0,1).
- **Native move** (`HandleNavigateInput`, tree closed): every step wraps (Right from x10 to x0, Up
  from row 1 to row 3, Down from row 3 to row 0), Right from base x3 enters mastery 1, Down picks the
  nearest cell at or left of x and skips a row whose pick is hidden. Up from a slot (x≥3) selects
  navigable 121 (`SKILLS_UPPER_BUTTONS`, search). The d-pad fires on release; the stick does
  nothing on the overview.
- **Focus:** `currentlySelectedIdx = (x,y); FocusSelectable()` is the native selection (tooltip
  via `OnPointerEnter`, cursor); the native move first clears `EventSystem` selection and
  `HideIconTooltip()`.

Mod behaviour (`SkillPanelGrid`, prefix on `HandleNavigateInput`, first relative branch at +0x13):
rows as heard are the specialization slots, the base class (all its icons in native reading order),
then masteries 1–3 (membership from the panel's own lists); hidden cells are skipped. Right/Left
step inside the row, Down/Up go to the next/previous row at the same place (clamped); ends never
wrap and repeat the skill. Up from the slots keeps the game's move to the search row. Entering a row
speaks its name first (`baseClassName`, `masteryNClassName`, "Casillas de especialización"), and
every skill ends with "N de M" in its row. Empty/locked slots now speak instead of staying silent
(locked adds the slot's visible level text). Specialization trees use the same rule by branch
("Rama N"), and the passive trees by column ([passives](passive-reading.md)); shared planner
`GroupedGridOrder`, entry tracking `GroupEntryTracker`.

Log evidence for the "outside the menu" part: during the session the radar and enemy cues resumed
(gameplay owned input) more than 20 times while the panel stayed open, each followed by "Ráfaga"
(the default cell) — navigation was cleared and rebuilt. See the input-mode notes in
[runtime feedback](runtime-feedback.md#controller-mode-lost-to-screen-reader-keys-2026-10-04).

Skills suite: 233 checks. Runtime unverified: actual icon activity per class, labels' text.

## Modifying a skill with a controller (2026-10-04, static analysis)

User question: does the mod let a player change skills ("variants") through their trees? Native
evidence, build 25711439 (RVAs via the regenerated `BepInEx\interop\MethodAddressToToken.db` +
Capstone on `GameAssembly.dll`; bodies are split into chunks, so `.pdata` sizes cut them short).
The game has no separate variant system on this path: specialization nodes take points up to
`maxPoints` and change the skill.

- **Top row (y=0):** [2] Back, [3..7] five specialization slots, [8] respec, [9] specialise,
  [10] stop respeccing. Back → `CloseSkillTrees`/`ExitRespecMode`/`openSkillsOverview`.
- **A** (`HandleSubmitInput` @0x24922d0, only A/X/Y handled): node focused → `SkillTreeNode.Clicked()`
  @0x2488f70 → `TreeUIManager.skillTreeNodeClicked` @0x16e72e0, which does nothing unless the skill
  is specialised. Normally `spendSkillPointCommand` @0x16e7b80 adds a point, no confirmation; in
  respec mode (`SkillsSpecialisationUIManager.Respeccing`) and only if the node `couldBeRespecced`,
  an "Are you sure?" `ConfirmAsync` (skill loses a level above its minimum) → `respecSkillPointCommand`.
  No gold cost found. `LeftClicked`/`RightClicked` belong to the fullscreen passive tree only.
- **X** closes the panel to the action bar; **Y** focuses search; **B** closes the tree (and respec
  mode); **LB/RB** (`HandleContextNavigationInput` @0x2491720) cycle the specialised slots.
- **Specialising:** A on an empty slot → `SelectSkillToSpec` @0x24949c0 marks eligible skills; the
  next A on one specialises it at once (`specialiseInSlotCommand` @0x16e7830). From an open,
  unspecialised tree the specialise button (shown with a free slot and an unlocked skill) does the
  same (`specialiseButtonClicked` @0x16e7540). No confirmation, no cost.
- **Respec button** → `SkillRespecChoiceModal` (None=0, RemovePoints=1, DespecializeSkill=2):
  RemovePoints enters respec mode (`respeccingNodesText` shown); Despecialize confirms ("removes all
  skill points and experience") → `LocalTreeData.Despecialise`. `respecMinLevelText` is display only.
- **Points available:** `Tree.unspentPoints` on `CurrentlyOpenTree`, set to
  `max(0, getMaxPointsForSkill − allocated)` by `updateOpenSkillTreeUIWithData` @0x16e7ed0.

More native facts (follow-up analysis):

- **Leaving the tree:** with nothing above, native Up does `EventSystem.SetSelectedGameObject(null)`,
  `HideIconTooltip()`, `HideTreeNodeTooltip()`, `currentlySelectedIdx.y = 0` (x unchanged), `MoveRight()`
  until a non-empty cell, `FocusSelectable()`; `currentlySelectedNode` stays, so Down
  (`FocusTreeNode` + tooltip) returns to it. Top-row Left/Right wrap and skip empty cells
  (`ArraySlotIsEmpty` = no rect or inactive object); Up goes to the search row (navigable 121).
- **`inTopArea`** @0x2496310 is the controller cursor's centre above 900/1080 of the screen, updated
  synchronously when `FocusSelectable`/`FocusTreeNode` show the cursor.
- **Specialised:** `PlayerFinder.getLocalTreeData().isSpecialised(ability)`, the game's own test;
  `SkillTree.specialised` is never written.
- **Slot selection:** `AbilityPanelIcon.specMode`/`slotToSpec`/`locked`; eligible = `specMode &&
  slotToSpec >= 0 && !locked`, already specialised = `specMode` with `slotToSpec = -1`. Reset by
  `ResetAllAbilityPanelIcons` (tree opening, Back) and `OnEnable`. The game shows no text for it.
- **Modals:** `SkillRespecChoiceModal` ("Respec Skill", minimum-level text) and `ConfirmationModal`
  ("Are you sure?" + explanation) inherit `Modal<,>`, the only class declaring `IModal`, so the
  [generic modal reader](announcement-lifecycle.md#generic-modal-windows) reads their title,
  message and static text; their buttons are read through focus. Prefab button labels unread.

Mod behaviour (2026-10-05, `SkillTreeState`, `SkillTreeNavigation`, `SkillSelectionReader`):

- Up from a branch's first node goes to the root; Up from the root leaves the tree the native way,
  on the button the tree needs now (Especializar, then Terminar de quitar puntos, then Respec, then
  Back; the native step right if none is found). Top-row buttons read their own label, or the
  mod's name when unlabelled, plus the respec minimum-level text.
- A new tree says "Habilidad sin especializar" first, or its unspent points after the node; later
  only changes are spoken (points left after a point, specialised/despecialised, point removal mode
  started/ended). The root, which takes no points, no longer reads "0 de 0".
- Cross on a node the game ignores says why: specialise first, node full (added 2026-10-05 with the
  passive rework), or no points left. In point removal
  mode a node with points the game will not refund says "No se puede quitar".
- A free slot says confirming chooses a skill; after that it asks for one, eligible skills say
  "Confirma para especializarla" and specialised ones "Ya especializada".
- When point removal hides Respec under the cursor, the last node is focused again (native Down).

Skills suite: 263 checks; no new hooks (the existing `HandleSubmitInput` prefix now also takes the
panel). Runtime unverified: every button label, whether hidden buttons are inactive in the
hierarchy, online point updates, and the modals as heard.
