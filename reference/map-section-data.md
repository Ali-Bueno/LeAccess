# Authored map section pilot

The manually reviewed `campaign.json` profile covers two wooden bridges in
**The Old Road**. These are descriptive mod labels, not official named sections.
The separate [generated catalog and runtime readers](map-sections.md) now extend
coverage beyond this initial pilot; the evidence below remains specific to it.

## Installed evidence

- Game: Steam 1.5.0.1, Unity 6000.4.8f1; installed `build_hash.txt`:
  `c8576b3db279ad61f09835dd69cc9d589e5a5cbe`.
- Installed `PermaLoad.bundle`, SceneList object path ID `587261613367599811`:
  serialized `Z12` at byte offset `0x1c3c0`, `The Old Road` at `0x1c3d0`.
- Spanish confirmation: Addressables `localization-assets-shared_assets_all.bundle`,
  `Scenes Shared Data` object `5102725623726984631`, maps `Scene_Name_Z12` to
  entry ID bytes `00 a0 04 f4 ba 9b 2a 01`. The installed
  `localization-string-tables-spanish(spain)(es-es)_assets_all.bundle`,
  `Scenes_es-ES` object `-5463472516920740103`, has the same ID followed by
  **Camino ancestral** (string bytes at offset 9908). Thus the pilot covers the
  user's named zone. The MapObjects table independently confirms this mapping.
- `scene_Z12_unity.bundle` SHA256:
  `81c2196dcbd794275006689f684e93386b85af827aec476b6f46aa326d785f71`.
- Its AssetBundle `m_SceneHashes` preserves the scene path exactly:
  `Assets/BundledScenes/Z12.unity`, internal scene identifier `CAB-scene_Z12_unity`.
- Both bridge roots have only a Transform component. They sit under
  `SceneObjects/Visuals/Foreground/Deco Folder`. Each owns three enabled,
  non-trigger BoxColliders on children named `WalkableCube`, `WalkableCube (1)`,
  and `WalkableCube (2)`. This is authored physical deck geometry, not a
  semantic inference from a decorative mesh alone.

## Coordinates and height

World matrices were composed recursively from serialized local position,
quaternion rotation, and scale. All eight box corners were transformed; the
top four give each walkable deck quad. The profile polygon is the exact planar
union of the three quads, preserving indentations rather than taking a convex
hull or axis-aligned rectangle. Coordinates are in Unity world units and rounded
to six decimals. [Footprint diagram](oldroad-bridge-footprints.svg).

| Section | Collider top Y | Navigation floor Y | Polygon area |
| --- | ---: | ---: | ---: |
| Wooden bridge 1 | -0.003459 | 0.074841 | 53.133180 |
| Wooden bridge 2 | -2.003459 | -1.925159 | 56.810542 |

Navigation evidence comes from `sceneassets_Z12_unity.bundle`, NavMeshData
object `6429231270748307022`. Its tile blobs use `VAND` version 16; vertex count
is the int32 at offset 24 and float3 world vertices begin at offset 72.
Thirteen vertices inside bridge 1's quads share Y `0.07484054565429688`;
ten inside bridge 2 share Y `-1.9251594543457031`. Tile bounds and positions
corroborate this interpretation. The navmesh is approximately 0.078299 above
the collider tops, so collider-only vertical bounds would miss valid players.

The authored NavMesh build settings specify `agentClimb=0.4000000059604645`,
`agentRadius=0.75`, `agentHeight=2.0`, and `cellSize=0.1599999964237213`.
The profile accepts navigation floor plus/minus the authored climb allowance:
bridge 1 `[-0.325159, 0.474841]`, bridge 2 `[-2.325159, -1.525159]`.
This is a documented tolerance policy grounded in the scene's navigation
settings; player-pivot behavior still needs in-game validation.

## Identity anchors and limits

Each exact path occurs once in the extracted scene:

| Transform path below scene roots | World position (X, Y, Z) |
| --- | --- |
| `SceneObjects/Visuals/Foreground/Deco Folder/BoardedBridge` | -26.770000, -0.170000, 22.040009 |
| `SceneObjects/Visuals/Foreground/Deco Folder/BoardedBridge (1)` | -31.970001, -2.170000, 63.809998 |
| `SceneObjects/Visuals/Foreground/MicroVerse/Terrain` | -91.000000, -6.000000, -76.699997 |

These non-collinear environmental anchors avoid a quest letter that can
disappear. Runtime acceptance requires the exact scene name/path/build hash
and all anchors within 0.01 world units; procedural/echo variants are excluded.
The installed extraction proves geometry and identity, but no game was launched.
Tomorrow's checks must confirm anchor availability and player-pivot height,
cross each deck in both directions, and verify one announcement across the
three collider seams and silence on nearby banks and incompatible variants.

Extraction used UnityPy 1.25.3 and Shapely in an isolated temporary Python
environment. Neither is a plugin dependency. No installed files were modified,
no Unity assets or extracted textures are included in the repository, and no
game methods were invoked. The SVG is a generated coordinate diagram.
Reproduce with `tools/extract_oldroad_sections.py GAME_DIRECTORY OUTPUT_JSON`;
its output includes the profile plus source quads and navigation evidence.
