"""Generate conservative installed static map annotations; analysis dependencies only.

Usage: python tools/extract_map_sections.py GAME_DIRECTORY OUTPUT_JSON EVIDENCE_JSON
Requires UnityPy 1.25.3 and Shapely; never writes the installed game.
"""
from collections import Counter, defaultdict
from concurrent.futures import ThreadPoolExecutor, as_completed
import hashlib
import json
import math
from pathlib import Path
import re
import sys

import UnityPy
from shapely.geometry import Polygon

from map_section_navmesh import load_navigation
from map_section_metadata import load_scene_metadata
from map_section_geometry import compact_components, normalize_components


PATTERNS = {"bridge": r"bridge", "stairs": r"stair", "walkway": r"walkway|boardwalk|catwalk",
            "cave_entrance": r"cave[_ ]?entrance|entrance[_ ]?cave"}
EXCLUDE = re.compile(r"background|broken|destroyed|rubble|collapsed|spawner|vegetation|preview|lod|dynamic", re.I)
SAFE_BEHAVIORS = {"ColliderFootstepSoundHolder", "FootstepSoundAreaOverride"}
FLAT_FACE_TOLERANCE = .0001  # Float quaternion/parent-matrix conversion noise.
ANCHOR_TOLERANCE = .01  # Same positional policy as the manually reviewed pilot.


