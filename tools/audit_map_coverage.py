"""Inventory installed scene families and evidence; names alone never establish runtime coverage."""
from collections import Counter, defaultdict
from concurrent.futures import ThreadPoolExecutor, as_completed
import argparse
import json
from pathlib import Path
import re
import struct

import UnityPy

from map_section_metadata import SCENE_TYPES


PLACE_WORDS = {
    "bridge": r"\bbridge\b", "stairs": r"\bstairs?\b|\bstaircase\b",
    "walkway": r"\bwalkway\b|\bboardwalk\b|\bcatwalk\b",
    "room": r"\brooms?\b|\bchambers?\b|\bkitchen\b|\btavern\b|\bhall\b",
    "corridor": r"\bcorridor\b|\bhallway\b|\bpassage\b", "tunnel": r"\btunnel\b",
    "cave": r"\bcave\b|\bcavern\b|\bgrotto\b",
    "junction": r"\bjunction\b|\bcrossroads?\b|\bintersection\b|\bfork\b",
    "courtyard": r"\bcourtyard\b|\bplaza\b|\bsquare\b",
    "dock": r"\bdock\b|\bjetty\b|\bpier\b|\bwharf\b",
    "street": r"\bstreet\b|\balley\b|\broad\b", "gate": r"\bgate\b|\bentrance\b|\bdoor\b",
}


def read_registrations(root):
    scripts = {o.path_id: o.read_typetree()["m_ClassName"]
               for o in UnityPy.load(str(root / "monoscripts.bundle")).objects if o.type.name == "MonoScript"}
    objects = []
    for obj in UnityPy.load(str(root / "PermaLoad.bundle")).objects:
        if obj.type.name != "MonoBehaviour":
            continue
        data = obj.read_typetree()
        if scripts.get(data["m_Script"]["m_PathID"]) == "SceneList":
            objects.append((obj.get_raw_data(), data["m_Name"]))
    if len(objects) != 1:
        raise ValueError("Expected one installed SceneList")
    raw, name = objects[0]
    marker = struct.pack("<i", len(name.encode())) + name.encode()
    header = raw.find(marker)
    if header < 0:
        raise ValueError("SceneList header not found")
    collection = (header + len(marker) + 3) // 4 * 4
    count = struct.unpack_from("<i", raw, collection)[0]
    candidates = []
    for offset in range(collection + 4, len(raw)-20, 4):
        length = struct.unpack_from("<i", raw, offset)[0]
        if not 2 <= length <= 128:
            continue
        try:
            scene = raw[offset+4:offset+4+length].decode("ascii")
        except UnicodeDecodeError:
            continue
        if not re.fullmatch(r"[A-Za-z][A-Za-z0-9_ .-]*", scene):
            continue
        field = (offset+4+length+3)//4*4
        if field+12 > len(raw):
            continue
        kind, era, display_length = struct.unpack_from("<iii", raw, field)
        if not 0 <= kind < len(SCENE_TYPES) or not 0 <= era <= 4 or not 0 <= display_length <= 512:
            continue
        if display_length == 0 and not re.fullmatch(r"[A-Z][A-Za-z0-9_]*", scene):
            continue
        try:
            display = raw[field+12:field+12+display_length].decode("utf-8")
        except UnicodeDecodeError:
            continue
        if any(ord(c) < 32 for c in display):
            continue
        end = (field+12+display_length+3)//4*4
        candidates.append(dict(scene=scene, sceneType=SCENE_TYPES[kind], displayName=display,
                               recordOffset=offset, headerEnd=end))
    rows = []
    for candidate in candidates:
        if any(row["recordOffset"] <= candidate["recordOffset"] < row["headerEnd"] for row in rows):
            continue  # DisplayName itself can resemble an empty-header record.
        rows.append(candidate)
        if len(rows) == count:
            break
    if len(rows) != count or len({r["scene"] for r in rows}) != count:
        raise ValueError("SceneList registration count/uniqueness mismatch")
    for index, row in enumerate(rows):
        if index == len(rows)-1:
            row["nativePcgRoot"] = None  # Last record's boundary needs full nested-schema decoding.
            continue
        value = struct.unpack_from("<i", raw, rows[index+1]["recordOffset"]-4)[0]
        if value not in (0, 1):
            raise ValueError("SceneDetails final isPcgRootScene bool does not match verified layout")
        row["nativePcgRoot"] = bool(value)
    return rows


