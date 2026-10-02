"""Read verified Unity VAND-16 navigation footprints for offline map annotation.

The serialized layout is documented in reference/map-section-generation.md.
This module never runs in the plugin and requires the analysis-only UnityPy/Shapely environment.
"""
from dataclasses import dataclass
import math
import struct

import UnityPy
from shapely.geometry import Polygon
from shapely.strtree import STRtree


# Installed Unity 6000.4.8f1: 72-byte header, float3 vertices, 32-byte polygons.
HEADER_BYTES = 72
POLYGON_BYTES = 32
MAX_POLYGON_VERTICES = 6


@dataclass(frozen=True)
class NavigationCell:
    footprint: Polygon
    min_y: float
    max_y: float


def read_tile(raw):
    if len(raw) < HEADER_BYTES or raw[:4] != b"VAND" or struct.unpack_from("<I", raw, 4)[0] != 16:
        raise ValueError("Unsupported navigation tile layout")
    polygon_count, vertex_count = struct.unpack_from("<II", raw, 20)
    polygon_start = HEADER_BYTES + vertex_count * 12
    if not polygon_count or not vertex_count or polygon_start + polygon_count * POLYGON_BYTES > len(raw):
        raise ValueError("Invalid navigation tile counts")
    vertices = [struct.unpack_from("<fff", raw, HEADER_BYTES + i * 12) for i in range(vertex_count)]
    if not all(math.isfinite(c) for vertex in vertices for c in vertex):
        raise ValueError("Nonfinite navigation vertex")
    result = []
    for index in range(polygon_count):
        record = struct.unpack_from("<6H6HII", raw, polygon_start + index * POLYGON_BYTES)
        area_mask, count = record[-2:]
        if (not 3 <= count <= MAX_POLYGON_VERTICES
                or any(v >= vertex_count for v in record[:count])
                or len(set(record[:count])) != count):
            raise ValueError("Invalid navigation polygon indices")
        for neighbour in record[6:6 + count]:
            if neighbour and not neighbour & 0x8000 and neighbour > polygon_count:
                raise ValueError("Invalid navigation polygon neighbour")
        points = [vertices[i] for i in record[:count]]
        projected = [(v[0], v[2]) for v in points]
        if len(set(projected)) != count:
            raise ValueError("Duplicate navigation polygon footprint vertices")
        footprint = Polygon(projected)
        if not footprint.is_valid or footprint.area <= 0:
            raise ValueError("Invalid navigation polygon footprint")
        # Bit zero is Unity's default Walkable area. Other authored area policies need review.
        if area_mask != 1:
            continue
        result.append(NavigationCell(footprint, min(v[1] for v in points), max(v[1] for v in points)))
    return result


class NavigationFootprints:
    def __init__(self, cells, climb):
        if not math.isfinite(climb) or climb <= 0 or not cells:
            raise ValueError("Navigation cells/climb unavailable")
        self.cells = cells
        self.climb = climb
        self.tree = STRtree([cell.footprint for cell in cells])

    def clip(self, footprint, min_y, max_y):
        """Intersect a structural surface with navigation polygons at its authored height."""
        if not footprint.is_valid or not all(math.isfinite(y) for y in (min_y, max_y)) or min_y > max_y:
            raise ValueError("Invalid structural footprint")
        parts = []
        for index in self.tree.query(footprint, predicate="intersects"):
            cell = self.cells[int(index)]
            if cell.max_y < min_y - self.climb or cell.min_y > max_y + self.climb:
                continue
            clipped = cell.footprint.intersection(footprint)
            polygons = [clipped] if clipped.geom_type == "Polygon" else getattr(clipped, "geoms", [])
            for polygon in polygons:
                if polygon.geom_type != "Polygon" or polygon.area <= 0:
                    continue
                # Never fill a hole or repair bad geometry into a larger, guessed area.
                if polygon.interiors or not polygon.is_valid:
                    raise ValueError("Clipped structural polygon requires manual subdivision")
                parts.append(dict(minY=cell.min_y - self.climb, maxY=cell.max_y + self.climb,
                                  polygon=[dict(x=x, z=z) for x, z in list(polygon.exterior.coords)[:-1]]))
        return parts


def load_navigation(bundle):
    environment = UnityPy.load(str(bundle))
    datasets = [obj.read_typetree() for obj in environment.objects if obj.type.name == "NavMeshData"]
    datasets = [data for data in datasets if data.get("m_AgentTypeID") == 0]
    if len(datasets) != 1:
        raise ValueError("Expected one default-agent navigation dataset")
    data = datasets[0]
    if any(data["m_Position"][axis] != 0 for axis in "xyz") or data["m_Rotation"] != dict(x=0, y=0, z=0, w=1):
        raise ValueError("Transformed navigation data requires explicit world conversion")
    cells = []
    for tile in data["m_NavMeshTiles"]:
        cells.extend(read_tile(bytes(tile["m_MeshData"])))
    return NavigationFootprints(cells, data["m_NavMeshBuildSettings"]["agentClimb"])
