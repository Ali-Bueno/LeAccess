# Runtime map places

`RuntimeMapPlaces` complements fixed map annotations by inspecting the physical
floor under the current player. `RuntimeSceneryPlaces` separately describes nearby
authored model footprints. Both work independently of scene IDs, catalog
coverage, fixed/generated layout classification, and material announcements.
It returns a place identity and localization key; the existing map-section
service confirms movement between places and controls speech priority.

## Evidence and contact

- The installed 1.5.0.1 `FootstepRaycastTrigger` exposes the current visual owner,
  foot transforms, ray distance and layer mask. Native `HandleStepRaycast`
  (`0x15B5340`) adds 0.5 to the foot's vertical ray origin. This adapter uses the
  same origin and current runtime range/mask, without calling a footstep/audio
  method or reading cached `lastCollider`/emitter material data.
- Fresh downward rays choose the nearest physical collider regardless of its
  name or material holder. Unknown ground therefore blocks an older/named floor
  underneath. Trigger, disabled, destroyed and player-owned colliders are ignored;
  a closer foreign-scene collider, actor, invalid normal or ambiguous tied contact
  prevents classification. Tied coplanar colliders may agree on the same
  structural category; rooms/context must also agree on the same native owner.
  An unknown or conflicting tied collider vetoes the result. Actual upward-facing
  sloped and mesh surfaces work.
  Unity documents that [RaycastAll order is undefined and ray origins inside a
  collider do not hit that collider](https://docs.unity.com/en-us/engine/6000.7/script-reference/unityengine/physics/raycastall).
- Player, native trigger, living visual owner, foot transforms, active scene and
  contact hierarchy are revalidated. The current local actor's cached
  `actorSync.gameplayActor` must point back to that exact actor before its
  `actorVisuals` can authorize a separate visual hierarchy. A legacy visual on
  the player's own hierarchy remains supported when no cached sync exists.
  A trigger is discovered only within the authorized visual hierarchy and
  cached; unavailable/ambiguous discovery retries once per
  second. There are no global scene searches, new hooks or runtime dependencies.
  Trigger `actorVisuals` must match that owner exactly, and feet must descend from
  its visual root. Replacing the visual owner during a form change invalidates
  discovery immediately, even within the same player session.
- Physical feet must agree when they contact different categories or when one
  foot is still on unnamed floor. The enclosing service supplies additional
  confirmation and deduplication. A missing foot contact contributes no category;
  an ambiguous/invalid nearest contact vetoes the other foot.
- `MapPlayerFloor` shares the existing generated-room clearance calculation:
  actual body radius and the current owned agent's native base offset/climb.
  A ray hit must also lie in that vertical band around the current player.
  Long foot rays therefore cannot announce a bridge on a different floor merely
  because it is two meters below an airborne player. Nonfinite heights and
  foreign/reparented/disabled agents cannot expand the allowance. The bound is a
  conservative location check; it is not a replacement for a native grounded flag.

## Structural names

Only the collider's own active ancestry can supply a **floor** label. Nearby renderers,
textures, materials, scene names and detached signs cannot label the floor.
Camel-case/underscore/digit-separated authored words select translated category
keys; internal asset names are never spoken.

Installed scene evidence includes `BoardedBridge` in Z12, `LR_Bridge_01_Combined`
and `LR_Stairs007` in A30, `TOE_Walkway_RoadI/RoadT/RoadX` and
`TOE_Walkway_Stairs` in B80/C10, and `FarHarborDock`, `Pier_Stairs2`,
`Wood_Set_Bridge`, `WalkableStairsCube` and `EterraStairsLOD` in H10.
Live physical ownership allows moving structures, duplicate names, sloped
colliders and active LOD geometry that cannot be safely stored as fixed polygons.

Folders/groups and whole scene roots do not name a structural place. Background,
broken/rubble, effects, spawners, objectives and interactables are excluded.
Railings, pillars, walls, roofs and shields cannot be mistaken for their parent
bridge/walkway. Examples include `BridgeShield1`, `WETA_Woodbridge_sfx`,
`IsleBrokenBridge2` and `TOE_Walkway_Railing_Metal_Straight`.

## Authored context

Installed B80 roots `EterraRoom` and `OrobyssRoom` own actual
`TOE_Walkway_RoadX/RoadI` and `TOE_Base_Square` floor colliders. C10/B50
`NewCourtyard/Floor Folder/Walkways Subfolder` owns walkway floor pieces.
Those explicit contexts can label their own contacted floor. A descendant must
identify a physical floor/road/walkway or verified `TOE_Base_Square`; organizational names such as
`Floor Folder` do not count. This prevents a crate or decorative collider inside
the same room group from defining the room's interior.

The B80 `P_TOE_Room_01_Walls` prefab contains real floor descendants, so a
verified floor can pass through that ancestor container. A wall collider itself
still fails. Stairs and bridges take precedence over encompassing room/courtyard
context; a generic walkway can yield to the more informative room. Context
identity follows the actual owner; adjoining anonymous structural pieces share
their category for speech deduplication.

Additional actual floor families found in the installed scene census:

| Place | Installed floor/owner evidence |
|---|---|
| Council chamber | A10/A80 `CouncilChamberFloor_Middle`; A30 `Council_Chamber_Floor_01` |
| Torture room | B4S10 `TortureRoomCenter`, with its physical center collider |
| Corridor | D50 `SnakeWall Folder/DeepCorridorCatacomb` |
| Tunnel | E1S10/E20/E2S10 `UberAque_Tunnel` |
| Cave | A3S10 `CaveFloor`; A50 `FloorCave` |

A corridor or tunnel named on its actual contacted structural part can be read
without treating the surrounding wall folder as a floor. A generic cave asset
does not imply an interior: the cave category requires the explicit cave-floor
family. Authored intersection context is supported only with a contacted floor
descendant and means "Junction", without inferring open-exit counts. The inspected
G110 `4WayIntersectionSmall` owners did not own verified floor colliders: their
remaining objects were under `NotWalkableObjects Folder/WalkwayCoralAddV2/Biomass_021`.
Those branches are explicitly rejected, so the junction label does not establish
coverage there. `HallwayGroup` candidates similarly owned only ceiling fade
triggers, not floor. No kitchen or tavern floor boundary was found by the census.

## Limits and validation

This is map-independent detection, not evidence that every floor has a semantic
label. Unnamed or ambiguous geometry remains silent. `RoadT`/`RoadX` do not prove
an open junction, and no junction count is inferred from prefab shape.
The native foot source must be available; a missing trigger or unowned/dead
visual hierarchy produces no guessed fallback. The installed visual-prefab audit
verified one enabled trigger and two owned foot references for all five base
classes (native range 3) and Werebear, Spriggan and Swarmblade forms (range 5).
An agent bound to a NavMesh alone does not prove physical ground contact; nearby
scenery therefore says "Nearby", never "inside" or a floor-contact label.
Materials and entry summaries remain disabled
by default.

Run `dotnet run --project tests/LeAccess.RuntimeMapPlaces.Tests` for deterministic
classifier, physical-contact, hierarchy, priority, lifecycle and failure checks.
No game was launched for this implementation. Controller/listening validation
is still needed on the installed game.

## Silent-contact correction

A subsequent user log contained only fixed bridge announcements in Z12/Z22.
The old runtime tests assumed the visual prefab always descended from the gameplay
player object. That requirement could reject the game's separate actor/sync/visual
ownership before any floor ray was cast. Current tests cover a completely detached
visual root, exact sync backlinks, unavailable or foreign syncs, stale legacy
visual caches, visual replacement, and own visual colliders.

Native `PlayerFinder.getPlayerActor` (`0x17A9F10`) obtains its cached actor on the
cached player GameObject; `getPlayerVisuals` (`0x17AA3F0`) independently attempts
the visual component on that same object. The latter is not an ownership proof
for separate visual roots. The inspected direct fields `Actor.actorSync` and
`ActorSync.gameplayActor/actorVisuals` provide the explicit association without
calling the mutating `Actor.GetActorSync()` creation path.

`MapPlaceDiagnostics` records at most 16 informational messages per actual
player/scene session, including at most three distinct unnamed physical floor
paths. Stages distinguish missing visual ownership, absent/ambiguous triggers,
foreign feet, successful rig capture, missing ground, rejected height/ownership,
unnamed geometry and recognized place. These are log diagnostics only, never
screen-reader announcements. They do not assert that this ownership issue caused
every missing place in the user's run; the next run can identify remaining stages.

## Camp scenery: installed 1.5.1, 2026-10-02

The user's `BepInEx-152324.log` changes from Z22 to Z32 at line 1810 and names
**Keeper's Camp / Campamento de los guardianes** at 1873. The footstep rig is
available: owned local visual, native range 3, mask 85984769, clearance 0.5.
Lines 1865 and 1965/1967 encounter unnamed Terrain and `Navmesh Folder/Cube`
colliders; 2055 successfully names `Stairs (1)`. These are real contacts, but
they cannot identify decorative buildings beside the path. Separately, the
fixed catalogs still held the previous build hash and could not match this
installed build; the catalog refresh is recorded in its own provenance.

Read-only comparison with today's working D2R implementation:
`D:/code/c++/diablo 2 access project/d2r-access/docs/places.md:68` and
`src/places.cpp:74`. Its useful change measures distance to the transformed
**footprint**, not just an NPC/prefab origin. Walking distance and actual
interior volumes prevent naming a kitchen through a wall. D2R's room acoustic
volumes do not establish equivalent interiors in Last Epoch.

Fresh `scene_Z32_unity.bundle` and its referenced meshes from Steam build
25672295 establish these useful, bounded model families:

| Category | Authored owner and current source |
|---|---|
| Tents | 41 `Tent_01/02/03` MeshFilter+MeshRenderer objects under `Tents Folder`; no own colliders. Example GO13676 `Tent_01 (4)` uses sceneassets mesh6692287729664568065, local center `(0,3.406266,0)`, extents `(6.020132,3.417642,6.585278)`. Its transformed base is around `(-5.05,4.010,27.29)`. |
| Wagon | Four `Wagon1B`/`Wagon1B(Empty)` groups. Read the exact `WagonBody` child, e.g. GO16480, mesh5197301198422176590 `WagonCombined` in `assets_1162bb217239bd18.bundle`. Other boxes/planks do not expand its footprint. Example transformed base `(-50.629,6.009,54.830)`. |
| Statue of Eterra | GO15817 `Temple Folder/Circle_Statue/Eterra_Statue_Model`, mesh7660974856051224027 in `assets_b991a5071bb14fff.bundle`: local footprint about 2.89 by 3.25 units, base `(35.090,3.878,31.643)`. The parent platform's 27.9-unit bounds do not define the statue's reach. |
| Gateway | Eight `Bridge_Gate_001/002` meshes on the two bridge groups. Example GO1917 uses mesh8438115556616732491 in `assets_8db916e52c8d30a9.bundle`, transformed base `(35.393,4.078,-17.540)`. This describes the structure, not a zone destination. |

These sample renderers are enabled and have no static-batch submeshes. Runtime
code reads each renderer's **live local bounds and transform**; none of the
sample IDs, coordinates or scene names are hardcoded. Exact verified model
families can work in other scenes using the same assets. Internal model names
are translated to category labels and never spoken literally.

The current loaded scene's renderers are indexed once per player/scene session,
including inactive phase variants; per-poll reads recheck active state, owner,
scene, family, actor exclusion and background/damaged/effect ancestry. Empty
indexes do not cause repeated whole-scene searches. Rotated/nonuniformly scaled
local footprints avoid the empty corners of a world-aligned bounding box.
Distance is to that footprint, with reach one current body width (twice the
larger of actual actor radius and native navigation radius). Base height must
match the existing body/base-offset/climb allowance.

The owned native NavMesh agent supplies radius, agent type and area mask. Its
component can be disabled by native movement while those query settings remain
valid; inactive/foreign/destroyed ownership still rejects the query. Both
the player and nearest approach must sample onto that same floor; a clear
NavMesh segment establishes the short walking route. A body-midpoint physics
ray additionally rejects an intervening wall, including cases where sampling
finds our side of the wall. The prop's own collision and local player's body
are excluded. No movement, interaction, NavMesh mutation or new hook is used.

Scenery has its own state in `MapSectionAccessibility`. A fully validated
arrival can speak immediately; departure still requires two observations to
avoid repeating the same prop after a single missing sample. It yields
to zone/menu/dialogue and floor speech, then says **"Nearby: Tents"** / **"Cerca:
Tiendas de campaña"**. Thus a bridge does not hide its gateway. Adjacent pieces
share category identity; obsolete deferred text is canceled on leaving range,
and loading/session reset clears the cache and pending speech. Bounded existing
map diagnostics include indexed count, owned navigation settings and first
confirmed source/distance for each family.

Coverage remains specific: `ShrineCenter` is a large ruin grouping, not an
interior volume. `Circle_Fountain` has a large, partly buried mesh; its bounds
do not prove a walkable room or current floor. Torch particle objects called
`Campfire` do not prove an actual campfire location. No tavern/kitchen interior
was found in this Z32 audit. Waypoint, stash and merchant roles belong to the
separate typed interaction/service reader, not these decorative meshes.

Validation: **207** runtime-place checks and **150** map-section service checks
cover source-linked geometry/ownership, walls, floors, rotated/scaled bounds,
inactive phase changes, cache lifetime, priority, localization and stale pending
speech. These are deterministic tests, not listening confirmation in-game.

### First listening follow-up: discovery worked, admission was too strict

The 19:40:15 UTC log was preserved in the session temp folder as
`LeAccess-camp-followup-20261002/BepInEx-194015.log`. This session **does** contain
Z32 (lines 446–694), followed by a new Z12 online character. The installed and
local DLLs matched SHA256 `5D3469AB384E06F5C486FE399E5C33211E4694B9FE12D43B8938B6B0308BB9C8`.

- Lines 460–461 confirm 54 authored renderer footprints and an agent on the
  exact player GameObject: radius 0.1, height 4, type 0, mask -1. Missing assets,
  an absent initial rig or the fixed catalog guard do not explain this run.
- Line 480 actually recognizes `Tent_02 (17)` at footprint distance 0.16378868,
  within the unchanged one-body-width reach of 1.0000001. There is no
  `[Map scenery]` speech anywhere in this session. The previous state required
  two consecutive samples even after all model/route/height checks passed.
  First-valid arrival now speaks when higher-priority speech permits. Deferred
  text is still canceled immediately if the physical evidence disappears.
  The old log records only first detection, so it cannot prove the precise
  number of valid samples or identify every speech delay. A bounded
  `scenery-deferred` diagnostic now records that distinction.
- Line 581 changes to `scenery-nav-missing` while the radar continues to read
  the same player's navigation data. Requiring `isActiveAndEnabled` was not
  appropriate for querying the static NavMesh with an owned component's
  settings. Current native `CollisionAndNavMeshToggler.DisableNavmeshAgent`
  (`0x162B2D0`) calls `UnityEngine.Behaviour.set_enabled(false)` (`0x4017010`)
  at `0x162B399`; the log does not identify which action caused this transition.
  `TryGetQueryAgent` now retains validated settings during that state. The
  stricter active-agent rule remains for expanding physical floor clearance.
- Fresh Z32 `NavMeshData` path7816013741281028484 in
  `sceneassets_Z32_unity.bundle` proves a **baked radius of 0.5**, climb 0.4,
  cell size 0.15, agent type 0. The runtime radius is only 0.1. The previous
  0.1-unit approach offset and sample sphere could both remain inside the
  0.5-unit obstacle clearance excluded during baking. Approach/projection now
  uses the greater of the runtime and native build-setting radii. This changes
  how the nearest walkable edge is found, not the announcement distance;
  route length, wall rejection and floor checks still apply. A source-linked
  regression reproduces the old failure with those exact two radii.

The related suites also pass: PCG 66, map-state 323 and installed map data 51203;
the production build succeeds with deployment disabled. No game was launched
for this correction; the next listening run must confirm which camp references
the user's actual route reaches.
