# Static map section generation

## Steam asset refresh — build 25690669

On 2026-10-03, all **58 fixed profiles / 750 sections** and all **three tent
canopy profiles** were independently re-extracted from the installed sources
for build hash `5cec0d5385ee8ac4d7d557676361703c0f058813`. The 57 generated
layouts used `--scenes-from` with the previous accepted-scene evidence; Z12
bridges and Z32 canopies used their separate extraction tools.

Every anchor, source/collider guard, floor band, polygon and canopy contour
compared structurally equal to the prior resources. All 58 scene/navigation
bundle pairs and the tent mesh source also retained their previous SHA256.
The new build binding is therefore supported by fresh extraction rather than
a guessed hash replacement. Runtime build and ownership checks remain intact.
There are still 6,118 generated polygon components and 2,205 collider guards;
no new layouts or sections were added and no full scene census was repeated.

[Current provenance](map-section-build-25690669.json) records installed source,
tool and output hashes, classification and comparison results. Detailed
extraction output and previous-resource backups are in
`%TEMP%/LeAccess-map-refresh-25690669-20261003/`.

The installed-data suite passes 51,203 checks and the runtime-place suite passes
264, including four new independent canopy build-binding checks. Updating the
test evidence before the resources reproduced both stale-catalog failures.
The main build with `SkipPluginDeployment=true` has zero warnings/errors; no
game, audio device or installed file was opened or modified for extraction.

## Steam 1.5.1 refresh — build 25672295

On 2026-10-02 all **58 shipped profiles / 750 sections** were regenerated against
the installed `build_hash.txt` value
`bf5a46c674908886ecdabb6e9906b8a3bf81ec5b`. Previously both catalogs still bound
the older `c8576b3db279ad61f09835dd69cc9d589e5a5cbe`; the exact runtime guard
therefore disabled their fixed-map coverage after the update.

The generator re-read installed MonoScripts, SceneList classification, hierarchy,
colliders and default-agent navigation data for the 57 previously shipped
generated profiles. `extract_oldroad_sections.py` independently regenerated Z12.
All anchors, paths, source/collider guards, height bands and polygons compared
structurally equal to the prior profiles; only their verified build binding
changed. The runtime hash check was retained. Tests now assert the independently
recorded current hash on every generated profile and reject the previous hash.

[Provenance](map-section-build-25672295.json) records the two installed source
bundle SHA256 values for each of the 58 scenes, source metadata, generator/tool
hashes and resulting catalog hashes. Full extraction notes and previous catalog
backups remain in `%TEMP%/LeAccess-map-refresh-25672295-20261002/`. Reproduction
used the documented generator's `--scenes-from` option with the previous
`full-monolith-generated-evidence.json` (57 accepted scenes); rejected scenes and
the separate 360-scene coverage census were not reclassified. Accordingly the
[coverage ledger](map-coverage-ledger.json) remains historical census evidence,
not a claim of a new complete scene audit.

No new fixed categories or map coordinates were added. The totals below remain
valid for the regenerated catalogs. Runtime/source ownership checks and the
user's next listening test still determine whether a profile can speak.

## Generator and coverage

`tools/extract_map_sections.py` builds annotations from the installed scene
bundles. It uses analysis-only UnityPy 1.25.3 and Shapely and imports the verified
navigation decoder in `tools/map_section_navmesh.py`. Neither Python package
is a runtime dependency. No game launch, native hook, or installed-file mutation
is needed.

Run in an isolated analysis environment:

```powershell
python tools/extract_map_sections.py GAME_DIRECTORY OUTPUT_JSON EVIDENCE_JSON
```

For a faster reproducible refresh of previously accepted scenes, append
`--scenes-from PREVIOUS_EVIDENCE_JSON`. A full run examines all 360 scene bundles.

The generated catalog is separate from `campaign.json`; Z12 is deliberately
excluded because its two bridges already have a manually reviewed pilot.

