# Menu visibility, focus and reading order — Steam 1.5.1

Audited against the replaced build **25672295** interop/source on 2026-10-03.
The game was not launched. The findings below are source and automated-test
evidence, not a claim that every screen has been listened to in-game.

## Shared visibility and focus

`UiVisibility` now checks active objects, enabled canvases, finite positive
CanvasGroup opacity, rendered screen bounds and ancestor scroll/stencil bounds.
`UiScreenGeometry` projects actual Graphic rectangles through a known
screen-space canvas/camera; camera-space rectangles behind that rendering camera
cannot supply menu text. Partially visible rectangles remain eligible.
Layout-only rectangles do not act as implicit masks, even when their bounds are
outside the viewport and a child label extends back on screen. Missing/zero-sized
layout anchors remain valid owners, while zero-sized rendered labels do not
supply text. Explicit masks still clip those child labels.

Unknown projection is distinct from proven invisibility. For a WorldSpace
canvas, `worldCamera` is an event camera and does not establish which cameras
render its text; neither its absence, disabled state nor depth can prove that
the canvas is invisible. These labels retain native alpha/culling checks and
stable source order rather than using invented screen bounds. The API meaning
is documented by Unity's
[Canvas.worldCamera reference](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Canvas-worldCamera.html).

`TextUtils.IsVisibleLabel` and `UiVisibility.IsVisibleGraphic` additionally check
the graphic's enabled flag, color opacity, CanvasRenderer opacity and native
culling. An invisible parent background Image does not hide independently
rendered child labels. There is no guessed opacity threshold and no requirement
that a visible disabled option be interactable.

Generic speech candidates also require a visible graphic when their own
subtree contains graphics. This prevents tooltip/object-name fallbacks from
announcing a fully faded control. Controls without graphics remain eligible;
this additional filter does not decide whether a container owns a menu.

