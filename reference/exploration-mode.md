# Exploration mode (proposal, 2026-10-03)

Status: implemented 2026-10-03 (sections below); unexplored areas added 2026-10-04 and
replaced the same day by the full D2R category set ([D2R categories](#d2r-categories-2026-10-04)). Requested by the user after
learning that Last Epoch gives sighted players no route line to quests (only the
minimap icon and its edge arrow, see
[automatic orientation](automatic-orientation.md#live-report-silent-for-a-whole-session-2026-10-03-2118-log)).
The user's rule: do not compute help the game does not give sighted players;
build an exploration mode like the D2R mod's instead.

## What the D2R mod does (`D:\code\c++\diablo 2 access project\d2r-access`)

There is no separate mode toggle: "exploration" is the default fairness policy of
the right-stick scanner plus the beacon (`docs/entity-scan.md`, `docs/beacon.md`,
`docs/decisions.md`). "Full assistance" is opt-in.

- Scanner: right stick up/down picks a category (empty ones skipped, name spoken),
  left/right steps through its entries nearest first: name, straight-line distance
  in steps (`esES.h` "%d pasos", `EntityRadar::DistancePhrase`) and one of eight
  screen directions ("arriba y a la izquierda", `Radar::ScreenSector`). Both describe
  the direct line to the target, not the walking route.
- Categories: exits, portals, waypoints, NPCs, stash, shrines, other objects, quest
  objects, unexplored rooms, enemies (live only), players, corpses. Loot and
  ordinary chests excluded (they have their own loops).
- Discovered only: drawn on the automap, stood in, or seen in range and line of
  sight. Found places persist per map. First discovery speaks "new exit/place" with
  a one-shot sound.
- Beacon: L3 locks the last spoken entry (same entry again cancels, another entry
  retargets). After 600 ms a 620 Hz ping repeats, 160-1200 ms by remaining route,
  panned toward the next route leg, aimed ahead to turn corners early. Routes only
  through explored ground ("helps you return, not discover"); otherwise refused.
  Arrival releases the lock. Silent but kept in menus/dialogue.
- Lessons: R3 was bound by the game, so the lock moved to L3; diagonal radar rays
  were an "overload of cues" and were reverted; an automatic beacon to the nearest
  frontier was rejected as too close to routed navigation; the mod's own
  visited/seen memory proved more reliable than the native automap.

## Last Epoch input facts

- The game uses Rewired through `EpochInputManager` (`rewiredPlayer`); action ids in
  `EpochInputManager.InputActions` (Ability1-5, UsePotion 8, UsePortal 9,
  DodgeRoll 192, Map 14, QuestLog 41...). Bindings live in serialized Rewired user
  data, not on disk; `%USERPROFILE%\AppData\LocalLow\Eleventh Hour Games\Last Epoch\`
  holds no binding file.
- Verified gameplay bindings (DualSense, Rewired `Default` map, live log 22:41,
  layout Default): L1 potion; Square/Triangle/R1/L2/R2 abilities 1-5; Cross
  interact; Circle dodge; Options main menu; **L3 overlay map; R3 portal;
  D-pad up/down "Keybased Item Selection" (choose the nearby object to pick up);
  D-pad left minion AI toggle; D-pad right dungeon ability**; left stick movement.
  Touchpad opens the controller menu wheel (`FixedUiInputs`, right stick navigates
  it). **Create (Share) has no action in any map.** The right stick has no
  `Default` action, only `ControllerMenuH/V` (wheel) and `VirtualMouseH/V`.
- Other maps (`SkillsMenu`, `InventoryMenu`, `WorldMap`, `ItemToolTip`,
  `VirtualKeyboard`...) reuse the same buttons inside their panels; in the world map
  the D-pad steps quests.
- The right stick aims: `ControllerTargeting.RightStickTarget`,
  `RightStickHitTarget(Vector3)`, `GetMostViableTarget(..., rightStickInput)`.
- Mod controller reads today (read-only, game still receives them): right stick up
  flick (`MenuDetailInput.cs:26`, menu details and F7 enemy scan), right stick down
  flick (`MenuDetailInput.cs:27`, F6 orientation), Back/View (`VitalsInput.cs:41`,
  vitals report). Reads use the Rewired gamepad template, so custom remaps are not seen.

## Proposed Last Epoch mapping (awaiting the user)

- The D-pad, L3 and R3 are all bound in gameplay, and the mod only reads input (the
  game still receives it), so they cannot host the scanner.
- Scanner on the right stick as in D2R (up/down category, left/right entry), only in
  gameplay. It replaces today's F6/F7 flicks. Cost: `ControllerTargeting` reads the
  stick for manual aim, so a flick during a fight may nudge aim; the user to confirm
  whether the right stick visibly aims in practice.
- Lock/cancel beacon on **Create (Share)**, the only button with no game action.
  Keyboard equivalents as in D2R.
- Categories: exits, waypoints, portals, NPCs/merchants, stash, shrines
  (`ShrinesManager.allShrines`), quest markers, enemies, players. Quest markers may
  be listed with straight-line distance and direction even when unexplored, because
  the minimap shows them to sighted players; they must not require the minimap to be
  shown.
- Discovered: the mod's own visited/seen memory, or currently on the native minimap.
- Beacon: open question for the user: straight bearing only (like the minimap
  arrow), or D2R's route through explored ground only.
- Existing route-computing guidance (automatic orientation, F6) conflicts with the
  rule and would be turned off by default or removed.

## Controller binding log (installed 2026-10-03)

`Diagnostics/ControllerBindingLog.cs`, config `[Diagnostics] ControllerInputLog`
(default **false** since the research run; the first build used `ControllerBindingLog`,
default true, which is now an unused cfg key). Read-only; no hooks.

- On first sight of a joystick: `[Controller bindings]` lines with each Rewired map
  (category, layout, enabled) and every element → action (`name (descriptiveName)`).
- When the set of enabled maps changes: `[Controller bindings] enabled maps
  (gameplay|menu): ...`, showing which categories are live in each context.
- On every button press, and every axis crossing 0.5 (sticks, triggers):
  `[Controller press] <element> (gameplay|menu): mapped=[actions in enabled maps]
  fired=[actions whose GetButtonDown is true this frame]`.

Research run done (22:41 log): results above. Limitation: in menus the press lines
showed the `Default` map's elements disabled, yet no "enabled maps (menu)" line was
logged; the once-per-second map summary missed that change. Not investigated.

## User decisions (2026-10-03, after the binding run)

- Scanner on the right stick: accepted. The user plays with auto-targeting, so
  right-stick aim is not a concern for them.
- Create (Share) held = a mod menu. While held: Square = life/mana report, Cross =
  start guidance toward the target currently selected in the scanner.
- Today Create alone already gives the vitals report: on the DualSense it is the
  Rewired gamepad template's `back` (log 22:41: the report at line 578 is the same
  frame as the Create press), which `VitalsInput` reads.
- The user's concern: Xbox pads have only View/Menu, and View probably opens the
  game's controller wheel (the DualSense touchpad does). Unverified: needs the
  binding log run with an Xbox pad. Xbox Series pads have a Share button; Xbox One
  pads do not. The modifier should be configurable.

Technical consequence: the mod only reads input, so Square would also cast ability 1
and Cross would interact. While the modifier is held the mod must switch off those
buttons' game bindings and restore them on release, focus loss, scene change,
errors and cleanup (restore only after the buttons are released, to avoid a cast).
Compile-checked against the installed interop: `ActionElementMap.enabled`,
`ControllerMap.enabled` and `MapHelper.SetMapsEnabled` are settable; no hook needed.
Not yet tried live.

## Implementation design (2026-10-03)

User answers: suppression while Create is held is accepted; the right stick drives
the scanner only in gameplay (never with the controller wheel or any menu open,
where it keeps reading tooltips); automatic orientation, F6 and F7 are removed
(`5a494a4`); the beacon routes only through explored ground, as in D2R.
Other pads (8BitDo Ultimate 2C, Xbox One) have only Start/Select, so the modifier
must be configurable later; this first version targets the DualSense.

Code lives in `LeAccess/Accessibility/Exploration/`.

- `ExplorationInput` (input): one command per frame from the right stick
  (edge-triggered flick from neutral, dominant axis: up/down = previous/next
  category, left/right = previous/next entry), from keyboard Ctrl+Numpad 8/2/4/6 and
  Ctrl+B, and from the held modifier (Rewired template `back`, Create on the
  DualSense): Square = vitals, Cross = lock. While the modifier is held in gameplay,
  every enabled Button-type `ActionElementMap` of the player's joysticks is
  disabled; each one is re-enabled after the modifier is released and its own
  button is up, and all of them immediately on leaving gameplay, errors or
  cleanup. Only maps the mod disabled are re-enabled. Axes stay enabled, so the
  player can keep moving.
- `ExplorationCatalog` / `ExplorationProbe` (scanner): categories with entries,
  nearest first; a category step speaks its name and nearest entry; an entry step
  speaks name, distance in steps (1 step = the character's body width) and one of
  eight screen directions (`GuidanceGeometry.Direction`). Places are listed once
  seen (within a seen range with NavMesh line of sight, remembered per fixed-layout
  scene); quest markers and the encounter return arrow are listed when the game
  shows them (minimap visibility not required); enemies are live only.
- `ExplorationBeacon` (beacon): Create+Cross locks the selected entry, again cancels,
  another entry retargets. It keeps an explored-cell map (cells seen with NavMesh
  line of sight) and routes only through it (A* with NavMesh-checked edges);
  otherwise it refuses. A D2R-style ping (about 620 Hz, 80 ms) repeats 0.16-1.2 s
  by remaining route, panned toward a look-ahead point; arrival speaks and releases;
  a route lost for 3 s releases.

## Implemented first version (2026-10-03, not yet heard)

Files in `LeAccess/Accessibility/Exploration/`: `ExplorationInput` + `ExplorationInputLogic`
(input and suppression ledger), `ExplorationCatalog`, `ExplorationProbe`,
`ExplorationSeenMemory` (scanner), `ExploredMap` + `ExploredMapRouting` + `ExploredMemory`
(explored cells, A*), `BeaconState`, `BeaconCueSynthesizer`, `BeaconAudio`,
`NavMeshWalkability`, `ExplorationBeacon` (beacon), `ExplorationAccessibility` (service,
run before vitals in `MenuAccessibility.LateUpdate`). Tests: `LeAccess.ExplorationCatalog.Tests`
(30), `LeAccess.ExplorationInput.Tests` (27), `LeAccess.Exploration.Tests` (133),
`LeAccess.VitalsInput.Tests` (28).

- Categories: objectives (tracked quest markers read without requiring the minimap to be
  shown, `GuidanceTargetReader.TryRead(..., requireMinimapVisible: false)`, plus the
  Morditas return arrow), exits (zone/dungeon transitions), travel (waypoints, portals),
  characters (NPCs, with service role), services (stash, forge, non-NPC merchants), enemies
  (living hostiles within `ControllerTargetingParadigm._SkillTargetMaxDistance`). Chests,
  loot and signposts are left to their own sounds.
- Seen: a place enters the list once within `[Exploration] SeenRange` (20) with NavMesh
  line of sight; off-mesh places (doors, exits) within half that. Remembered per
  fixed-layout scene for the game session (not saved to disk yet).
- Speech: category step "Salidas, 2. Salida hacia X, 12 pasos, arriba a la izquierda";
  entry step "Name, N pasos, direction"; "aquí" within one step. 1 paso = body width.
- Beacon tuning (from the beacon implementation): 2-unit cells, explore radius
  `[Exploration beacon] ExploreRadius` default 20 (raised from 8 so places seen from
  afar can be routed to), 0.4 s route refresh, reroute after 2 units off path or target
  moved, 4-unit look-ahead, pings 0.16-1.2 s saturating at 30 units of route, first ping
  0.6 s after locking, route lost after 3 s, minimum arrival 1.5, A* cap 6000 expansions.
  Ping recipe 620 Hz / 80 ms / 0.15 peak / rear x0.94 from d2r-access `beacon_audio.cpp`.
  Lock "Guiando hacia X, recto/rodeando", refusal "No hay una ruta por zonas exploradas
  hacia X", arrival, route lost, target lost; same entry again cancels, another retargets
  (a refused retarget keeps the old lock).
- Live checks: Create+Square/Cross never cast or interact, and buttons always come back
  (`[Exploration input] game button bindings paused/restored` log lines); quest markers
  with the minimap hidden (`[Exploration] ... objectives=` status every 10 s); floor checks
  on a bridge (`unverified_floors` in the beacon status); performance of the 20 Hz map ticks.
- Not done yet: configurable modifier for pads without Create (8BitDo 2C, Xbox One), saving
  explored ground and seen places to disk, "new place discovered" speech, shrines.

## Saved exploration

- File: `<BepInEx config>/LeAccess/Exploration/<key>.bin`, key `offline-<name>-<classId>` (`online-` when
  `GameplayEnvironment.IsOnlineClient`); the name keeps Unicode letters, invalid file-name characters and spaces become `_`.
  No character name, no persistence.
- Format (`ExplorationStore`, little endian): `LEXP`, int32 version (1); explored scopes: int32 count, each
  ushort-prefixed UTF-8 scope (`scene|variant`), int32 point count, x/y/z float32; seen scopes: int32 count, each
  scope (scene name), int32 key count, ushort-prefixed keys. Reading rejects the whole file on bad magic/version,
  counts over the caps (4096 scopes, 100000 points, 50000 keys per scope), non-finite floats, truncation or trailing
  bytes; a rejected file is renamed `.corrupt` and ignored.
- Cadence (`ExplorationPersistence.Update`, from `ExplorationAccessibility.Refresh`): load on first sight of a
  character or a switch (the previous one is saved first, memories cleared); save when dirty every 30 s, on a scene
  session change and on `Cleanup`. Writes go to `.tmp`, then replace the file.
- Imported cells carry no links: `ExploredMap.Import` makes no NavMesh calls and routing proves each uncached edge
  when the search reaches it (bounded by `MaxExpansions` x 8 per search).
- Not saved: procedural scopes (regenerated), the locked target, catalog selection. Plugin `Unload` is the only
  `Cleanup` path, so a game exit relies on the 30 s save.

### Online saving (2026-10-07, co-op log 16:16)

Report: "online, maps do not count as explored after passing through them".

- In-session memory is fine: `visited_rooms` grows with movement (Z62 2 -> 42, line 6107 onward) and a re-entered
  scene keeps its rooms (Z52 `remembered=47` at line 4658, 49 at 5770).
- Nothing persisted: the 16154-line log has no `[Exploration saved]` line at all, no `Exploration persistence` warning,
  and the config folder holds only `offline-fiona-4.bin` (2026-10-05). Every first entry starts at 2-8 remembered
  places (lines 687, 1337). The save/load path is silent while `CurrentKey()` is null, so the key was null online
  although `getPlayerData()` was non-null (quests spoke, line 3668). The online character is `yarkidius` (line 551);
  the likely gap is a blank `CharacterData.CharacterName`/class online (not provable from the log).
- Fix (`ExplorationKey`, `ExplorationPersistence.CurrentKey`): online, the name falls back from `CharacterData` to
  `CharacterDataTracker.getCharacterName()`, then `LocalPlayerInfoProvider.getCharacterName()`, then the account
  `UserIdentity.Username` of the local rig (`online-account-<name>`); a missing class drops from the key
  (`online-<name>`). Offline keys are unchanged. Online, one `[Exploration saved] online identity key=... data=...
  name=... class=... account=...` line is logged whenever the key changes, so the next log shows which source worked.
- Not changed: the beacon refuses party members, NPCs and the death place whose route crosses unexplored ground
  (lines 7611, 9178, 14657) by design (explored ground only).
- To check in game: a `[Exploration saved] saved key=online-...` line within 30 s of walking, the file under
  `BepInEx/config/LeAccess/Exploration/`, and `loaded key=` plus a high `remembered=` on the next launch.

## Live report fixes (2026-10-04, first listening session, 23:54 log)

The user confirmed the scanner, Create menu and beacon work offline. Fixes:

- Guardiana Leena was never listed although the user walked up and talked to her
  (`[NPC name]` named her; the scanner's status stayed `unseen=5`). The landmark status
  (`candidate=1566276`, `active=15`) proves her listener is in the active scene, so the
  cause is the seen test: her position never passed the NavMesh line-of-sight check (on a
  platform, behind a counter or off the mesh; exact geometry unknown). Places within the
  game's own interaction range (capped at 10 units) or 6 units now count as seen without
  line of sight. The scanner also accepts listeners from any loaded scene, like the NPC
  probe (consistency, not the cause). The status line now names up to six places not yet
  seen (`not_yet_seen=[...]`).
- The stash ("Alijo", twice) was listed under characters: it passes the NPC test. Stash,
  forge and waypoints are now classified before the NPC test.
- Exploration is saved per character (section above); `MenuAccessibility.OnApplicationQuit`
  also flushes, since closing the game skips `Plugin.Unload`.
- Online characters: the code paths use the online player visuals and the player's own
  NavMesh agent like the radar, but nothing has been heard online yet. `origin=False` in the
  `[Exploration]` status would mean no walkable mesh for line of sight and routing.

## Unexplored areas (2026-10-04)

User report (2026-10-04, 00:46 log): in Z62 the quest marker "Escapa de la cámara de los
guardianes" stayed 190-260 steps away, the beacon refused it (`no explored route`, 403 cells)
and nothing listed new ground "like the unexplored rooms in Diablo 2". Port of the D2R
*unexplored room* category (`d2r-access docs/entity-scan.md`, `docs/decisions.md` 2026-09-24):
only the edge of the known map is listed, nothing inside is revealed, the beacon may enter it.

**Rooms: the game's minimap persistence volumes.** `MinimapVolumesManager.volumes` holds one
`MinimapVolume` (`SphereCollider col`, `hasBeenTriggered`) per room; `Statuses` (bool per volume,
same order) is what the game saves with the character (`MinimapVolumesSaving`, packed into a
ulong, 64 slots). Bundle survey of build 25690669 (scratch scripts, not kept): 185 of 360 scenes
have exactly one manager plus volumes, the other 175 (Z12 and, presumably, procedural maps)
have none. Z62 has 61 volumes, Z32 43, Z42 55, Z52 62; most scenes have at most 64, but WE505 has
74 and C2S10 157 (how those are saved is unknown). Volumes are all named
`MinimapPersistenceVolume` under `MinimapVolumesHolder`, so identity is the array index. Z62 radii
are 8.0-21.3 (median 14.9), centres 8-19 apart, spheres overlapping in a chain along the paths.
All colliders are serialized with `m_IsTrigger=False`; whether `Start` turns them into triggers is
unknown, so the mod does not rely on the game's flags alone.

- `MinimapRoomReader`: finds the manager with `FindAnyObjectByType` at most every 5 s while
  absent, reads centre (`TransformPoint(center)`) and radius (`radius` x largest lossy scale)
  once per scene session, statuses and `hasBeenTriggered` on every scanner refresh.
- A room is **visited** when the game marks it, or once the player stands inside its sphere;
  the latter is remembered with the seen places (`room|<version>.<count>|<index>`, saved per
  character). A room is **listed** when not visited and at least one explored cell lies inside
  its sphere (the player has seen some of its ground). Position is the sphere centre (it may sit
  inside scenery; it gives direction and distance), arrival radius the sphere radius.

**Frontier, for scenes without rooms** (`ExploredFrontier.cs`). Each explored cell checks its eight
neighbour columns: walkable on the same floor, unexplored, a straight step away, and opening onto
an unseen region of at least 6 cells (breadth-first over unexplored walkable columns, verdict
kept), so a pocket behind a crate is not listed. Checks run newest cell first within 24 NavMesh
queries per beacon tick (a region search may add up to 48), only while the scene has no rooms;
saved cells are checked again after loading. Links are grouped into 8x8-cell areas per floor,
anchored at the edge nearest the group's middle; key `frontier:x,z,floor`, arrival 2 cells.

**Scanner and beacon.** First shipped as *Zonas sin explorar* / "Zona explorada"; the same day these
became D2R's *sala inexplorada N* and *sala N* ([D2R categories](#d2r-categories-2026-10-04)). A locked
unexplored room is reached on entering it ("Has llegado a sala inexplorada N", with the room cue); a
frontier area when its edge is seen or reached. The radar marks opening cues toward a listed area
([unexplored mark](navigation-radar.md#latency-and-the-unexplored-mark-2026-10-04)).

**Evidence and limits.** 338 exploration checks (corridor edge moving ahead, pillar pocket not
listed, doorway, budget and backlog, saved maps, rooms visited/listed/layout change) and 45
catalog checks. Not heard in game. Live checks: the `[Exploration]` status line now has
`unexplored= source=rooms|frontier rooms= visited_rooms= frontier_pending=`; confirm
`source=rooms rooms=61` in Z62 and `source=frontier` in Z12 or a dungeon; whether `Statuses`
already marks rooms entered before this version (`visited_rooms` right after loading); that
listed rooms point along real paths; the frontier's cost in a large saved map.

## D2R categories (2026-10-04)

The user, after the first version: "I want the same categories as the D2R radar; I asked you to
implement the exploration mode as it is there and you did it halfway." Sources:
`d2r-access src/entity_radar.h` (`Category`), `src/map_survey.cpp` (`CategoryOrder`),
`src/map_survey_service.cpp` (speech), `src/native_entities.cpp` (`ObjectCategory`),
`src/terrain_service.cpp` (`RememberPlaces`), `docs/entity-scan.md`, `docs/exploration.md`.

**Order and Last Epoch sources** (`ExplorationClassifier`, `ExplorationProbe`):

| D2R | Spoken | Last Epoch |
|---|---|---|
| Exit | salida | zone and dungeon transitions ("Salida hacia X"), same-scene transitions, echo continuations |
| Portal | portal | portals (`PortalVisualController`) |
| Waypoint | punto de viaje | `service.waypoint` |
| UnexploredRoom | sala inexplorada | minimap rooms glimpsed, never entered, "sala inexplorada N"; frontier areas where a scene has no rooms |
| Room | sala | minimap rooms entered or revealed by the game, "sala N" (N = volume index + 1) |
| Quest | misión | quest markers and the encounter arrow the game shows |
| Npc | PNJ | relay/`NPCInteraction`, or a name tooltip plus conversation on an actor body |
| Player | jugador | every other player (`NearbyPlayerProbe.TryReadAll`), in sight or not, online only; entry "username, character name, Nivel N" (name and level only when known) |
| LastDeath (D2R Corpse) | última muerte | "donde moriste": where the player last stood alive before dying in this scene; only a destination, Last Epoch leaves no corpse to pick up (renamed from "cadáver" 2026-10-08 at the user's request) |
| Stash | alijo | `service.stash` |
| Enemy | enemigo | live hostiles on screen (camera viewport, as a sighted player sees them) or within skill-targeting range (2026-10-08; before only targeting range, so the category was usually absent while exploring) |
| Treasure | tesoro | chest identities of the world sounds (`WorldCueIdentityReader`), until opened |
| Shrine | santuario | listeners under `ShrinesManager.GetAllShrineObjects()` (refreshed every 5 s) |
| Interactable | mecanismo | everything else with a name: notes and journals (name tooltip and conversation, no actor), the forge and other non-NPC services, levers, signposts |
| Item | objeto | ground items whose label the loot filter shows, in sight now |

The catalogue's old numbers stay in the saved keys (exits 1, waypoints 2, NPCs 3, stash 4), so seen places
survive the change; new categories are appended. Lore notes were listed as characters before.

**Speech.** A category step says the category name, then its nearest entry ("salida. Salida hacia X, 12
pasos, arriba y a la izquierda"); counts are not spoken. Empty category: "aquí no hay X"; nothing at all:
"aquí no hay nada". Diagonals read "arriba y a la derecha" as in D2R.

**What is remembered** (D2R `MapSurvey::Fixed`): exits, waypoints, the stash, NPC names, visited rooms and the
death place are saved per character and scene; portals, chests, shrines, objects and items only for the game
session (`ExplorationSeenMemory.Persistent`).

**New places** (`TerrainService::RememberPlaces`): the first sighting of an exit says "El radar ha registrado
una salida nueva: X" (destination name), a waypoint or the stash "... un lugar nuevo: X", queued, with the
discovery chime once per refresh. The exit back to the scene the player came from is the way back, not a
find. `[Exploration] Discoveries` turns both off.

**Room sounds** (`docs/exploration.md` "Supplied exploration sounds"): the whole D2R recordings, re-imported
with `tools/import_audio.py` settings (`Loops/unexplored_room.wav` 1.04 s, `inside_unexplored_room.wav`
0.93 s, `discovery.wav` 2.85 s). The nearest unvisited room (not the one the player is in) loops from its
nearest point within `[World object sounds] LoopRange`, with that volume, on its own output
("Exploration rooms"); entering an unvisited room plays the entry cue once (never on the first pass after
loading, never again on return), and is the beacon's arrival sound for such a room. Menus and dialogue
silence both. `[Exploration] RoomSounds`.

**Not ported:** D2R's full-assistance mode, the reach filter (Ctrl+U), quest-object finds (Last Epoch has no
readable quest-source flag on interactables), following another player through unexplored ground, and
corpses (none exist). Rooms come only from minimap volumes; frontier scenes have no room sounds, as D2R's open
levels have none.

**Evidence.** Exploration 357 and catalog 45 checks (order, speech composed from the test's own templates,
category walk, key compatibility, room numbering/entry/baseline, nearest room point and pan, persistence
filter, death replacement). Nothing heard in game. Status line fields: `quests players items death unexplored
source rooms visited_rooms frontier_pending`; log lines `[Exploration] El radar ha registrado ...`,
`entered unexplored room N`, `death place x,z`.

## Elevation blind spot (2026-10-08, End of Time report)

Scanner distance and direction are horizontal only (`ExplorationCatalog.Describe`), so a target on
another floor sounds close. Fin de los Tiempos: Elder Gaspar stands at Y 16.89 (`EoT.unity`
`SceneObjects/Gameplay/0.9.1 Mastery Adjustments/Elder Gaspar`, x -22.66 z 67.58) on the central hex
platform, right above the Forgotten Knight (Y 1.2, x -24.84 z 58.46). The scanner said "Anciano Gaspar,
7 pasos, arriba" while the user stood under him, and the beacon correctly refused (upper floor unexplored).
The default-agent NavMesh (`sceneassets_EoT_unity.bundle`, 9 tiles, 407 polygons, decoded with the
`tools/map_section_navmesh.py` layout plus neighbour links) is one connected component: the way up is the
east stairs north from about x -18 z 78, a ramp west along z ~101 to x -38, south to the upper island
(x -43 z 87, Y 16.7), then east and south across to Gaspar. The user had climbed the first flight and
turned back because the marker was behind; found Gaspar later the same day. Open: say when a target is on another floor
(|dY| >= `ExploredMap.LevelHeight`); awaiting the user.
