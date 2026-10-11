# Tooltip reading audit — Steam 1.5

Static audit, 2026-10-02, against the refreshed 1.5.0.1 interop reference.
Implementation follow-up: [skill tooltip details](skill-details.md) now covers
modern node information and ability stats; [passive nodes](passive-reading.md)
have their own reader. The omissions below describe the audited earlier state;
runtime/listening validation remains pending.
The game remained closed. `last epoch code/` contains native interop wrappers:
fields and signatures establish available APIs, not native call flow or proof
that a particular widget is used at runtime. Paths below are repository-relative.

## Existing coverage

- `LeAccess/Accessibility/Inventory/InventoryAccessibility.cs:40` observes the
  selected `ItemContainerUI.currentlySelectedItem`; its focused-container gate
  is at line 77. Matching item and opener ownership precedes tooltip reading.
- `LeAccess/Accessibility/Items/ItemTooltipReader.cs:109` supports modern item
  tooltips and checks readiness, active info identity and item identity.
  Its legacy path at line 139 reads the primary content and requirements footer.
- `LeAccess/Accessibility/Items/ItemTooltipV2Reader.cs:21` reads primary visible
  name/type, potential, description, implicits, affixes, unique modifiers, set
  information, roll possibilities, lore, prices, faction rank and requirements.
  Expanded text inside the tooltip scroll content bypasses scroll culling.
- `LeAccess/Accessibility/Skills/SkillSelectionReader.cs:119` reads the owned,
  initialized ability tooltip's name, description, definition and expanded
  more-information text. Its node path at line 161 reads legacy node title,
  description and points with an exact previous-node identity check.
- `LeAccess/Accessibility/Announcements/TextDetector.cs:17` reads
  `InfoForTooltip.SetText()` / `tooltipInfo.nameText` / `mainText` for focused
  controls. This already applies to derived tooltip classes when ownership is
  represented on the focused control or its parent.

## Prioritized opportunities

### 1. Modern skill-node tooltip and richer node details

Confirmed reader omission; active runtime path unknown.

- `last epoch code/LE.UI.Tooltips/TooltipSystem.cs:267`: `_nodeTooltip`;
  line 425: `_activeNodeTooltipInfo`; line 522: `ActiveNodeTooltipNode`;
  line 849: `OpenNodeTooltip(SkillTreeNode, RectTransform, bool)`.
- `last epoch code/LE.UI.Tooltips/NodeTooltip.cs:336`: `_name`;
  line 366: `_description`; line 381: `_altDescription`;
  line 411: `_modifiersSection`; lines 426–531: point-bonus widgets;
  line 559: `_requirementsSection`; line 589: `_refundCostValueText`;
  line 634: `_info`.
- `last epoch code/LE.UI.Tooltips/NodeTooltipInfo.cs:290`: `Node` provides
  the displayed node identity; line 275: `Requirements`.
- The current reader only consults `NodeTooltipManager`, so modern rendered
  modifiers/requirements/bonuses cannot be recovered through that branch.
  Even the legacy manager's requirements (267–297), alternate text (387),
  point bonuses (462–507) and stat lists (552–582) are omitted.

Next: establish which tooltip the controller actually opens. Any future reader
must verify selected-node and displayed-node ownership, visibility and completed
layout; never open or manipulate a tooltip merely to obtain speech.

### 2. Ability costs, cooldown, DPS and scaling tags

Confirmed fields outside the reader's current selection; runtime visibility and
which values are meaningful for each ability remain untested.

- `last epoch code/UITooltipAbility.cs:1004`: `statFields`;
  lines 1019–1061: game-provided DPS/mana/cooldown/channeling field indices.
- Same file, line 431: `BigStatField.textField`; line 1090:
  `scalingTagsText`; line 1120: `minionTagsText`; line 1210:
  `scalingInfoFields`.
- `SkillSelectionReader.cs:144` combines only definition, description,
  unlock requirement and more-information text.

