# Runtime map places

`RuntimeMapPlaces` complements fixed map annotations by inspecting the physical
floor under the current player. It works independently of scene IDs, catalog
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

Only the collider's own active ancestry can supply a label. Nearby renderers,
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
An agent bound to a NavMesh alone does not prove physical ground contact, so no
second speculative probe is used. Materials and entry summaries remain disabled
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
