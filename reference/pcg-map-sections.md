# Generated rooms and passages

Verified against the installed Steam 1.5.0.1 interop/native build on 2026-10-02;
the game remained closed. This source supplements fixed campaign profiles.

## Authoritative sources

- `last epoch code/LE.PCG/LevelUtility.cs:308`: `TryGetLevel(Scene, out Level)`.
- `last epoch code/LE.PCG/Level.cs:4241`: `GeneratorData`;
  `GeneratorData.cs:261`: retained generated `BakedLevel Level`.
- `Level.cs:4217`: `LevelToWorld`; `:4342`: `IsLevelGenerationCompleted`;
  `:4070`: `RootScene`; `:4094`: `Archetype` (`Cave`, `Tomb`, `None`).
- Installed `LE.PCG.dll`: `BakedLevel.Rooms`, `Tilemap`, `Tileset`, `Seed`;
  `BakedRoom.Rect`, `Height`, `Doors`, `GrammarMetadata`.
  `GrammarNodeMetadata.IsEntrance` / `IsExit` are actual generator flags.
- `BakedTilemap.GetTileHash(int2)` gives four corner `TileKey` values
  (`Empty` / `Floor`); `Heights`, `Size`, `hashPosLookup` retain floor geometry.
  `Tileset.TileSize` supplies real horizontal and vertical tile scale.

Focused decompiled supplementary types and native address map are in
`%TEMP%/LeAccess-pcg-audit-20261002/`; no supplemental runtime package is needed.

## Native evidence and geometry

- `Level.RoomCenterToWorldCoord`, RVA `0x1B69EB0`, reads integer rectangle
  center and room height, multiplies by the retained tileset's three tile sizes,
  then applies `LevelToWorld`. `DoorToWorldCoord`: RVA `0x1B67EC0`.
- `BakedTilemap.GetTileHash`: RVA `0xFC32C0`.
- `BakedTilemap.GetHeightDelta`: RVA `0xFC3040`, reads corner heights using
  `hashPosLookup`, `x + z * Size.x`, and subtracts their minimum. Its result is
  a **delta**, so it must not be used as an absolute floor elevation.
- Installed `LE.PCG.Data.Constants.ROOM_TAG_HALLWAY` is the authoritative room
  category tag; `RoomTemplate.TagMask.HasTag(int)` tests it. The adapter reads
  the native constant rather than assuming a fixed bit index, validates it
  against `RoomTagMask.TAG_COUNT`, and reads the selected retained
  `BakedRoom.Template`. A missing template leaves only the geometry-backed
  room classification; collected templates and failed/invalid tag reads fail closed.
- `pcg.Layout.RoomGenerator.JoinDoorsWithRoom`, RVA `0xFBE420`, creates the
  connector's actual template using `GetTemplate` (`0xFBE97D`), sets its hallway
  tag (`0xFBE99A`), and retains the new placed room using
  `LayoutState.AddNewPlacedRoom` (`0xFBECE6`). This supports announcing a
  **passage while inside its actual floor-backed allocation**, rather than
  estimating a doorway radius or describing a nearby connection count.
- `BakedDoor(GraphRoom.DoorData)`, RVA `0xFA9260`, copies `Occupied` from
  `DoorData`.
- `pcg.Layout.Generic.ConnectRoomsNodeCommand.Operate`, RVA `0xFA9B90`,
  sets both selected door flags true at `0xFAA029` / `0xFAA036`;
  its `Rollback`, RVA `0xFAA200`, clears both at `0xFAA24E` / `0xFAA25B`.
  `PlaceChildRoomNodeCommand.Operate`, RVA `0xFBB0B0`, similarly sets the
  attachment pair at `0xFBB37F` / `0xFBB384`; rollback `0xFBB530` clears the
  reserved door at `0xFBB5D1`. These are successful layout attachment flags,
  not declarations that a passage is currently open or traversable.

`PcgMapSections.TryCapture(world, out section)` inverts `LevelToWorld`, divides
by tile size, checks a full four-corner Floor cell, and bounds player elevation
by the four actual raw heights. Room membership then uses the generated
rectangle; overlapping allocations are rejected. Rectangles alone would include
nonwalkable holes. World height allowance comes from actual actor body radius
or an owned active NavMeshAgent's base offset plus native agent climb; the inverse
matrix's Y-row norm converts this allowance into local coordinates.

## Lifecycle and reporting

- Caller supplies the guarded player snapshot and owns focus/menu/loading/death
  gates. The probe rechecks exact active scene identity, loaded state, active
  level, root scene, completion, and native wrapper lifetime.
- Session identity changes with player session, Level, GeneratorData, BakedLevel,
  room buffer, tilemap buffer, seed, reset, or observed incomplete generation.
  Current geometry is read from those buffers; no global scene scan is used.
- Agent lookup is local to the player hierarchy; missing/destroyed/reparented
  agents are retried, and ownership/activity are checked before use.
- Returns generated room index/number, entrance/exit flags and cave/tomb text key.
  A hallway-tagged selected room returns `map.section.passage`, an unnumbered
  category. Other valid registered allocations without a known archetype return
  the generic numbered `map.section.room`. The installed `LevelArchetype` enum
  contains only `None`, `Tomb`, and `Cave`; no additional named biome is guessed.
  `ConnectedPassages` counts actual Occupied doors as **layout connections**.
  Missing/collected arrays, invalid index/size, duplicate indices or duplicate
  positions make that count unknown; an intact empty array means zero.
  Native errors produce deduplicated warnings; reset clears diagnostics.

## Limits and verification

- Covers registered generated levels with complete retained room/tile geometry,
  not all campaign or echo maps. Generic fallback cannot create room geometry
  for a scene whose native level is absent.
  Room numbers follow generated buffer order; they are mod descriptions rather
  than official localized room names.
- Full-floor interiors are conservative: mixed floor edges, missing corner data,
  ambiguous rooms and unknown online foot clearance can remain unclassified.
- Connections do not identify a junction, guarantee open doors or assess dynamic
  obstacles. No inferred kitchen/tavern/bridge names, wall-as-door counting, input consumption,
  navigation changes or scene mutations.
- `tests/LeAccess.PcgMapSections.Tests`: **66 source-linked checks**, including
  floor holes/height, allocation seams, nonunit world scale, native flags, stale
  buffers, regeneration, missing/foreign/inactive agents, connection flags,
  duplicate/invalid/missing door arrays, diagnostics and reset; authoritative
  hallway tags, changed native tag index, unrelated/adjacent room tags, passage
  floor holes/elevation, missing/collected templates, failed tag reads, invalid
  tag indices, and recovery without stale passage metadata.
  Main no-deploy build passed. Live scene registration and online agent availability
  still need gameplay verification.
