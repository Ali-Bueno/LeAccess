# Character creation choices and manual descriptions

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

Navigation now announces only the localized name. Descriptions and mastery bonuses
are returned by `ClassCarouselHandler.TryReadDetails(out string text)` exclusively
for the manual right-stick-up route. The method rereads current localization and
selection, rejects loading/background/modal ownership, and suppresses any pending
short announcement so it cannot interrupt the requested description next frame.
Repeated manual requests remain available for the same choice. This method should
not be used to prefetch text during ordinary focus polling.

`Unity.Localization.dll` and `UnityEngine.UIElementsModule.dll` are existing game
interop references, both `Private=false`; no new DLL is shipped. Older
MasteryButton/passive-tree readers remain available for their separate menus.
The modern passive panel is resolved through its exact `ActiveChildNavigable`.
The class reader accepts selected tab controls or a typed
`PassiveTreeBannerNavigable`: its exact `CurrentButton`, or its visible
`descriptionContainer` only while `IsDescriptionSelected` is true. Native setters
at RVAs `0x105C330` and `0x105C5E0` move the focus cursor to those respective
targets. A selected passive grid, search field, missing child or unrelated control
cannot use the open `_currentMasteryIndex` as focus evidence. This prevents a
background mastery announcement from interrupting node speech, and prevents a
failed node-detail request from reading the mastery instead.

## Realm selection: the ambiguous "Select" buttons

The user's 13:01 runtime log (`LogOutput.log`, 2026-10-02) records the initial
`New Character Button` at line 407 and `SeasonSelectionButton` inside `CycleTile`
at line 410. This is the **Season / Legacy realm choice**. The log does not show a
gender-selection screen at that point. Both realm buttons have the same localized
`Button_Label_Select` child, which explains the reported speech "Elegir".

The installed `PermaLoad.bundle/CharacterSelection` prefab confirms these bindings:

| Control under `CycleModal/Contents` | Owned realm title | Native action |
|---|---|---|
| `CycleTile/SeasonSelectionButton` | `CycleTile/SeasonName` | `CharacterSelect.NewCharacterClick(true)`; `LocalCharacterSlots.ChangeCycle(1)` |
| `CycleTile (2)/LegacySelectionButton` | `CycleTile (2)/Name` | `CharacterSelect.NewCharacterClick(false)`; `LocalCharacterSlots.ChangeCycle(0)` |
| `CycleTile/chooseCycleInsteadButton` | `CycleTile/SeasonName` | Changes the existing creation form to Season |
| `CycleTile (2)/chooseLegacyInsteadButton` | `CycleTile (2)/Name` | Changes the existing creation form to Legacy |

`CreationRealmReader` requires the live declared `CharacterSelect`, those exact
button names and direct parent paths, a real `Button`, and visible controls. It
reads the visible TMP title plus `EterraFrameHeader/Header` (`Cycles/Header/ChooseRealm`)
and the tile's optional visible `Label/Type` recommendation. Missing titles wait
instead of falling back to "Select". The copied `BorderTitle/Recommended` text
contains "Season" even on the Legacy tile and is deliberately not a name source.

Manual details combine only the chosen tile's `Message` and the shared
`Footer/CycleEndExplanation`. Their localization entries use numeric table keys;
reading their rendered TMP text preserves the current language without hardcoded
translations. Unrelated or hidden sibling text is excluded. These four static
bindings are documented because this modal has no dedicated typed realm-choice
component or title reference. No native hooks or UI state changes were added.

## Verification and remaining listening checks

The source-linked class suite now passes **101 checks**, covering
the installed tab layout, inactive exact-owned content, localized fallback,
language changes, category-label exclusion, foreign/replaced content, invalid
indices, collected wrappers, rapid focus and reopening. Realm tests reproduce all
four buttons and their generic "Select" children, distinct sibling titles,
incorrect copied Season decoration, optional recommendation, manual-only details,
missing/hidden titles, foreign ownership, repeated manual requests and suppression
of pending short speech. The main mod builds
without warnings/errors. These are static/host checks, not a listening claim.

Open Create Character and navigate both realm choices: expect the realm context
and the specific title, with descriptions only on right-stick-up. Repeat from the
change-realm action in character details. Navigate all five base classes and each
mastery: expect short names on focus and the exact current description on the
manual gesture, without activation or adjacent-mastery speech. The user-confirmed
[character-name keyboard behavior](character-name-reading.md) was not changed.

## Character select tiles

`CharacterTileReader` speaks an existing character's `charName`, `charLvlAndMastery`,
`charCycle` and, only while its object is active, `hardcoreTag` text, all in the
game's own localized text. Popup mode no longer falls back to generic object names
("Button", "Toggle"); it uses tooltip text or stays silent. Needs an in-game check.
