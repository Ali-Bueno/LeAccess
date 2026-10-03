# Crafting and specialized filter controls

Inspected the installed Steam build **25690669** on 2026-10-03 without launching
the game or opening audio. The current native metadata, wrappers and installed
assets support two bounded fixes. Host checks exercise the production readers;
successful live focus and listening remain unverified.

## Verified fixes

- Forge add/upgrade buttons now include their own rendered affix name, tier and
  available shard count. The two actions have separate mod-owned English/Spanish
  labels. Visible empty or markup-only affix names suppress dependent tier/count
  text; hidden, foreign or collected labels cannot supply content.
- Crafting-material shard buttons now include their row's rendered name, affix
  type and quantity. Manual inspection also includes the visible improvement.
  Reused pooled rows are read afresh; there is no retained content cache.
- Filter bounds now include their own visible range heading: four potential
  ranges and the prefix/suffix count ranges. For example, the old generic speech
  was `Min: 2`; the new speech is `Legendary Potential. Min: 2`. Native localized
  headings and placeholder roles are read verbatim. Empty bounds remain empty;
  no zero, allowed limit or default is inferred.
- Native `Selectable.IsInteractable()` and the existing shared disabled wrapper
  preserve unavailable actions once. Foreground popup and navigation ownership
  continue through `UiFocusOwnership`; the specialized readers do not bypass it.
  Associated `InfoForTooltip` descriptions remain available on manual crafting
  inspection through the existing native `SetText()` path. Brief specialized
  speech does not invoke that path or construct an item/tooltip model.

Production entry points:
`Accessibility/Announcements/CraftingControlReader.cs`,
`FilterRangeControlReader.cs`, and the early specialized branch in
`ElementAnnouncer.BuildAnnouncementText`. These readers perform no input,
selection, item, filter, player or network writes and add no hooks/dependencies.

## Installed source and asset evidence

`PermaLoad.bundle` contains the `PanelSystemAssetsProvider` entries for
`ForgePanel` and `CraftingMaterialsPanel`. Their current soft GUIDs were resolved
through the current `LEAssetBundles/Catalog.bin`; loading the actual resulting
prefabs independently verified their identities. No old bundle-index table or
native address table was used as current evidence.

| Source | Installed identity / relevant structure |
|---|---|
| Forge panel | `assets_47fdfc1e558a73e2.bundle`, root `ForgePanel` |
| Four forge rows | `MainContent/ModSlot` and its three numbered siblings, each with an `AffixSlotForge` |
| Affix labels | `affixDescHolder/AffixName`, `affixDescHolder/tierLevel`, `AvailableShardsofSlottedType/Available Shard Count TMP` |
| Add/upgrade controls | `Shard/addShardButton` and `upgradeAvailable`; serialized `addAffixButton`, `upgradeButton`, `_upgradeButton`, and reciprocal `affixSlotForge` |
| Crafting material panel | `assets_302b3eb53d6ca3c0.bundle`, `ShardAffixListElement` path ID `5942668086542144629` |
| Shard button | Serialized `button` GameObject has its own native `Button`; name, quantity, type and improvement are row fields, outside that button |
| Filter templates | Installed `resources.assets`, roots `potentialcondition`, `affixcountcondition`, `affixcondition` |

Current wrapper members are at `last epoch code/AffixSlotForge.cs:157`
(`nameTMP`), `:172` (`countTMP`), `:202` (`tierTMP`), `:232` (`addAffixButton`),
`:247` (`upgradeButton`); `CraftingUpgradeButton.cs:43` (`_upgradeButton`) and
`:58` (`affixSlotForge`). `AffixSlotForge.NumberOfShardsAvailable` is exposed at
`:713`. Current native `SetVisuals` (RVA `0x114A7D0`) reads the shard-storage
quantity, retains it at `0x114AB3B`, formats it at `0x114AE02`, and assigns the
count label. The installed authored hierarchy also identifies this as available
shards, not a forging cost.

`RuleHolderUI.GetConditionPrefab` (`RuleHolderUI.cs:559`, current RVA
`0x11A2B80`) concatenates the native condition type name with the metadata
literal `ConditionsUI/`. The installed resource roots independently confirm
the corresponding potential and affix-count templates. Each supported range
row has direct serialized siblings in this order: `Title`, its `InputRange`,
and an optional `sub-title`. The min/max input placeholders are `Min` and `Max`.
The reader requires exactly one meaningful visible direct heading before the
owned range; it does not identify headings or conditions by GameObject names.

The native references used for runtime ownership are exposed in
`PotentialConditionUI.cs:47` (four range fields),
`AffixCountConditionUI.cs:126` (two range fields), `InputRange.cs:121`
(`_minInput`/`_maxInput`) and `NumberInput.cs:109` (`_input`). A field, wrapper,
range, condition, heading or placeholder that no longer belongs to this exact
control yields to ordinary control reading. A populated input can hide its
own placeholder; its authored role is still valid when the placeholder remains
an exact descendant of that input.

Current file SHA-256:

| Installed file | SHA-256 |
|---|---|
| `GameAssembly.dll` | `FC83F941396282C0E489ED44FF4375130FB74F3C5D8CDF983696A9EFBDC9019A` |
| `resources.assets` | `D4F82D96C854BE34D6CEC4CECF9533A2D3AE9DA8B9904395FFC63FC91BF4C49D` |
| Forge bundle | `3ECD187AF8A775E3E33A6870EF6E10657E66634D9F16D2C13279E06CF46AE478` |
| Crafting-material bundle | `92464BFFBA35D550EAB3384D3524026B286F146B4EE8313EB5B792EA201B45D8` |

Read-only extraction scripts/evidence live in
`%TEMP%/LeAccess-specialized-controls-20261003/`. Bundle MonoScript identities
came from the current installed `monoscripts.bundle`; resource identities came
from current `globalgamemanagers.assets`. Matching current bundle TMP schemas
decoded the stripped resource text/input records. Native disassembly used the
current method map in `%TEMP%/LeAccess-hazards-25690669-20261003/`.

## Validation and limits

`dotnet run --project tests/LeAccess.MenuReading.Tests` passes **231 checks**,
including **53 added checks** in `CraftingTests.cs` and `FilterRangeTests.cs`.
Both source-linked integration reproductions failed before their reader fixes.
Checks include action distinction, disabled wrapping, native descriptions,
updated/reused rows, markup-only/hidden/foreign labels, exact button/range/input
ownership, native lifetimes, localized text, empty fields and popup ownership.
`dotnet build LeAccess/LeAccess.csproj -p:SkipPluginDeployment=true` passes with
zero warnings/errors against the refreshed installed interop.

Rune/glyph item selector speech remains unverified. Their specialized native
`CraftingMaterialSelectorPanel` and `CraftingRunesAndGlyphsPanel` navigation is
not proof of a normal selected `Selectable` or a valid retained item-tooltip
model. No guessed matrix indexing, item matching, selection or model creation
was added. The general forge summary/preview, unavailable-reason explanation,
advanced affix comparison/combined-tier fields, sealed-affix dropdown, and other
filter condition types also remain outside these fixes. Full forging/filter
workflows, gamepad focus updates and listening still require gameplay checks.
