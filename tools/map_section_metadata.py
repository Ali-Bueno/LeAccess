"""Read installed SceneList classification from its verified serialized field layout."""
import struct

import UnityPy


# Named members in the installed SceneType.cs, in serialized enum order.
SCENE_TYPES = ("Normal", "Monolith", "Arena", "Town", "NonGameplay", "Dungeon", "Pinnacle")


def load_scene_metadata(bundle_root, scripts, scene_names):
    objects = []
    for obj in UnityPy.load(str(bundle_root / "PermaLoad.bundle")).objects:
        if obj.type.name == "MonoBehaviour":
            data = obj.read_typetree()
            if scripts.get(data["m_Script"]["m_PathID"]) == "SceneList":
                objects.append(obj.get_raw_data())
    if len(objects) != 1:
        raise ValueError("Expected one installed SceneList")
    raw, result = objects[0], {}
    for scene in scene_names:
        name = scene.encode("utf-8")
        marker = struct.pack("<I", len(name)) + name
        start, matches = 0, []
        while True:
            offset = raw.find(marker, start)
            if offset < 0:
                break
            start = offset + 1
            if offset % 4:
                continue
            field = (offset + len(marker) + 3) // 4 * 4
            if field + 12 > len(raw):
                continue
            kind, era, length = struct.unpack_from("<iii", raw, field)
            if not 0 <= kind < len(SCENE_TYPES) or not 0 <= era <= 4 or not 0 < length <= 512:
                continue
            try:
                display = raw[field+12:field+12+length].decode("utf-8")
            except UnicodeDecodeError:
                continue
            if any(ord(c) < 32 for c in display):
                continue
            matches.append(dict(sceneType=SCENE_TYPES[kind], displayName=display, recordOffset=offset))
        if len(matches) == 1:
            result[scene] = matches[0]
    return result