Clipping honors RectMask2D padding, the cumulative intersection of nested masks,
MaskableGraphic.maskable and override-sorting canvas boundaries. The last rule
is essential for expanded dropdowns that render outside their parent's scroll
viewport. Current interop exposes all the used members in the existing Unity
assemblies. The ancestry rules also match Unity's
[MaskUtilities implementation](https://github.com/Unity-Technologies/uGUI/blob/main/com.unity.ugui/Runtime/UGUI/UI/Core/MaskUtilities.cs).

`UiFocusOwnership` separates foreground access from exact selection. Generic
cursor callbacks and value changes must belong to the current native control;
retained background focus cannot speak merely because its GameObject is active.
The poller and manual-details path share the same current-control reader:

- `Navigable.ActiveSelectable`, including `PrioritizedNavigable` and
  `UnityDrivenNavigable`.
- `VerticalNavigable.selectables[currentlySelectedIdx]`, with live-array and
  bounds checks. In this build **VerticalNavigable derives MonoBehaviour**;
  assuming it derives Navigable silently omits these menus from polling.
- The exact settings selection from `SettingsFocusReader`.
- `CraftingMaterialsPanelSearchInputFieldNavigable.inputField`.

Other native INavigable components retain their cursor-driven hierarchy path,
but their Behaviour must be enabled and visible. Indexed owners with invalid
indices never fall back to a retained sibling control. Losing ownership clears
the announcement cache, so closing a modal can reannounce the restored choice
even when its underlying native index did not change. No navigation ordering,
selection, input or game action is changed.

The screen-content reader mixes TMP and legacy labels in one spatial order;
declared header/body references must belong to that popup. Foreground selection
uses overlay-canvas render order or a shared hierarchy when comparable, with
display chronology as the fallback. Nested native modals are tracked separately
so a child notice does not make an older parent steal foreground speech.
`LeAccess.ScreenContent.Tests` passes **87 checks** for this content lifecycle
and ordering path.

## Coverage of menu families

| Menu family | Selection/content source and audited behavior | Remaining runtime check |
|---|---|---|
| Login, ordinary buttons, pause, standard lists | Current native selection or owned cursor; visible button/input/dropdown/slider/toggle labels; stale owner rejected. Pause recovery remains its existing separately tested behavior. | Screen-specific custom/unnamed controls and focus during transitions. |
| Social | Native `SocialPanelNavigable` is a custom SerializedMonoBehaviour owner; visible owned cursor path remains. `SocialPanelPopupNavigable : VerticalNavigable` now participates in indexed polling. | Friend/party list completeness and popup interactions. |
| Settings: Gameplay, Graphics, Sound, Social | Exact current SettingsPanelTabNavigable and its declared tab/row; hidden or faded labels excluded. Slider/editor identity and native formatting retained. | Actual tab/row layouts and controller traversal. |
| Settings: Challenges | Visible owned enabled/disabled state; spatial order of challenge headings. Manual details now have the same popup/dialogue/loading/application gates as automatic reading. | State transitions and buttonless tab listening. |
| Class/mastery/realm creation | Native selected carousel index, exact declared tab/mastery identity. Disabled retained ListNavigable can no longer announce a choice. Localized definitions remain available on manual inspection. | Existing-character coverage and every creation transition. |
| Character name and screen keyboard | Existing exact input ownership, physical/controller focus recovery and error reader; error labels inherit common visibility. | No keyboard/input behavior was changed by this audit. |
| Skills and passive tree | Native active panel/child and exact selected node. Existing foreground-modal gates retained. Rendered skill labels use the shared renderer checks; visual stat rows use spatial order. Native selected-node models remain legitimate sources when a tooltip has not been rendered. | Late tooltip layout, all skill types and allocations. |
| Quest journal/world map | Exact native map/journal owner and selected quest; hidden journal and foreign quest text rejected. UI fallback objectives/history/rewards follow visible row positions. Model-backed active objectives retain model order and explicit history separation. | Live quest changes, scroll positions and map/journal transitions. |
| Inventory, equipped gear, stash and merchant | Separate selected-container/item model readers; generic controls retain shared visibility and foreground ownership. Equipped-slot handling is documented in the inventory reference. | All container types and merchant purchase/sale flows. |
| Forge/crafting | `CraftingMainUI : ItemContainerUI` uses item reading; `InventoryCraftingButtonsNavigable : VerticalNavigable` gets indexed polling; the materials search editor has an explicit current-control path. | Complete glyph/rune/affix operations and specialized tooltips remain unverified. |
| Loot-filter editor | `LootFilterPanel : Panel`, `ConfigureFilterPanel` and `AddLootFilterModal` retain their normal generic controls. Common visibility, dropdown clipping, labels, modal ownership and visual text order apply. | No dedicated model reader for every filter-rule condition; full editor operation remains unverified. |
| Dialogue/responses and subtitles | Existing conversation/response ownership retained; all visible text now uses common graphic visibility, including CanvasRenderer fades. Ambient bark source/range constraints remain intact. | Conversation handoffs and fades. |
| Tutorials, terms, modal notices and controller wheel | Dedicated content/selection readers, common visible text and screen-position ordering. Foreground content owns speech before underlying generic controls. | Multiple overlapping notices and every wheel variant. |

## Reading order and tests

`UiReadingOrder` uses projected rectangles: rows from top to bottom, then left
to right within intersecting vertical spans. If a fragment has no usable screen
geometry, its stable source position is retained. It changes speech order only.
Generic row-label candidates, multi-label screen content, Challenges, rendered
quest rows and skill-stat rows use it. Domain fields such as item name followed
by stats or quest title followed by state retain their meaningful structure.

`LeAccess.MenuReading.Tests` links the actual visibility, geometry, focus,
reading-order, text-detection and announcement sources: **165 checks**, including
offscreen/partial rectangles, camera depth, scroll padding, disjoint masks,
dropdown sorting boundaries, renderer fades, non-rendering parents, disabled
native owners, indexed vertical lists, material-search ownership, modal return
and blocked manual settings details. Specialized state suites use an explicitly
named geometry/order stub where they have no rendered geometry; they are not
counted as extra geometric coverage. Their existing selection/ownership checks
continue to run against the actual readers.

Arbitrary nonrectangular stencil interiors, partially covered glyphs and every
opaque overlay are not reconstructed from pixels. Native culling and actual
foreground ownership handle those cases where available. Rectangle overlap
alone is never treated as proof that an unrelated panel occludes another.