The final generated catalog contains **57 maps, 748 structural sections, and
6,118 polygon components**: 70 bridges in 22 maps, 135 stair sections in 18,
and 543 walkways in 22. Cave entrance candidates did not meet the geometry and
ownership requirements. It is approximately 2.73 MB; the largest polygon has
91 vertices. This supplements the two Old Road bridges, not every map or room.
There are 2,205 contributing collider guards. Full-precision source sizes remain
finite and nonzero, preserving signed values. Python JSON
parsing took approximately 43 ms in the analysis environment; this is not a
measurement of the plugin's C# parsing or runtime traversal.

Covered internal scene identities and classification are in the complete
[coverage ledger](map-coverage-ledger.json). Sixteen generated profiles are
Monolith layouts. Adding the separate Old Road pilot yields 58 fixed profiles
and 750 fixed sections; this is not complete coverage of every place.

The initial 360-scene candidate census found structural bridge names in 123
scenes, stairs in 183, walkways in 61, and cave entrances in six. These counts
include unverified candidates, repeated prefabs, and echo layouts. Geometry,
unique ownership, active-state provenance, and scene-classification checks reduce
that candidate set to the shipped coverage above.

## Acceptance rules

- Read actual `scene_*_unity.bundle` objects, not prefab names alone.
- Recognize structural bridge, stair, walkway/boardwalk/catwalk, or cave entrance
  names under `Visuals/Foreground`. The names choose a translated category;
  internal identifiers are never spoken. Organizational folders are excluded.
- Follow descendants by serialized object identity. Duplicate sibling names are
  common, so strings cannot safely identify which colliders belong to a piece.
- Require a unique path for the section's structural owner and every contributing
  collider owner. Each collider owner must have exactly one BoxCollider.
  `source` stores the owner's world matrix and `source.colliders` stores each
  collider path/world matrix/local center/local size. Runtime checks must reject
  a section if any source is absent, inactive, moved, resized, disabled, or changed
  into a trigger. Root anchors alone cannot detect externally controlled children.
  Signed finite nonzero serialized BoxCollider sizes are preserved verbatim,
  including negative components. They are not normalized with
  `abs`: runtime must compare the actual signed `BoxCollider.size`. A mismatch
  fails closed. For geometric corner enumeration, signed dimensions enumerate
  the same eight physical corners; axis-aligned extents use absolute products.
  Collider center and size preserve their original float values, without
  six-decimal rounding that could turn tiny nonzero components into zero.
  Truly zero serialized dimensions are excluded.
- Reject inactive ancestors, background, broken/collapsed/destroyed decoration,
  vegetation, spawners, previews, LOD variants, and explicitly dynamic variants.
- Reject unknown MonoBehaviour or Animator/Animation ancestors. Reject behavioral
  or animated descendants, except the two read-only footstep material holders,
  which are allowed consistently on the owner, ancestors, and descendants.
  Scenes containing generator/PCG behaviors are excluded from this static path.
- Only enabled non-trigger BoxCollider faces are eligible. Compose all parent
  transforms and inspect all six faces: imported prefabs can rotate local axes.
  The selected face must be the horizontal upper face in world space. Sloped
  faces and unsupported mesh colliders are skipped rather than bounded by a
  guessed render rectangle.
- Intersect that physical face with default-agent, default-Walkable NavMesh
  polygons at the matching authored height. Keep each intersection separately;
  multipart regions preserve gaps and do not introduce a convex hull.
- Use navigation polygon floor heights plus/minus the scene's authored climb
  allowance. This handles the real offset between physics and navigation floors.
- Require three exact unique environmental Transform paths with non-collinear
  X-Z positions; bind them to scene name/path and installed build hash. Reject
  scenes with insufficient anchors. Store bundle SHA256 in analysis evidence.
- Exclude NonGameplay by the installed SceneList's serialized
  `SceneDetails.Name`, `sceneType`, `era`, and `DisplayName` fields. Accept only
  a unique validated record. Monolith profiles retain all source/collider guards;
  unguarded profiles and native procedural scenes cannot use this static path.
  Era/theme names such as Ancient are not evidence of broken or dynamic geometry.

