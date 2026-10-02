# Installed navigation geometry decoder

`tools/map_section_navmesh.py` is an offline extraction helper, not plugin code.
It reads Unity 6000.4.8f1 NavMeshData from the installed sceneassets bundles using
the isolated UnityPy/Shapely analysis environment. No runtime dependencies or
game asset binaries are shipped.

## Verified serialized layout

The installed tile blobs begin with `VAND`, version 16. Polygon and vertex counts
are little-endian int32 values at offsets 20 and 24. A 72-byte header precedes
world-space float3 vertices. Following those vertices are 32-byte polygon
records: six uint16 vertex indices, six uint16 neighbour references, a uint32
area mask and a uint32 vertex count. Ground polygons contain three to six vertices.

This resembles [Detour's polygon topology](https://github.com/recastnavigation/recastnavigation/blob/main/Detour/Include/DetourNavMesh.h),
but Unity's serialized version and record layout differ. The decoder follows
the installed data; it does not substitute the upstream current C++ struct.

The interpretation validated all 493 polygons in Z12's sixteen tiles, plus 820
in C10, 3358 in B80, 417 in A30 and 565 in H10. Counts, buffer bounds, vertex
indices, neighbour ranges, finite coordinates, distinct projected vertices and
polygon validity are checked before use. Unsupported formats fail closed.

Only the default agent's single untransformed dataset and area mask 1 (Walkable)
are accepted. A different agent, multiple candidate datasets, transformed data,
unknown area policy or malformed geometry requires further evidence. The scene's
native `agentClimb` determines vertical tolerance; no arbitrary world radius
or screenshot-derived coordinates are introduced.

## Intersections

An STRtree indexes navigation footprints during offline extraction. Structural
horizontal collider faces are intersected with navigation polygons at compatible
heights. Each retained component keeps the navigation cell's elevation range
plus/minus its authored climb allowance. No convex hull or geometry repair
creates a route across a hole, wall or gap. Intersections with interior holes
require subdivision or are rejected.

`tests/MapSectionExtraction/test_navmesh.py` contains twenty offline unit tests
covering malformed layouts, counts, neighbours, indices, gaps, holes, height
bands, separate floors and shared seams. Run with the analysis interpreter:

```powershell
python -m unittest discover -s tests/MapSectionExtraction -v
```

The generated static catalog is additionally parsed by the same C# model used
at runtime and checked against independent installed collider samples. Source
identity, active hierarchy and collider geometry are revalidated in the game;
offline geometry alone is insufficient when quests can change objects.
