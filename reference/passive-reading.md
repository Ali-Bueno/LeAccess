# Passive node reading — Steam 1.5.0.1

Implemented against build 25663618, without starting the game or adding native hooks.

`PassiveSelectionReader` follows `NavigationManager.currentlySelected` only when
it is a visible `PassiveTreeNavigable`. It casts the erased navigation array to
`Il2CppSystem.Array`, validates rank and actual dimensions, and reads
`[currentColumn,currentRow]`. Native `PassiveTreeNavigable.SelectNode` RVA
`0x105D990` and `ShowTooltip` RVA `0x105DCE0` confirm this coordinate order.
`ShowTooltip` obtains the selected GameObject's `SkillnodeTooltip`; the reader
uses its `node`, rejecting nonpassive nodes, unavailable trees and stale wrappers.
No native addresses or offsets are embedded in the implementation.

The current `LE.UI.Tooltips.TooltipSystem` comes directly from
`UIBase.instance.TooltipSystem`, without scene scans. Its visible, enabled,
layout-ready tooltip's bound `_info` must equal `_activeNodeTooltipInfo`, and
both `ActiveNodeTooltipNode` and the bound info's `Node` must match the selection.
Old, disabled or unfinished tooltips cannot
supply another node's text. The fallback is a cached `NodeTooltipInfo(node,false)`
from the game's own tooltip API; it does not open a tooltip or change selection.
Native constructor RVA `0x200C290` stores the node/alt-text flag and initializes
its own lists. `Refresh` RVA `0x200BC20` calls the tooltip content builder's
modifier/requirement refresh routines. Fallback refresh occurs on selection,
allocated/max points, requirements-met state, tree unspent points or game locale
changes; held selection does not rebuild lists each frame.

Speech includes localized name, allocated/max points, description, stat modifiers,
point-bonus description/modifiers and requirements. These use `NodeTooltipInfo`
and the game's `ToModifierString`/`ToRequirementString`, preserving its localization
and scaling logic. `NodeTooltipRequirementInfo.IsMet` supplies explicit spoken
met/unmet state: native `ToRequirementString` RVA `0x200CAB0` otherwise represents
this distinction with rich-text color, which speech cleaning removes.
Only the points template and requirement-state wrappers are mod translations.

`PassiveAccessibility` uses two stable `ScreenContentTracker` observations,
interrupts on delivered selection changes, and avoids repeat speech while held.
Loading, loss of application focus, modal content, dialogue and controller radial
menus have priority. Owned node focus bypasses generic control reading. Pending
speech remains queued when another announcement already used the frame.
Leaving the panel releases reader caches; reopening refreshes fallback data even
when the selection's counts have not changed. Reset also releases state on teardown.

Validation: 54 source-linked checks exercise the actual reader and announcer:
coordinates, allocated points, cached refresh, absence of scans, owned focus,
stable/deduplicated/interrupted delivery, priority gates, bounds/rank, visibility,
collected wrappers, nonpassive nodes, tooltip bound/active identity/layout,
disabled nodes/tooltips, reopen cache refresh and requirement state.
Feature checks used `SkipPluginDeployment=true`; the final integrated build was
installed (hash in `STATUS.md`). No new runtime dependency.

Controller listening remains pending: open Passives, move between nodes, compare
name/points/requirements with the shown tooltip, allocate a point and switch
mastery tabs. Check that reopening reads the current node and that respec/modal
content interrupts the node reader. The reader does not implement new controller
navigation or synthesize point spending. Dynamic requirements changed by outside
systems without the observed node/tree state changing need runtime confirmation;
the displayed owned model remains preferred when available.