One section owns all corroborated components of one structural object, so
crossing collider or navigation polygon seams does not change section identity.
Overlapping components are allowed; they never expand outside a physical face.
`speechGroup=textKey` lets continuous neighboring pieces share a spoken category.
Compaction unions only components with identical height bands. Unions with holes,
invalid geometry, or more than 128 vertices retain their original components;
no simplification or convex hull fills unknown space. Six-decimal rounding removes
consecutive duplicate vertices and rejects remaining duplicates/invalid polygons.
The 128-vertex limit bounds runtime polygon validation cost. Horizontal-face
tolerance 0.0001 addresses float quaternion conversion noise; source/anchor
tolerance 0.01 follows the manually reviewed pilot's positional policy.

## Coverage limits

This is a conservative category generator, not a room detector. For example,
B80 `OrobyssRoom` and `EterraRoom` contain both walls and floors; their aggregate
renderer bounds are not evidence that the entire rectangle is walkable. Generic
`SpawnerRoom(Clone)` names do not supply authored room names. Cave entrances
without a corroborated horizontal floor are skipped. Curved/sloped mesh stairs
can remain uncovered even when their names are recognizable.

Likewise, material and texture vocabulary is supplementary evidence, not proof
that a surface is currently visible or traversable. A census of all 360 installed
`sceneassets_*_unity.bundle` files found candidate names for stone in 127 scenes,
vegetation in 81, water in 62, snow/ice in 62, sand in 49, wood in 37, ruins in 26,
lava in 17, and fabric/interior in five. These are dependency-level minima;
shared external assets are not fully resolved. Rendering or authoritative
footstep-material ownership must corroborate a future environment description.
The implemented runtime path uses the player's actual footstep material rather
than dependency names; see [verified footstep surfaces](footstep-surfaces.md)
and [surface announcements](map-surfaces.md).

Static extraction establishes geometry and identity. Runtime validation still
needs to check active anchors and player height on representative campaign
maps, changes of scene variant, and silence beside rather than on the selected
surface. Procedural room topology requires a separate generated-level source.

## Independent geometry fixtures

These points were sampled from freshly read installed BoxCollider faces and
checked against installed navigation cells, independently of generated polygon
centroids. Outside points lie one world unit beyond the complete source's physical
X maximum; they assert exclusion from that source section, not from every nearby
section. The arena lobby demonstrates the authored approximately 2,000-unit
parent offset, so missing parent transforms would fail its fixture.

| Scene / section | Installed collider path ID | Inside world X, Y, Z | Outside source X, Y, Z |
| --- | ---: | --- | --- |
| `ArenaLobby_bridge_156` | 29484 | 1954.206845, 30.206070, 2004.171852 | 1958.346866, 30.206070, 2004.171852 |
| `B7S10_stairs_67` | 26124 | -23.849960, 0.023737, 33.559982 | -20.030682, 0.023737, 33.559982 |
| `B50_walkway_1297` | 40378 | 148.540284, 0.128857, -3.913961 | 156.690234, 0.128857, -3.913961 |
| `M_A10_bridge_84` | 46111 | 34.885097, -0.995152, 2.031567 | 45.123678, -0.995152, 2.031567 |

Sources are respectively:

- `LobbyOffset/SceneObjects/Visuals/Foreground/Bridges Folder/BridgeGroup (2)/Cube`.
- `SceneObjects/Visuals/Foreground/Navigation Folder/NavMeshStairs (3)`.
- `SceneObjects/Visuals/Foreground/NewCourtyard/Floor Folder/Walkways Subfolder/TOE_Walkway_RoadT`.
- `SceneObjects/Visuals/Foreground/Manmade Folder/Buildings Folder/Bridge/LR_Road_Small_1x (3)`.

The navigation climb allowance is 0.4 for the first two and approximately 0.45
for B50; M_A10 also uses 0.4. Its actual six collider sources have maximum X
44.12367798510517, independently establishing the outside point. B7S10's
original local collider size is `[-1, 1, 1]`, illustrating why
the source comparison preserves signed sizes rather than guessing normalization.
