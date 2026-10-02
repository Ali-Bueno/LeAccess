# Map sections and environment descriptions

Implemented for Steam 1.5.0.1, build 25663618. Default speech identifies concrete
structures and rooms when the player reaches them. The user explicitly prefers
these local landmarks over generic material changes or an entry summary.
The existing central speech sink serves these readers:

- **Fixed scenes:** the reviewed two-bridge Camino ancestral pilot plus an
  automatically extracted catalog of bridges, stairs and walkways. See the
  [generator and coverage evidence](map-section-generation.md).
- **Live structures:** fresh contacts beneath the player's feet identify the
  current structural owner, including mesh/sloped pieces and repeated names.
  This path runs in any loaded gameplay scene, including echoes and generated
  layouts, without requiring a fixed coordinate profile. See
  [live place detection](runtime-map-places.md).
- **Generated caves/tombs:** native hallway tags, current room number, entrance/exit metadata and
  connected-door counts from the actual generated level, with native floor/elevation checks. See
  [PCG evidence and limits](pcg-map-sections.md).
- **Ground (optional, disabled by default):** changes between eleven verified footstep surface categories,
  including wood, grass, dirt, gravel, mud, sand, paved ground, metal, snow, ice
  and water. This works independently of fixed map profiles; see
  [surface sampling](map-surfaces.md).

These are descriptive accessibility labels, not official names for every room.
A generic room allocation does not establish a kitchen, throne room or tavern.
Connection counts describe the generated layout; they do not establish that a
passage is currently open or locate a physical crossroads. Missing or ambiguous
door data omits the count. Fixed cave entrances without corroborated floor geometry are
not included by the fixed catalog. Availability in every scene does not establish
that every visual landmark has recognizable ownership metadata. Unknown contacts
remain silent.

## Speech and controls

Settings are created under `[Map sections]` on the next launch:

```ini
AnnounceSections = true
DescribeMapOnEntry = false
AnnounceSurfaceChanges = false
```

The first setting enables local section announcements while exploring. The
other two options remain off unless explicitly enabled. An optional entry
summary lists verified landmark categories; optional ground feedback describes
material changes. Neither is part of the user's requested default experience.
With both disabled, the reader does not prepare summaries or probe floor materials.
No additional shortcut or automatic movement is introduced.

Polling follows the existing 200 ms world-reader cadence. Two matching samples
confirm a section or changed description. Remaining in one section is silent;
confirmed departure permits a return announcement. Multipart geometry and a
shared `speechGroup` suppress speech at seams and across consecutive pieces of
the same generic structure. The old named bridges retain their distinct labels.
Static and live versions of the same generic category share speech identity.
Consecutive native hallway allocations also share a category within a generation.

The offline actor's actual radius supplies boundary hysteresis, provided the
old source remains valid. A strict hit on a different section takes precedence.
Height bounds separate nearby floors. Generated rooms require actual complete
floor cells; bounding rectangles alone never establish a room.
Live contacts can identify a more specific place within a broader catalog piece,
such as stairs on a bridge or a room containing walkway floor meshes. Matching
categories keep the fixed profile's specific name, including the two pilot
bridges. Without a live classification, the current fixed section, its boundary
margin, and native generated room/passage metadata provide successive fallbacks.
An actual new live place overrides an old boundary margin.
Missing static catalogs, changed anchors or unknown builds do not disable live
place reading. Generation identity remains part of procedural speech identity.

Menus/dialogue suspend reading while preserving delivered content. Pending text
is revalidated at the current position before delivery. The current section
follows zone-name speech. If explicitly enabled, an overview precedes sections
and changed ground follows them. Unknown foot samples cancel
pending surface text without repeating the last known surface on recovery.
Other gameplay readers retain their existing ordering and critical life/mana
feedback retains interruption priority.

Section and ground deliveries log `[Map section]` and `[Map surface]` respectively.
The [nearby sign/exit reader](map-landmarks.md) remains complementary.

## Fixed-map safeguards

All `LeAccess.MapSections.*.json` resources load together; duplicate scene/build
profiles are rejected. Each profile requires the exact installed build hash,
scene name/path and three unique non-collinear environmental anchors. Authored
Monolith layouts additionally require structural and collider guards for every
section; their scene category alone no longer excludes them. Procedural roots
still cannot consume fixed coordinate profiles.

Generated sections additionally identify their unique structural source and
all contributing BoxColliders. Runtime caches their transforms but rechecks
activity, scene/path ownership, all sixteen world-matrix entries, collider
availability, enabled/trigger state, local center and local size. A changed or
missing contributing child invalidates that section even when its parent and
global map anchors remain unchanged. Failed discovery retries no more than once
per second, allowing asynchronous scene objects to arrive without frame scans.

Geometry uses one logical section with one or more polygon components. Gaps,
holes and different height bands remain separate. Overlaps prefer priority,
then the smallest containing component, then ordinal section ID. Component
bounding boxes reject distant positions before polygon tests. Finite values,
invalid polygons, duplicate identifiers, malformed matrices and ambiguous paths
are rejected. A complete malformed static catalog disables static coverage;
independent procedural and surface readers can continue.

## Extending coverage

Run `tools/extract_map_sections.py` against a new installation, review the
coverage/rejection report, and validate the generated catalog before shipping.
It uses real named structural pieces plus physical and navigation geometry;
[the navigation decoder](map-navigation-format.md) documents its layout guards.
This replaces hand-authoring every repeated bridge or staircase. New structural
families need evidence and a conservative rule before joining the generator.

For distinctive rooms/corridors, an authored profile may still provide better
labels. `campaign.json` preserves the [Old Road pilot](map-section-data.md).
Single polygons remain supported alongside `components`, `speechGroup`, and
optional `source`/collider guards. Add names to both translation catalogs.

A material/texture census supplies research candidates, not live descriptions:
unused dependency assets do not prove visible scenery. Ground descriptions use
owned live footstep materials and a fresh floor sample instead. This does not
infer biome, indoor status, room function or physical danger from a texture name.

## Validation limits

Pure geometry/state, runtime ownership/anchors/sources, service priority,
procedural rooms, live surface adapters and extracted catalog data have isolated
source-linked checks. Actual source points are sampled independently from the
installed colliders/navigation data. Current totals and deployment hash are in
[STATUS.md](../STATUS.md).

The game remains closed for the user's next test. Check representative stairs,
walkways and bridges; continuity between pieces; menu closure; language changes;
a generated cave/tomb entrance and another room. Confirm no map summary or
generic material announcements with default settings. Ground-transition checks
apply only when that optional reader is explicitly enabled. Compare room/player
height and active source paths when silent.
Static and host tests cannot establish audible results or complete map coverage.
