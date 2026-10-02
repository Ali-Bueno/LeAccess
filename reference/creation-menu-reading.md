# Character creation class and mastery descriptions

Investigated and implemented against Steam 1.5.0.1, build 25663618, on
2026-10-02. The game was not started.

## Confirmed cause

The installed `PermaLoad.bundle` owns the `CharacterSelection` prefab. Its
character-creation screen contains **20 `TabUIElement` controls**, five
`TabUIController` owners, five base-class descriptions and fifteen mastery
descriptions. There are **no `MasteryBanner` or `MasteryButton` components** under
that prefab. The previous reader depended on those components, so its tests for
the older layout could pass while descriptions in this installed screen remained
unreadable.

The name/mastery screen uses this declared ownership:

- `TabUIElement.controller` and `elementID` identify the exact content entry in
  `TabUIController.tabContents` (`last epoch code/TabUIElement.cs:214`, `:229`;
  `TabUIController.cs:245`).
- For example, Sentinel's controller is path ID `310018009806884741`; its four
  entries bind base Sentinel, Void Knight, Forge Guard and Paladin in that order.
  The Paladin tab is `8741127241726881942`, with `elementID=3`.
- Each content entry owns a `LocalizeStringEvent` on its description TMP. Its
  exact key starts with `Class_Description_` or `Mastery_Description_`.
  Sentinel's base source is `2329863363396556034`, key
  `Class_Description_Sentinel`; Paladin's is `6801043648049157475`, key
  `Mastery_Description_Sentinel_Paladin`.
- Each tab also owns a localized class/mastery title, separate from the
  `Class_Tab_Base_Label` / `Class_Tab_Mastery_Label` category labels.

Native `TabUIController.GetActiveTabContents` at RVA `0x17E4B40` bounds-checks
the selected ID and indexes the declared `tabContents` list. This matches the
serialized links; content order is not inferred from scene names or sibling order.

## Reader behavior

`CreationClassTabReader` caches the typed links under the current `CharacterSelect`
instance, including inactive tab content. The selected tab receives its own
localized title and description even before it is activated. Only the two
verified description roles and class/mastery title roles qualify; category labels,
skill headings, name fields and nearby paragraphs cannot become descriptions.
The class carousel finds its base description by matching the actual class enum
name to the installed `Class_Description_` key within those owned entries.

Localization is resolved through the game's existing `Localization.GetText`
service with the associated TMP's serialized text as fallback. This works before
an inactive panel's localization event has rendered it and follows subsequent
language changes. The reader does not open panels or change selection. It validates
the owner/content pointers and index on each use, rejects ambiguous description
sources and retains the existing two-observation announcement tracking.

`Unity.Localization.dll` and `UnityEngine.UIElementsModule.dll` are existing game
interop references, both `Private=false`; no new DLL is shipped. Older
MasteryButton/passive-tree readers remain available for their separate menus.

## Verification and remaining listening checks

The source-linked class suite passes **67 checks**, including 20 new cases for
the installed tab layout, inactive exact-owned content, localized fallback,
language changes, category-label exclusion, foreign/replaced content, invalid
indices, collected wrappers, rapid focus and reopening. The main mod builds
without warnings/errors. These are static/host checks, not a listening claim.

Navigate all five base classes, enter the name/mastery screen and highlight each
mastery without activating it. Expect its complete localized description once,
with navigation interrupting the previous selection. Activate a tab and enter its
skill list; it must not speak a neighboring mastery. Then return to the name field
and verify the [explicit keyboard activation](character-name-reading.md).
