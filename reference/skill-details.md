# Skill tooltip details — Steam 1.5

Implemented 2026-10-02 against 1.5.0.1 interop; gameplay listening is pending.
Navigation now reads only the name, current binding/state and node points.
The [assignment and learning reader](skill-assignment.md) also supplies declared
weapon-variant assignment, learned state, required level/passive points and the
destination button while equipping a skill. Its follow-up uses Steam build
25672295 interop and native evidence.
All descriptions and tooltip content require right-stick-up; late tooltip
rendering and native expansion alone stay silent.
The game remained closed. These readers add no native hooks. See
[manual details](skill-inventory-buttons.md#manual-details-2026-10-02) for the
right-stick-up reread command.

## Localized ability stat names (2026-10-02 follow-up)

The session log ending 11:06:42 reads Aullido as `39,2. 10s` at line 1005.
The reader used only `BigStatField.textField`, which is the numeric value.
Installed `PermaLoad.bundle`'s `UITooltipAbility` (path ID
`8398561638648025117`) stores each value in a `Stat Value` child and its localized
name in the sibling `StatDescription/StatText`. The four stat boxes hold the
game's DPS estimate, mana cost, cooldown and channel cost labels; each label has
its own `LocalizeStringEvent`. Their TMP path IDs are respectively
`8684854497167753235`, `8602185316988926608`, `5754680594566309451` and
`683888024483248820`.

The reader now consumes the entire owned `BigStatField.obj` section when its
value is visible, retaining localized label, number and native units. It does
not infer stat kinds from array positions or add translation keys. Hidden,
transparent or culled values suppress the corresponding stat, and unrelated
tooltip sections stay excluded. Five source-linked regressions cover the actual
sibling layout, source scope, visibility and changed localization. Runtime
listening remains pending.

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

Ability combat stats occupy `SkillSelection.LiveDetails`. Manual inspection
refreshes them from current visible widgets; automatic selection reading skips
tooltip traversal entirely. The brief tracking snapshot uses the selected
definition's name, so a differently worded tooltip title cannot cause another
automatic announcement. Node point changes remain live without reading their
full description again.

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
Final skills suite after assignment/learning follow-up: 143 checks. The follow-up
built without deployment; `STATUS.md` records integrated installation status.

Tomorrow: select adjacent specialized skill nodes; compare modifiers, alternate
details, requirements, bonus and refund text. Allocate/refund a point and check
speech updates. Compare an attack, cooldown and minion ability, then expand
more information; verify costs/tags are understandable and no speech repeats
while DPS changes. Scroll a long tooltip to check whether mask-culling limits
some rows: this reader only consumes currently visible widgets. Passive-tree
navigation is handled separately from these specialized-skill node readers.

The additional 2026-10-03 service audit reproduced manual details speaking after
foreground or selection changed during formatting. Speech now rechecks the
current selected skill and foreground immediately before output. Six regression
cases cover popup, dialogue, radial menu, loading, application focus and a new
selected ability; the skill suite passes 177 checks. The main project builds with
`SkipPluginDeployment=true`, zero warnings/errors. Listening remains pending.

The output-acceptance follow-up prepares brief candidates without marking them
spoken until `ScreenReader.TrySpeak` succeeds. A rejected manual read retains
selection-bound intent and rebuilds current details when the reader recovers;
hidden, collected, unready or changed selections discard it. Automatic retries
yield to another accepted output in the same frame. The skill suite now passes
189 checks, including 12 acceptance/retry regressions. See
[the centralized contract](screen-reader-lifecycle.md) for transport evidence.
