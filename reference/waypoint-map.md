# World-map waypoints and regions

`WaypointReader` (`Accessibility/Map/`) is a typed reader called from
`ElementAnnouncer.BuildAnnouncementText`, right after `CharacterTileReader`. Without it the
generic path spoke the first visible text or a cleaned object name.

## What is spoken

Waypoint (nearest `UIWaypoint` in the focused object's parents, visible):

1. Name: `UIWaypointStandard.locationLabel`; other types use the text under `UIWaypoint.label`.
   An unreadable label (hidden, alpha 0) returns false so the generic path runs; no object names.
2. `waypoint.here` when `GetIsPlayerHere()`.
3. `waypoint.locked` when `!GetIsActive()`.
4. Quests from `questIndicators` (`WaypointQuestIndicators`): active main, active side,
   available main (`hasPotentialMainQuest`), available side (`haPotentialSideQuest`, sic).

Region (`UIWaypointRegion`): `regionLabel`, `locationCountLabel` as displayed, here when
`playerLocation` is visible, and the same four quest flags from the region.

Right-stick-up (`includeDetails`): adds `MapPanel.waypointDescriptionPopup.description` when
visible. The map is found as an ancestor `MapPanel`, or via `WorldMapPanel._mapPanel`.

Keys: `waypoint.here`, `waypoint.locked`, `waypoint.quest_{main,side}_{active,available}`
in `en.json`/`es.json`.

## How focus reaches waypoints

Not proven. The reader assumes the focused `Selectable` (typically `UIWaypoint.waypointButton`
or a region button) sits inside the waypoint/region object, like the quest reader assumes for
quest buttons. `MapPanel` is itself a navigable (`HandleNavigateInput`, `SelectRegion`,
`FocusOnRegion`, `SelectLocation`, `focusedSceneID`, `focusedRegionID`, `mapCursor`) and the
quest reference shows it moves quest selection without focusing a descendant, so waypoint
navigation may likewise bypass `Selectable` focus.

## Selection poller (controller navigation)

`MapSelectionAnnouncer.Update(map, questMoved)` runs from `MenuAccessibility.LateUpdate`
after `FlushPendingFocus`. `OpenWorldMap.Find()` resolves the map from the registered
`NavigableID.WORLD_MAP_PANEL` navigable (`WorldMapPanel._mapPanel`), no scene search.

- Change key: `GetFocusedSceneID()`, `GetFocusedRegionID()` and the `selectedLocationLabel` text.
  Speaks the label and `selectedLocationLevelLabel` as displayed, then `WaypointReader` states
  of the `UIWaypoint` where `representsSceneInEra(scene, CurrentEra)` (searched through
  `eras[].waypointController.waypointsInMenu`), else of the `UIWaypointRegion` whose
  `regioniName` equals the focused region id.
- `mapTitle` is spoken with the first selection after opening and whenever `CurrentEra`
  changes (the quest reader never reads it).
- Quest-driven moves: a quest selection change sets `QuestAccessibility.MapMovePending`; the
  next change of the selected location (not an era switch alone) is queued after the quest brief
  and consumes it. The map tweens to the quest location (`WorldMapZoom.ZoomToLocation`), so the
  old one-frame window missed the move and the location was dropped or cut the brief.
  `HasActiveContent` cannot be used: the map always owns a selected quest.
- A selection already spoken by the focus path in the same frame is recorded, not doubled.
- A closed or hidden map resets state, so reopening announces again.

## Needs live verification

- Resolved by static analysis (2026-10-09, below): `FocusOnRegion` sets `focusedRegionID` to
  `regioniName`; `SelectLocation` writes the label and level text synchronously; the controller
  never focuses a `Selectable` on the map, so the poller is the path that speaks.
- Whether the player's own waypoint reads locked; `noWaypointInScene` and monolith waypoints are not handled.
- Whether `label`/`locationLabel` stays visible when `labelHidden` is set.
- Whether quest flags are set while the indicators are hidden (`ShowIndicators(false)`).
- Whether the description popup is open for the focused waypoint on controller.

## Cross on the map: travel (2026-10-04)

Native evidence (build 25690669, disassembly of `MapPanel.HandleSubmitInput` 0x17617D0,
`QuestButton.clicked` 0x17B6D20, `UIWaypointStandard.LoadWaypointScene` 0x181B4D0):
selecting a quest only recenters the map (`ZoomToQuestLocation`, no travel); Cross
(`SubmitType.A_EX`) with a focused scene presses `travelButton`, whose listener is the
target waypoint's `LoadWaypointScene`. That travels at once, without a prompt, when
`!noWaypointInScene && isActive && !playerIsHere`; otherwise nothing happens. This is why
"some quests let you travel" and others "go silent". `openQuickTravelWindow` belongs only
to `WorldMapPanel.OnOpenWithArgs` and is read by the generic modal reader.

`MapTravelAnnouncer` mirrors that guard: "Viajando a X", "Bloqueado", "Estás aquí" or
"Aquí no hay punto de viaje" (prefix on `UIWaypointStandard.LoadWaypointScene`). If Cross
on a focused scene never reached the waypoint, a `MapPanel.HandleSubmitInput` postfix says
"No se puede viajar" plus the focused location. Cross on a region zooms in and stays with
the selection reader. Listening pending: whether online server rejections need speech.

Tests: `tests/LeAccess.Waypoints.Tests` (35 checks, doubles only).

## List navigation with the quests hidden (2026-10-09)

User report: the left-stick cursor is hard to use by ear, and the D-pad belongs to the quests.

Native evidence, build 25764382 (`MethodAddressToToken.db` + Capstone, read-only; scratch in the
session folder, not kept):

- `WorldMapMap/<ProcessInput>d__6.MoveNext` 0x103DD00 reads the gamepad template itself. Every
  frame the left stick goes to `WorldMapArrow.MoveMap` 0x18238C0. LB/RB call
  `HandleNavigateInput(LEFT/RIGHT)`, which switches the era with wrap. D-pad up/down call
  `HandleNavigateInput(UP/DOWN)`, which selects the previous or next quest; they repeat while held.
  D-pad left/right call `HandleContextNavigationInput`: right focuses the quest details or the
  epoch mastery. Square calls `QuestPanel.ToggleQuests`, Triangle recentres on the player, R3 pins
  the quest, and L2/R2 zoom. The navigable is `MapPanel` itself (id 42). On the keyboard, Left
  Shift is `ToggleQuestPanel`.
- No pointer is involved. `WorldMapArrow.MoveMap` scrolls the `ScrollRect` and takes the view
  centre as the cursor. `TryFocusOnAnyWaypoint` 0x1824C60 looks through the current era's
  `waypointController.waypointsInMenu` (+0x30). Within a viewport distance of 0.035 it calls
  `SelectSceneLocation`, which leads to `MapPanel.SelectLocation`. With nothing in range it calls
  `DeselectCurrentLocation`. Regions use a threshold of 0.03 and `FocusOnRegion`.
- At or below `labelZoomRegionBreakpoint` the waypoint icons are inactive and only regions can be
  focused; `regionZoomLevel` is where the game's own zoom tweens end.
- `questsHidden` hides the list and details, but D-pad up/down still change the quest and
  recentre the map. The setting survives closing the map, because the panel is not destroyed.
- The game's own recentre (`CenterOnWaypointWithPlayer` 0x18235C0) calls
  `CenterOnGameobject(waypoint)` (instant, keeps the zoom) and then `SelectSceneLocation()`. The
  next per-frame check therefore confirms the selection.
- Avoid `ZoomToWaypoint` / `ZoomToLocation` for this. They tween over `autoZoomSpeed`, change
  the zoom, and drop calls made while a tween runs.
- Prologues: `MapPanel.HandleNavigateInput` 0x1764D50 has no relative branch, so the original
  may run. `WorldMapPanel.HandleNavigateInput`, both `MoveMap`s, `FocusOnRegion` and
  `ToggleQuestPanel` start with a short `je` and must not be hooked with the original running.

What the mod does (`MapLocationList`, prefix `MapNavigatePatch`):

- With `questPanel.questsHidden`, D-pad up/down walk the current era's `waypointsInMenu`.
  Regions come in `regionsInMenu` order, each with its own `waypointsInRegion` order, and places
  in no region come last. Each step zooms in first if the map is in the region band, centres the
  map on the place and selects it, exactly like the recentre above. Cross then travels natively.
- Speech is: the region's label (raw text, since it is hidden while zoomed in) when a new region
  is entered, then the selection's label, level and states, then "N de M" within the region. With
  nothing focused, up/down start from either end. The list never wraps; at an end the current
  place repeats. Presses during an era crossfade or a zoom tween are swallowed.
- With the quests shown, LB/RB and the D-pad behave natively.
- `MapSelectionAnnouncer` speaks the era when the displayed `mapTitle` text changes, because
  `OpenEra` sets it only after its crossfade. It adds the era's position among the `eras` tabs,
  says "Misiones ocultas/visibles" when Square toggles them, and adds the hidden state to the
  first reading after opening.
- `CheckSnap` logs `[World map] snapped selection X was replaced by 'Y'` when the game's next
  check did not keep the snapped place. That would happen if the `ScrollRect` cannot centre an
  icon near the map edge.

Open: the order inside regions is the prefab's, not necessarily story or geographic order.
D-pad left/right are untouched, so jumping between regions is not offered. Whether the
`UIEra.mapTitle` text is localized is unverified. Waypoints tests 56 checks; listening pending.
