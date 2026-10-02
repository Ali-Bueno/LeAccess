"""Analysis-only extraction; pip install UnityPy==1.25.3 shapely in a scratch venv.

Usage: python tools/extract_oldroad_sections.py GAME_DIRECTORY OUTPUT_JSON
Reads the installed bundles, writes geometry/profile evidence only, never assets.
"""
import collections
import hashlib
import json
from pathlib import Path
import struct
import sys

import UnityPy
from shapely.geometry import Point, Polygon
from shapely.ops import unary_union


def extract(game):
    root = game / "Last Epoch_Data/StreamingAssets/LEAssetBundles"
    bundle = root / "scene_Z12_unity.bundle"
    env = UnityPy.load(str(bundle))
    data = collections.defaultdict(dict)
    metadata = None
    for obj in env.objects:
        if obj.type.name in ("GameObject", "Transform", "BoxCollider"):
            data[obj.type.name][obj.path_id] = obj.read_typetree()
        elif obj.type.name == "AssetBundle":
            metadata = obj.read_typetree()["m_SceneHashes"]
    gos, transforms, colliders = [data[t] for t in ("GameObject", "Transform", "BoxCollider")]
    by_go = {t["m_GameObject"]["m_PathID"]: k for k, t in transforms.items()}

    def path(go):
        parent = transforms[by_go[go]]["m_Father"]["m_PathID"]
        prefix = path(transforms[parent]["m_GameObject"]["m_PathID"]) + "/" if parent in transforms else ""
        return prefix + gos[go]["m_Name"]

    matrices = {}

    def matrix(key):
        if key in matrices:
            return matrices[key]
        t = transforms[key]
        x, y, z, w = [t["m_LocalRotation"][k] for k in "xyzw"]
        r = [[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)],
             [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)],
             [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]]
        m = [[r[i][j]*t["m_LocalScale"]["xyz"[j]] for j in range(3)] +
             [t["m_LocalPosition"]["xyz"[i]]] for i in range(3)] + [[0, 0, 0, 1]]
        parent = t["m_Father"]["m_PathID"]
        if parent in transforms:
            p = matrix(parent)
            m = [[sum(p[i][k]*m[k][j] for k in range(4)) for j in range(4)] for i in range(4)]
        matrices[key] = m
        return m

    def world(go, point=(0, 0, 0)):
        m = matrix(by_go[go])
        return [sum(m[i][j]*point[j] for j in range(3))+m[i][3] for i in range(3)]

    names = {go: path(go) for go in by_go}
    counts = collections.Counter(names.values())
    base = "SceneObjects/Visuals/Foreground/Deco Folder/BoardedBridge"
    anchor_paths = [base, base + " (1)", "SceneObjects/Visuals/Foreground/MicroVerse/Terrain"]
    anchors = []
    for name in anchor_paths:
        assert counts[name] == 1, "Anchor missing or ambiguous: " + name
        go = next(go for go, p in names.items() if p == name)
        anchors.append(dict(path=name, **dict(zip("xyz", map(lambda v: round(v, 6), world(go)))), tolerance=.01))

    nav = next(o for o in UnityPy.load(str(root / "sceneassets_Z12_unity.bundle")).objects
               if o.type.name == "NavMeshData").read_typetree()
    vertices = []
    for tile in nav["m_NavMeshTiles"]:
        raw = bytes(tile["m_MeshData"])
        assert raw[:4] == b"VAND" and struct.unpack_from("<i", raw, 4)[0] == 16
        count = struct.unpack_from("<i", raw, 24)[0]
        vertices.extend(struct.unpack_from("<fff", raw, 72 + i*12) for i in range(count))
    climb = nav["m_NavMeshBuildSettings"]["agentClimb"]
    sections, evidence = [], []
    for i, prefix in enumerate(anchor_paths[:2], 1):
        quads = []
        for cid, c in colliders.items():
            go = c["m_GameObject"]["m_PathID"]
            if not names[go].startswith(prefix + "/") or "WalkableCube" not in names[go]:
                continue
            assert c["m_Enabled"] and not c["m_IsTrigger"]
            center, size = c["m_Center"], c["m_Size"]
            quad = [world(go, (center["x"]+sx*size["x"]/2, center["y"]+size["y"]/2,
                               center["z"]+sz*size["z"]/2)) for sx, sz in [(-1,-1), (-1,1), (1,1), (1,-1)]]
            quads.append(dict(path=names[go], colliderPathId=cid, topFace=quad))
        assert len(quads) == 3
        union = unary_union([Polygon([(v[0], v[2]) for v in q["topFace"]]) for q in quads])
        assert union.geom_type == "Polygon" and not union.interiors
        inside = [v for v in vertices if union.covers(Point(v[0], v[2]))]
        floors = {v[1] for v in inside}
        assert len(floors) == 1 and inside
        floor = floors.pop()
        sections.append(dict(id=f"oldroad_bridge_{i}", textKey=f"map.section.oldroad_bridge_{i}",
                             priority=0, minY=round(floor-climb, 6), maxY=round(floor+climb, 6),
                             polygon=[dict(x=round(x,6), z=round(z,6)) for x,z in list(union.exterior.coords)[:-1]]))
        evidence.append(dict(quads=quads, navigationFloor=floor, navigationVertices=len(inside), area=union.area))
    profile = dict(scene="Z12", scenePath="Assets/BundledScenes/Z12.unity",
                   buildHash=(game / "Last Epoch_Data/build_hash.txt").read_text().strip(),
                   anchors=anchors, sections=sections)
    return dict(maps=[profile], evidence=dict(bundleSHA256=hashlib.sha256(bundle.read_bytes()).hexdigest(),
                 sceneHashes=metadata, navMeshSettings=nav["m_NavMeshBuildSettings"], bridges=evidence))


if __name__ == "__main__":
    Path(sys.argv[2]).write_text(json.dumps(extract(Path(sys.argv[1])), indent=2) + "\n", encoding="utf-8")