Next: compare spoken details with the visible stat/tag groups for one attack,
one cooldown skill and one minion skill. Read visible localized widgets, using
game-defined indices if necessary; avoid inventing units from raw numeric fields.

### 3. Passive nodes

Confirmed dedicated-reader gap, also recorded in `STATUS.md`.
`last epoch code/PassiveTreeNavigable.cs:121` exposes `_navigationElements`,
lines 136/149 the current row/column, and line 356 `ShowTooltip()`.
`SkillSelectionReader.cs:29` handles `SkillsPanelManager`, followed by ordinary
`PrioritizedNavigable` controls; it has no `PassiveTreeNavigable` branch.
Generic label reading may supply fragments, but full selected passive details
and point allocation feedback are not established. Determine the actual displayed
passive tooltip and its owner before choosing a reader or hook.

## Categories requiring listening tests, not assumed missing

- **Stash:** `last epoch code/StashItemContainerUI.cs:11` derives from
  `ItemContainerUIWithSearch`, which derives from `ItemContainerUIWithHighlight`
  (`ItemContainerUIWithSearch.cs:10`), then `ItemContainerUI`
  (`ItemContainerUIWithHighlight.cs:13`). This is eligible for the existing
  inventory reader. Tab switching, search, focus ownership and refreshed item
  details still need runtime confirmation; do not claim all stash items are silent.
- **Vendor:** item price and rank requirements are included in the modern
  reader. `VendorContainer.cs:8` describes item data storage, not the focused
  UI. Test vendor and buyback item focus, prices, currency and affordability;
  storage declarations alone do not prove controller coverage.
- **Crafting:** `CraftingToolTip.cs:7` derives from `GeneralTooltop`, itself
  `InfoForTooltip` (`GeneralTooltop.cs:7`), so generic focus reading can already
  obtain its text. `CraftingMainItemContainer.cs:9` and
  `CraftingModifierItemContainer.cs:11` describe storage, not UI navigation.
  Test selected item stats, rune/glyph/shard descriptions and changed forge
  outcomes before selecting a dedicated implementation.
- **Loot-filter editing:** modern item reading already includes can-roll/adds-
  affixes sections; generic tooltip reading may cover focused rule controls.
  Confirm rule conditions, thresholds and preview text in the live editor.
  There is insufficient static ownership evidence here to name a safe new hook.
- **World descriptions:** `BaseDisplayInformation.cs:635` exposes
  `GetLocalizedDescription()`. `LeAccess/Accessibility/Npcs/NpcNameReader.cs:92`
  reads its name, not full descriptions. Some objects may have useful visual
  descriptions, but this is not evidence every interactable has such a tooltip.
  Test an NPC, a transition and a special interactable for meaningful displayed
  text before broadening world speech.

## Intentional exclusions

Equipment comparison stays out of the primary item readers; since 2026-10-04 it has its
own reader, see [item comparison](item-comparison.md). Action prompts are also intentionally
excluded from the primary modern item section list; check their existing hint
speech in listening tests before adding duplicate output.

## Tomorrow's short test order

1. Select two adjacent specialized skill nodes; compare speech with modifiers,
   requirements, alternate details and point bonuses. Move quickly and verify
   that no text from the previous node is announced.
2. Compare two abilities, including more information, mana/cooldown and tags.
3. Open passives; select a locked and unlocked node and inspect allocation/refund
   requirements without committing a change just for this test.
4. Read an inventory item, a stash item after changing tabs, and vendor/buyback
   items. Check stats, price and requirements; use native comparison separately.
5. Inspect one crafting modifier and one loot-filter rule condition. Change a
   value and verify current outcome/details, then close the panel for silence.

No production changes or new hooks were made by this audit.

## Keyword explanations (2026-10-10, user report)

The "more information" tooltip (`UITooltipAbility.moreInfoObj`, `NodeTooltip._altDescription`) exists only while
Right Stick is held. The mod now appends the alt text to details requests and speaks the held tooltip once; see
`passive-reading.md` and `skill-tree-navigation.md` for sources, config and the log line to confirm them.