def scene_profile(bundle, scripts, build_hash, metadata):
    scene = bundle.name[6:-13]
    note = {"scene": scene, "candidates": {}, "accepted": {}, "rejected": Counter()}
    if scene == "Z12":
        note["skip"] = "Existing manually verified pilot"
        return None, note
    info = metadata.get(scene)
    if info is None or info["sceneType"] == "NonGameplay":
        note["skip"] = "SceneList metadata absent/ambiguous or excluded scene type"
        note["metadata"] = info
        return None, note
    note["metadata"] = info
    try:
        nav = load_navigation(bundle.with_name("sceneassets_" + scene + "_unity.bundle"))
        env = UnityPy.load(str(bundle))
        gos, transforms, boxes, behavior, scene_paths = {}, {}, {}, {}, []
        animated = set()
        for obj in env.objects:
            typ = obj.type.name
            if typ not in ("GameObject", "Transform", "BoxCollider", "MonoBehaviour", "AssetBundle", "Animator", "Animation"):
                continue
            d = obj.read_typetree()
            if typ == "GameObject":
                gos[obj.path_id] = d
            elif typ == "Transform":
                transforms[obj.path_id] = d
            elif typ == "BoxCollider":
                boxes[obj.path_id] = d
            elif typ == "MonoBehaviour":
                behavior[obj.path_id] = scripts.get(d["m_Script"]["m_PathID"], "UnknownBehavior")
            elif typ in ("Animator", "Animation"):
                animated.add(obj.path_id)
            else:
                scene_paths = [entry[0] for entry in d.get("m_SceneHashes", [])]
        if any("Generator" in name or "PCG" in name for name in behavior.values()):
            raise ValueError("Procedural generator scene")
        if len(scene_paths) != 1:
            raise ValueError("Ambiguous scene path")
        by_go = {t["m_GameObject"]["m_PathID"]: k for k, t in transforms.items()}
        children, parent, paths, matrices = defaultdict(list), {}, {}, {}
        for go, key in by_go.items():
            father = transforms[key]["m_Father"]["m_PathID"]
            parent[go] = transforms[father]["m_GameObject"]["m_PathID"] if father in transforms else None
            children[parent[go]].append(go)

        def path(go):
            if go not in paths:
                paths[go] = (path(parent[go]) + "/" if parent[go] is not None else "") + gos[go]["m_Name"]
            return paths[go]

        def matrix(go):
            if go in matrices:
                return matrices[go]
            t = transforms[by_go[go]]
            x, y, z, w = [t["m_LocalRotation"][a] for a in "xyzw"]
            r = [[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)],
                 [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)],
                 [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]]
            m = [[r[i][j]*t["m_LocalScale"]["xyz"[j]] for j in range(3)] +
                 [t["m_LocalPosition"]["xyz"[i]]] for i in range(3)] + [[0,0,0,1]]
            if parent[go] is not None:
                p = matrix(parent[go])
                m = [[sum(p[i][k]*m[k][j] for k in range(4)) for j in range(4)] for i in range(4)]
            matrices[go] = m
            return m

        def point(go, xyz=(0,0,0)):
            m = matrix(go)
            return [sum(m[i][j]*xyz[j] for j in range(3))+m[i][3] for i in range(3)]

        def descendants(go):
            yield go
            for child in children[go]:
                yield from descendants(child)

        def safe(go):
            current = go
            while current is not None:
                d = gos[current]
                if not d["m_IsActive"] or EXCLUDE.search(d["m_Name"]):
                    return False
                # An ancestor can animate, replace, or disable every descendant.
                if any((c["component"]["m_PathID"] in behavior and
                        behavior[c["component"]["m_PathID"]] not in SAFE_BEHAVIORS)
                       or c["component"]["m_PathID"] in animated
                       for c in d["m_Component"]):
                    return False
                current = parent[current]
            return True

        counts = Counter(path(go) for go in by_go)
        candidates = {}
        for go in by_go:
            category = next((name for name, pattern in PATTERNS.items() if re.search(pattern, gos[go]["m_Name"], re.I)), None)
            organizational = re.search(r"(?:folder|subfolder|group)$", gos[go]["m_Name"], re.I)
            if category and not organizational and "Foreground" in path(go) and safe(go):
                candidates[go] = category
        note["candidates"] = dict(Counter(candidates.values()))
        sections, accepted_gos, examples = [], [], []
        for go, category in candidates.items():
            if counts[path(go)] != 1:
                note["rejected"]["ambiguous_source_path"] += 1
                continue
            ancestor = parent[go]
            while ancestor is not None and ancestor not in candidates:
                ancestor = parent[ancestor]
            if ancestor is not None:
                continue  # One owner for its entire structural piece.
            components, used_boxes, rejected = [], [], False
            for child in descendants(go):
                d = gos[child]
                active = child
                while active != go and gos[active]["m_IsActive"]:
                    active = parent[active]
                if not d["m_IsActive"] or not gos[active]["m_IsActive"]:
                    continue
                for c in d["m_Component"]:
                    cid = c["component"]["m_PathID"]
                    if cid in behavior and behavior[cid] not in SAFE_BEHAVIORS:
                        rejected = True
                    if cid in animated:
                        rejected = True
                    if cid not in boxes:
                        continue
                    box = boxes[cid]
                    if not box["m_Enabled"] or box["m_IsTrigger"]:
                        continue
                    center, size = box["m_Center"], box["m_Size"]
                    corners = [point(child, tuple(center[a]+sign[j]*size[a]/2 for j,a in enumerate("xyz")))
                               for sign in [(x,y,z) for x in (-1,1) for y in (-1,1) for z in (-1,1)]]
                    top = max(v[1] for v in corners)
                    quad = None
                    # Imported meshes often rotate the prefab axes: inspect all six faces.
                    for axis in range(3):
                        others = [a for a in range(3) if a != axis]
                        for side in (-1,1):
                            face = []
                            for a,b in [(-1,-1),(-1,1),(1,1),(1,-1)]:
                                sign = [0,0,0]
                                sign[axis],sign[others[0]],sign[others[1]] = side,a,b
                                face.append(point(child, tuple(center[k]+sign[j]*size[k]/2 for j,k in enumerate("xyz"))))
                            if all(abs(v[1]-top) <= FLAT_FACE_TOLERANCE for v in face):
                                quad = face
                    if quad is None:
                        continue
                    ys = [p[1] for p in quad]
                    if not all(math.isfinite(v) for p in quad for v in p) or max(ys)-min(ys) > FLAT_FACE_TOLERANCE:
                        continue  # Sloped/vertical surfaces require separate evidence.
                    footprint = Polygon([(p[0],p[2]) for p in quad])
                    if not footprint.is_valid or footprint.area <= 0:
                        continue
                    parts = normalize_components(nav.clip(footprint, min(ys), max(ys)))
                    if parts:
                        components.extend(parts)
                        used_boxes.append(cid)
            components = compact_components(components)
            if rejected or not components:
                note["rejected"]["behavior" if rejected else "no_verified_floor"] += 1
                continue
            collider_sources = []
            for cid in used_boxes:
                box = boxes[cid]
                child = box["m_GameObject"]["m_PathID"]
                owned = [c["component"]["m_PathID"] for c in gos[child]["m_Component"]
                         if c["component"]["m_PathID"] in boxes]
                if (counts[path(child)] != 1 or len(owned) != 1 or
                    not all(math.isfinite(box["m_Size"][a]) and box["m_Size"][a] != 0 for a in "xyz")):
                    rejected = True
                    break
                collider_sources.append(dict(path=path(child), matrix=[round(v,6) for row in matrix(child) for v in row],
                                             tolerance=ANCHOR_TOLERANCE,
                                             center=[box["m_Center"][a] for a in "xyz"],
                                             size=[box["m_Size"][a] for a in "xyz"]))
            if rejected:
                note["rejected"]["ambiguous_collider_source"] += 1
                continue
            sections.append(dict(id=f"{scene}_{category}_{go}", textKey="map.section."+category,
                                 speechGroup="map.section."+category, priority=0, components=components,
                                 source=dict(path=path(go), matrix=[round(v,6) for row in matrix(go) for v in row],
                                             tolerance=ANCHOR_TOLERANCE, colliders=collider_sources)))
            accepted_gos.append(go)
            examples.append(dict(path=path(go), pathId=go, category=category, boxColliderIds=used_boxes,
                                 components=len(components)))
        if not sections:
            raise ValueError("No corroborated structural sections")
        # Prefer unique structural/environmental transforms, then persistent visual roots.
        choices = [go for go in accepted_gos if counts[path(go)] == 1]
        choices += [go for go in by_go if go not in choices and counts[path(go)] == 1 and safe(go)
                    and "/Visuals/Foreground" in path(go)
                    and re.search(r"terrain|floor|deco|rock|stone|wall|pillar|column", gos[go]["m_Name"], re.I)]
        anchors = []
        for go in choices:
            pos = point(go)
            if not all(math.isfinite(v) for v in pos):
                continue
            if anchors and sum((pos[i]-anchors[0][i])**2 for i in range(3)) < ANCHOR_TOLERANCE**2:
                continue
            if len(anchors) == 2:
                a,b = anchors
                cross = (b[0]-a[0])*(pos[2]-a[2])-(b[2]-a[2])*(pos[0]-a[0])
                if abs(cross) < ANCHOR_TOLERANCE**2:
                    continue
            anchors.append(pos)
            if len(anchors) == 3:
                break
        if len(anchors) != 3:
            raise ValueError("Insufficient unique noncollinear anchors")
        anchor_objects = []
        for pos in anchors:
            go = next(go for go in choices if point(go) == pos)
            anchor_objects.append(dict(path=path(go), **dict(zip("xyz", [round(v,6) for v in pos])), tolerance=ANCHOR_TOLERANCE))
        note.update(accepted=dict(Counter(e["category"] for e in examples)), examples=examples,
                    bundleSHA256=hashlib.sha256(bundle.read_bytes()).hexdigest(), objects=len(gos))
        return dict(scene=scene, scenePath=scene_paths[0], buildHash=build_hash, anchors=anchor_objects,
                    sections=sections), note
    except Exception as ex:
        note["skip"] = str(ex)
        return None, note


