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
  branch. Down/Up = next/previous node; Right/Left = first node of the next/previous branch; ends
  stay and repeat the node, restoring `y = 1`. Stick flicks use the dominant direction
  (`DirectionExtensions.ToDirection`). Nodes speak "N de M" after their points.
- In the top area or with the tree closed the game's handling runs unchanged; B leaves the tree and
  LB/RB switch specialization trees as before.
- A prefab node maps to the open tree's node with the same `id` for reading and moving; after
  opening, with a controller, the open tree's node is focused the native way.

Skills suite: 204 checks. Runtime unverified: whether any node sits in the top area (the native
trap remains there), the layout and branch order as heard, and the feature flag value.