def scan_vocabulary(bundle):
    environment = UnityPy.load(str(bundle))
    objects = {o.path_id: o for o in environment.objects}
    gos = {o.path_id: o.read_typetree() for o in objects.values() if o.type.name == "GameObject"}
    transforms = {o.path_id: o.read_typetree() for o in objects.values() if o.type.name == "Transform"}
    by_go = {t["m_GameObject"]["m_PathID"]: k for k,t in transforms.items()}
    parents, children, paths, colliders = {}, defaultdict(list), {}, {}
    for go,key in by_go.items():
        father = transforms[key]["m_Father"]["m_PathID"]
        parents[go] = transforms[father]["m_GameObject"]["m_PathID"] if father in transforms else None
        children[parents[go]].append(go)

    def path(go):
        if go not in paths:
            paths[go] = (path(parents[go])+"/" if parents[go] is not None else "") + gos[go]["m_Name"]
        return paths[go]

    def count_colliders(go):
        if go not in colliders:
            types = [objects[c["component"]["m_PathID"]].type.name for c in gos[go]["m_Component"]
                     if c["component"]["m_PathID"] in objects]
            result = Counter(t for t in types if t.endswith("Collider"))
            for child in children[go]:
                result.update(count_colliders(child))
            colliders[go] = result
        return colliders[go]

    counts, with_collider, examples = Counter(), Counter(), []
    for go in by_go:
        name = gos[go]["m_Name"]
        normalized = re.sub(r"(?<=[a-z])(?=[A-Z])", " ", name).replace("_", " ")
        if re.search(r"spawner|mushroom|camera|popup|navigable|soundless|ambient|effect|notification", normalized, re.I):
            continue
        categories = [k for k,p in PLACE_WORDS.items() if re.search(p, normalized, re.I)]
        location = path(go)
        if not categories or "/Visuals/" not in location and "/Gameplay/" not in location:
            continue
        counts.update(categories)
        physical = count_colliders(go)
        if physical:
            with_collider.update(categories)
            examples.append(dict(path=location, categories=categories, colliders=dict(physical)))
    return dict(scene=bundle.name[6:-13], objects=len(gos), counts=dict(counts),
                withCollider=dict(with_collider), examples=examples)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("game", type=Path)
    parser.add_argument("catalogs", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--extraction-evidence", type=Path)
    parser.add_argument("--place-census", type=Path)
    args = parser.parse_args()
    root = args.game / "Last Epoch_Data/StreamingAssets/LEAssetBundles"
    bundles = sorted(root.glob("scene_*_unity.bundle"))
    registrations = read_registrations(root)
    metadata = {r["scene"]: r for r in registrations}
    if args.place_census:
        vocabulary = json.loads(args.place_census.read_text())
    else:
        vocabulary = []
        with ThreadPoolExecutor(max_workers=4) as executor:
            futures = [executor.submit(scan_vocabulary,bundle) for bundle in bundles]
            for index,future in enumerate(as_completed(futures),1):
                vocabulary.append(future.result())
                if index % 40 == 0:
                    print(f"Inspected {index}/{len(bundles)} scene hierarchies", flush=True)
    vocab = {row["scene"]: row for row in vocabulary}
    if set(vocab) != {b.name[6:-13] for b in bundles}:
        raise ValueError("Place census does not match every installed named scene bundle")
    sections = Counter()
    for catalog in args.catalogs.glob("*.json"):
        for profile in json.loads(catalog.read_text())["maps"]:
            sections[profile["scene"]] += len(profile["sections"])
    extraction = {n["scene"]: n for n in json.loads(args.extraction_evidence.read_text())} if args.extraction_evidence else {}
    ledger = []
    for bundle in bundles:
        scene = bundle.name[6:-13]
        info, candidates, result = metadata.get(scene), vocab[scene], extraction.get(scene,{})
        reason = None if sections[scene] else result.get("skip", "No shipped fixed section profile")
        if info and info["nativePcgRoot"]:
            reason = "Native procedural root; live generated room/tile buffers required"
        if scene in ("Between", "TransitionScene"):
            reason = "Auxiliary empty/loading scene, verified from serialized scene objects"
        if scene == "Testing_Funtimes_Zone":
            reason = "Developer/testing scene with empty SceneList display name"
        examples = []
        for category in PLACE_WORDS:
            example = next((e["path"] for e in candidates["examples"] if category in e["categories"]),None)
            if example:
                examples.append(dict(category=category, path=example))
        ledger.append(dict(scene=scene, sceneType=info["sceneType"] if info else None,
                           displayName=info["displayName"] if info else None,
                           nativePcgRoot=info["nativePcgRoot"] if info else None,
                           catalogSections=sections[scene], nameCandidates=candidates["counts"],
                           candidatesWithSerializedCollider=candidates["withCollider"],
                           missingFixedEvidence=reason, rejectedCandidates=result.get("rejected",{}),
                           candidateExamples=examples))
    installed = {row["scene"] for row in ledger}
    missing = [dict(scene=r["scene"],sceneType=r["sceneType"],displayName=r["displayName"])
               for r in registrations if r["scene"] not in installed]
    summary = dict(namedSceneBundles=len(bundles), sceneListRegistrations=len(registrations),
                   installedClassCounts=dict(Counter(row["sceneType"] for row in ledger)),
                   registeredWithoutMatchingNamedBundle=len(missing),
                   namedProductionGameplayLayouts=sum(row["sceneType"] not in (None,"NonGameplay")
                                                       and row["scene"] != "Testing_Funtimes_Zone" for row in ledger),
                   developerTestingLayouts=1 if "Testing_Funtimes_Zone" in installed else 0,
                   nativePcgRootTemplates=sum(row["nativePcgRoot"] is True for row in ledger),
                   fixedProfiles=len([row for row in ledger if row["catalogSections"]]),
                   fixedSections=sum(sections.values()))
    output = dict(buildHash=(args.game/"Last Epoch_Data/build_hash.txt").read_text().strip(),
                  warning="Names and serialized collider counts are candidates, not proven runtime place coverage.",
                  summary=summary, installedScenes=ledger, registeredWithoutNamedBundle=missing)
    text = "{\n" + ",\n".join(json.dumps(key)+": "+(
        "[\n"+",\n".join(json.dumps(row,separators=(",",":")) for row in value)+"\n]"
        if isinstance(value,list) else json.dumps(value)) for key,value in output.items()) + "\n}\n"
    args.output.write_text(text,encoding="utf-8")
    print(json.dumps(summary),flush=True)


if __name__ == "__main__":
    main()
