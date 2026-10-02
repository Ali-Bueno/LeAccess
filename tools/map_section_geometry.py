"""Geometry compaction for analysis-only authored map extraction."""
from collections import defaultdict

from shapely.geometry import Polygon
from shapely.ops import unary_union

MAX_UNION_VERTICES = 128  # Bound runtime polygon validation quadratic edge checks.


def normalize_components(components):
    """Round safely and remove identical polygons, including reversed/cyclic vertices."""
    output, seen = [], set()
    for component in components:
        raw = [(round(p["x"],6), round(p["z"],6)) for p in component["polygon"]]
        points = [p for i,p in enumerate(raw) if i == 0 or p != raw[i-1]]
        if len(points) > 1 and points[0] == points[-1]:
            points.pop()
        if len(points) < 3 or len(set(points)) != len(points):
            continue
        polygon = Polygon(points)
        if not polygon.is_valid or polygon.area <= 0:
            continue
        cycles = []
        for vertices in (points, list(reversed(points))):
            cycles.extend(tuple(vertices[i:]+vertices[:i]) for i in range(len(vertices)))
        low, high = round(component["minY"],6), round(component["maxY"],6)
        key = (low, high, min(cycles))
        if key in seen:
            continue
        seen.add(key)
        output.append(dict(minY=low, maxY=high, polygon=[dict(x=x,z=z) for x,z in points]))
    return output


def compact_components(components):
    """Union only identical vertical bands; preserve original parts when a union has holes."""
    bands = defaultdict(list)
    for component in normalize_components(components):
        bands[(component["minY"],component["maxY"])].append(component)
    result = []
    for (low,high), parts in bands.items():
        shapes = [Polygon([(p["x"],p["z"]) for p in c["polygon"]]) for c in parts]
        union = unary_union(shapes)
        polygons = [union] if union.geom_type == "Polygon" else list(union.geoms)
        for polygon in polygons:
            if not polygon.is_valid or polygon.interiors or len(polygon.exterior.coords)-1 > MAX_UNION_VERTICES:
                result.extend(c for c,s in zip(parts,shapes) if polygon.covers(s.representative_point()))
            else:
                result.append(dict(minY=low,maxY=high,polygon=[dict(x=x,z=z) for x,z in list(polygon.exterior.coords)[:-1]]))
    return normalize_components(result)
