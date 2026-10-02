# Skill tooltip details — Steam 1.5

Implemented 2026-10-02 against 1.5.0.1 interop; gameplay listening is pending.
The game remained closed. No new hooks or input behavior were added.

## Reading and ownership

`SkillSelectionReader` retains the actual selected panel cell/node and localized
definition fallback. `SkillTooltipDetailsReader` adds:

- Modern node tooltip through `UIBase.instance.TooltipSystem._nodeTooltip`.
  The system and tooltip must be active and visible, layout must be settled,
  displayed `_info` must equal `_activeNodeTooltipInfo`, and both `info.Node`
  and `ActiveNodeTooltipNode` must match the selected node by native pointer.
- Modern node type, description, alternate description, modifiers, point bonuses,
  allocated/max points, requirements and refund-cost root. Matching info model
  requirements preserve explicit met/unmet status, which would otherwise be lost
  when rich-text colors are cleaned. Game-provided requirement strings use the
  shared localized `node.requirement_met`/`node.requirement_unmet` wrappers;
  the visible section is a fallback when model entries are unavailable, so the
  same requirement is not repeated. Reading is confined to those sections;
  action prompts and unrelated sibling text are excluded.
- Legacy `NodeTooltipManager` points, passive/base/quest requirements, alternate
  description/header, lore, point bonus and displayed scaling/non-scaling stats.
  Existing previous-node, visibility and layout checks still apply. A visible
  modern tooltip takes precedence even while unsettled: localized selected-node
  fallback is safer than a retained legacy tooltip.
- Owned, initialized ability tooltip stat fields, scaling/minion tags and scaling
  effects. Speech uses visible localized widgets, including the game's units,
  rather than synthesizing costs or DPS from raw numeric fields.

Ability combat stats occupy `SkillSelection.LiveDetails`, separate from stable
name/description/requirements. The announcement layer snapshots this portion
for the current selection/detail mode so fluctuating DPS or cooldown values do
not restart speech every frame. A stable description change or closing/reopening
the selection permits a fresh snapshot. Node point/detail changes remain live.

## API evidence

The refreshed `last epoch code/` consists of native interop wrappers, not recovered
native algorithm bodies. These declarations provide typed read access; they do
not establish which UI variant is used in every runtime state.

- `LE.UI.Tooltips/TooltipSystem.cs:267`: `_nodeTooltip`; line 425:
  `_activeNodeTooltipInfo`; line 470: `_nodeTooltipRequiresLayout`; line 522:
  `ActiveNodeTooltipNode`.
- `LE.UI.Tooltips/NodeTooltip.cs:336`: `_name`; line 366: `_description`;
  line 381: `_altDescription`; line 411: `_modifiersSection`; line 426:
  `_pointBonusContainer`; line 531: `_pointBonusModifiersSection`; line 559:
  `_requirementsSection`; line 574: `_refundCostRoot`; line 634: `_info`.
- `LE.UI.Tooltips/NodeTooltipInfo.cs:290`: `Node`.
  Lines 445/459: `PointsAllocated`/`MaxPoints`; line 275: `Requirements`.
  `NodeTooltipRequirementInfo.cs` supplies `IsMet` and `ToRequirementString()`.
- `NodeTooltipManager.cs:267`: passive requirement; lines 282/297:
  base/quest requirements; line 387: `altText`; line 462: point-bonus container;
  lines 552/567/582: scaling/non-scaling/point-bonus stat lists.
- `UITooltipAbility.cs:1004`: `statFields`; nested `BigStatField.textField`
  at line 431; scaling/minion labels at lines 1090/1120; scaling fields at 1210.

## Validation and remaining listening tests

Source-linked tests exercise modern/legacy selection priority, mismatched info
and node identities, unfinished layout, hidden/collected widgets, section scope,
localized stat/tag reading, rapid navigation and suppression of fluctuating DPS.
Main project compiled against installed interop without launching the game.
Final skills suite: 85 checks. Integrated build installed; `STATUS.md` records
the final DLL hash and regression totals.

Tomorrow: select adjacent specialized skill nodes; compare modifiers, alternate
details, requirements, bonus and refund text. Allocate/refund a point and check
speech updates. Compare an attack, cooldown and minion ability, then expand
more information; verify costs/tags are understandable and no speech repeats
while DPS changes. Scroll a long tooltip to check whether mask-culling limits
some rows: this reader only consumes currently visible widgets. Passive-tree
navigation is handled separately from these specialized-skill node readers.
