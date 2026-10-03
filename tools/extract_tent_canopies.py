"""Extract conservative tent canopy contours from installed meshes (UnityPy/Shapely)."""
import hashlib
import json
from pathlib import Path
import sys

import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
from shapely.geometry import Polygon
from shapely.ops import unary_union


def extract(game):
    data = game / "Last Epoch_Data"
    bundle = data / "StreamingAssets/LEAssetBundles/sceneassets_Z32_unity.bundle"
    profiles = []
    for obj in UnityPy.load(str(bundle)).objects:
        if obj.type.name != "Mesh":
            continue
        mesh = obj.read()
        if mesh.m_Name not in ("Tent_01", "Tent_02", "Tent_03"):
            continue
        handler = MeshHandler(mesh)
        handler.process()
        center, extents = mesh.m_LocalAABB.m_Center, mesh.m_LocalAABB.m_Extent
        # Every contributing face lies strictly above this conservative canopy plane.
        polygons = []
        for submesh in handler.get_triangles():
            for indices in submesh:
                vertices = [handler.m_Vertices[i] for i in indices]
                if min(v[1] for v in vertices) <= center.y:
                    continue
                polygon = Polygon([(v[0], v[2]) for v in vertices])
                if polygon.area > 0:
                    polygons.append(polygon)
        canopy = unary_union(polygons)
        components = list(canopy.geoms) if canopy.geom_type == "MultiPolygon" else [canopy]
        profiles.append({"name": mesh.m_Name, "vertices": mesh.m_VertexData.m_VertexCount,
                         "submeshes": len(mesh.m_SubMeshes),
                         "min": [getattr(center, a) - getattr(extents, a) for a in "xyz"],
                         "max": [getattr(center, a) + getattr(extents, a) for a in "xyz"],
                         "roofFloor": center.y,
                         "components": [{"outer": list(p.exterior.coords)[:-1],
                                         "holes": [list(r.coords)[:-1] for r in p.interiors]}
                                        for p in components]})
    if len(profiles) != 3:
        raise ValueError("Expected exactly three verified tent meshes")
    return {"buildHash": (data / "build_hash.txt").read_text().strip(),
            "sourceSha256": hashlib.sha256(bundle.read_bytes()).hexdigest(),
            "profiles": sorted(profiles, key=lambda p: p["name"])}


if __name__ == "__main__":
    Path(sys.argv[2]).write_text(json.dumps(extract(Path(sys.argv[1])), separators=(",", ":")) + "\n")
