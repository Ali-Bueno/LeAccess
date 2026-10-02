# Installed map coverage audit

The installed build contains **360 named scene bundles**. The serialized
SceneList declares **390 records**, of which 358 match those bundles. The other
two installed scenes are `Between` (no GameObjects) and `TransitionScene`
(loading UI). Thirty-two registered names have no matching named scene bundle;
this does not establish that those names are unreachable or absent from aliases.

The inventory therefore contains **354 named production gameplay layouts**, one
developer testing layout, and five non-gameplay/loading/auxiliary layouts. These
are scene identities, not distinct public zone names or every procedural seed.

| Installed SceneList class | Named bundles | Fixed profiles |
| --- | ---: | ---: |
| Normal | 136, including developer testing | 27 |
| Monolith | 175 | 16 |
| Town | 16 | 7 |
| Arena | 17 | 2 |
| Dungeon | 9 | 6 |
| Pinnacle | 2 | 0 |
| NonGameplay | 3 | 0 |
| Outside SceneList | 2 | 0 |

The shipped catalogs contain **58 fixed profiles and 750 sections**: 57 generated
profiles plus the two manually reviewed Old Road bridges. The generated portion
has 70 bridges, 135 stair sections, and 543 walkways. This is verified geometry
coverage for those pieces, not complete landmark coverage of those 58 maps.

## Recoverable inventory

[The ledger](map-coverage-ledger.json) records all 360 installed scenes, exact
class/display name, native procedural marker, fixed section count, naming
candidates, collider-backed candidate counts, and extraction rejection reasons.
Every naming count is explicitly a candidate, not proof of an active walkable
place. [The audit tool](../tools/audit_map_coverage.py) reproduces the inventory:

```text
python tools/audit_map_coverage.py GAME_DIRECTORY CATALOG_DIRECTORY OUTPUT_JSON
```

Optional `--extraction-evidence` adds the generator's rejection evidence;
`--place-census` reuses a census only after validating its complete scene set.
The tool reads assets offline; UnityPy is an analysis dependency only.

The complete extraction examined all 360 scene bundles. It accepted 57 and
skipped 270 without corroborated structural sections, 20 without one expected
navigation dataset, six absent/non-gameplay metadata records, four invalid
navigation polygon datasets, one invalid tile-count dataset, one procedural
generator scene, and the existing pilot. Candidate-level rejection counts were
15,177 ambiguous owner paths, 5,940 without verified floors, 120 ambiguous
collider sources, and 689 behavioral owners. Repeated prefab instances inflate
these counts; they cannot be converted into a missing-place percentage.

## Concrete expansion sources and limits

The native procedural roots are `PCG_Cave`, `PCG_Tomb`, and `PCG_Cemetery`.
Their generated `Level.Rooms`, room rectangles/heights, doors, and grammar
metadata provide current layout topology rather than a fixed profile per seed.
The final SceneList record `WE539` has an unresolved serialized tail boundary;
its procedural flag remains unknown in the offline ledger instead of guessed.

Live contact with an actual floor can identify authored structural ancestors
across scene families, including sloped or mesh floors rejected by the static
BoxCollider generator. Concrete examples include D50
`DeepCorridorCatacomb/Sidewalk`, B80 `EterraRoom`/`OrobyssRoom` floor descendants,
A30 `Council_Chamber_Floor_01`, B4S10 `TortureRoomCenter/WelrynRuinsSlope/Collider`,
and A3S10 `FloorFolder/CaveFloor`. These support translated descriptive
categories; they do not supply official localized room names.

Several tempting sources do not establish a place:

- Dun1Q10 `HallwayGroup` descendants contain ceiling `Fade` trigger boxes, with
  no identified owned solid floor. Visual occlusion is not room-entry evidence.
- G110/G111/G115 `4WayIntersectionSmall` contain walls, fades, vases, and coral;
  no explicit floor child was identified. A name does not prove four open paths.
- `SpawnerRoom(Clone)`, `CliffSquare`, `DockSquare`, and background rooms are
  unsuitable room or plaza names. Room-wide renderer bounds include walls/gaps.
- No collider-backed kitchen or tavern branch was established by this census.

`WorldAreaEnterListener` exposes radius, stay duration, tags, and trigger callbacks
(`listeningAreaRadius`, `requiredStayDuration`, `triggerTags`, `OnTriggerEnter`),
but no localized section name. HideableSystem/HideableCollisionTester expose
visual occlusion colliders and scanning, not a verified named player-room API.
Neither source justifies announcing arbitrary object identifiers.

All-scene eligibility of live readers and native procedural topology improves
availability beyond fixed profiles. Complete semantic coverage of every room,
street, cave, and fork still requires authored evidence; this audit does not
claim it. The game remained closed throughout extraction and verification.