def main():
    game, output, evidence = map(Path, sys.argv[1:4])
    root = game / "Last Epoch_Data/StreamingAssets/LEAssetBundles"
    scripts = {}
    for obj in UnityPy.load(str(root / "monoscripts.bundle")).objects:
        if obj.type.name == "MonoScript":
            scripts[obj.path_id] = obj.read_typetree()["m_ClassName"]
    build = (game / "Last Epoch_Data/build_hash.txt").read_text().strip()
    bundles = sorted(root.glob("scene_*_unity.bundle"))
    metadata = load_scene_metadata(root, scripts, [b.name[6:-13] for b in bundles])
    if len(sys.argv) == 6 and sys.argv[4] == "--scenes-from":
        previous = json.loads(Path(sys.argv[5]).read_text())
        selected = {n["scene"] for n in previous if n.get("accepted")}
        bundles = [b for b in bundles if b.name[6:-13] in selected]
    maps, notes = [], []
    with ThreadPoolExecutor(max_workers=4) as executor:
        futures = [executor.submit(scene_profile, b, scripts, build, metadata) for b in bundles]
        for i, future in enumerate(as_completed(futures), 1):
            profile, note = future.result()
            notes.append(note)
            if profile:
                maps.append(profile)
            if i % 30 == 0:
                print(f"Processed {i}; accepted maps {len(maps)}", flush=True)
                evidence.write_text(json.dumps(sorted(notes, key=lambda n: n["scene"]), indent=2)+"\n", encoding="utf-8")
    maps.sort(key=lambda m: m["scene"])
    notes.sort(key=lambda n: n["scene"])
    output.write_text(json.dumps(dict(maps=maps), separators=(",", ":"))+"\n", encoding="utf-8")
    evidence.write_text(json.dumps(notes, indent=2)+"\n", encoding="utf-8")
    totals = Counter(s["textKey"] for m in maps for s in m["sections"])
    print("Finished", len(notes), "scenes:", len(maps), "maps;", dict(totals), flush=True)


if __name__ == "__main__":
    main()
