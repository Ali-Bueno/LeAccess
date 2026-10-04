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

- Whether `regioniName` equals `focusedRegionID`, and that `selectedLocationLabel` updates the same frame as the focused ids.
- Which object `UiFocusOwnership.TryGetSelected` returns on the map with the controller: is
  it inside a `UIWaypoint`/`UIWaypointRegion`? If not, add a hook on `MapPanel.SelectLocation`
  / `FocusOnRegion` (or poll `focusedSceneID`/`focusedRegionID`) feeding the same reader.
- `GetIsActive()` versus the `isActive` field for locked destinations; whether the player's
  own waypoint reads locked; `noWaypointInScene` and monolith waypoints are not handled.
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
