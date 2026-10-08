"""Recreate the embedded cues and full loops from the user's D2R recordings and the installed Last Epoch banks."""
import argparse
import functools
import hashlib
import json
import mmap
import pathlib
import shutil
import struct
import subprocess
import tempfile

STREAMING_ASSETS = pathlib.PurePosixPath("Last Epoch_Data/StreamingAssets")


def d2r(relative):
    return ("d2r", relative)


def le(bank, sample):
    return ("le", bank, sample)


# Last Epoch choices: user listening, 2026-10-03 (reference/sound-design.md).
GOLD = le("UI", "sfx_ui_general_goldsmall")
PORTAL = le("Ingame_Events", "sfx_soundevent_timerift_loop1")
ZONE_TRANSITION = le("Environmental", "sfx_enviro_waypoint_loop_v2")

# One-shots with no loop twin: exploration (D2R docs/exploration.md) plays them whole, once.
# The short fallback cues are cut from LOOPS at load time (NaturalCueBank), not shipped twice.
CLIPS = (
    ("discovery", d2r("one shot sounds/exploration/discovered.ogg"), None),
    ("inside_unexplored_room", d2r("item loop sounds/exploration/inside unexplored room.ogg"), None),
)

# D2R loot rarity and type loops (D2RAccess ground_items.cpp); Last Epoch shrine and Nemesis loops
# from the Environmental bank (reference/sound-design.md).
LOOT = "item loop sounds/loot/"

# Whole recordings for the continuous world loop mixer (D2RAccess item_audio.cpp loops each file).
LOOPS = (
    ("magic_equipment", d2r(LOOT + "magic equipment.ogg")),
    ("rare_equipment", d2r(LOOT + "rare equipment.ogg")),
    ("legendary_equipment", d2r(LOOT + "legendary equipment.ogg")),
    ("rune", d2r(LOOT + "rune.ogg")),
    ("gem", d2r(LOOT + "gem.ogg")),
    ("scroll", d2r(LOOT + "scroll.ogg")),
    ("quest_item", d2r(LOOT + "quest item.ogg")),
    ("shrine", le("Environmental", "sfx_enviro_shrine_nature_main_loop")),
    ("nemesis", le("Environmental", "sfx_ui_nemesis_corpse_loop")),
    ("gold", GOLD),
    ("equipment", d2r("item loop sounds/loot/normal equipment.ogg")),
    ("chest", d2r("item loop sounds/loot/chest.ogg")),
    ("stash", d2r("item loop sounds/loot/stash.ogg")),
    ("portal", PORTAL),
    ("zone_transition", ZONE_TRANSITION),
    ("potion", le("Environmental", "sfx_ui_potion")),
    ("tome", le("UI", "sfx_ui_inventory_glyph")),
    ("unexplored_room", d2r("item loop sounds/exploration/unexplored room.ogg")),
)


def convert(source, destination, duration=None):
    limit = ["-t", str(duration)] if duration else []
    subprocess.run([
        "ffmpeg", "-nostdin", "-v", "error", "-y", "-i", str(source), *limit,
        "-map_metadata", "-1", "-vn", "-ac", "1", "-ar", "48000",
        "-c:a", "pcm_s16le", "-fflags", "+bitexact", "-flags:a", "+bitexact", str(destination),
    ], check=True)


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as file:
        for block in iter(lambda: file.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def fsb5_subsongs(bank):
    """Map each FSB5 sample name in an FMOD bank to vgmstream's 1-based subsong number."""
    with bank.open("rb") as file, mmap.mmap(file.fileno(), 0, access=mmap.ACCESS_READ) as data:
        base = data.find(b"FSB5")
        version, count, headers_size = struct.unpack_from("<4x3I", data, base)
        # vgmstream fsb5.c: the base header is 0x40 bytes in version 0 and 0x3C in version 1.
        names = base + (0x40 if version == 0 else 0x3C) + headers_size
        subsongs = {}
        for index in range(count):
            start = names + struct.unpack_from("<I", data, names + 4 * index)[0]
            subsongs[data[start:data.find(b"\0", start)].decode()] = index + 1
        return subsongs


def source_reader(args, scratch):
    subsongs = functools.cache(fsb5_subsongs)
    digest = functools.cache(sha256)

    def decode(bank, sample, destination):
        number = str(subsongs(bank)[sample])
        info = subprocess.run([args.vgmstream, "-m", "-s", number, str(bank)],
                              check=True, capture_output=True, text=True).stdout
        if f"stream name: {sample}" not in info.splitlines():
            raise RuntimeError(f"{bank.name} subsong {number} is not {sample}")
        subprocess.run([args.vgmstream, "-i", "-s", number, "-o", str(destination), str(bank)],
                       check=True, capture_output=True)

    @functools.cache
    def read(source):
        """Return a file FFmpeg can read, its provenance path and the hash of the shipped source file."""
        if source[0] == "d2r":
            path = args.d2r_repository / "sounds" / source[1]
            return path, "sounds/" + source[1], digest(path)
        _, bank_name, sample = source
        relative = STREAMING_ASSETS / (bank_name + ".bank")
        bank = args.game / relative
        decoded = scratch / (sample + ".wav")
        decode(bank, sample, decoded)
        return decoded, f"{relative}#{sample}", digest(bank)

    return read


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("d2r_repository", type=pathlib.Path)
    parser.add_argument("game", type=pathlib.Path, help="Last Epoch installation folder")
    parser.add_argument("--vgmstream", default="vgmstream-cli",
                        help="vgmstream-cli executable; FFmpeg cannot decode FMOD's FSB5 Vorbis")
    args = parser.parse_args()
    output = pathlib.Path(__file__).resolve().parents[1] / "LeAccess" / "Sounds"
    loops = output / "Loops"
    loops.mkdir(parents=True, exist_ok=True)
    provenance = []
    with tempfile.TemporaryDirectory() as scratch:
        read = source_reader(args, pathlib.Path(scratch))
        assets = [(name + ".wav", source, duration) for name, source, duration in CLIPS]
        assets += [("Loops/" + name + ".wav", source, None) for name, source in LOOPS]
        for asset, source, duration in assets:
            path, origin, source_sha256 = read(source)
            destination = output / asset
            convert(path, destination, duration)
            provenance.append({"asset": asset, "source": origin,
                               "source_sha256": source_sha256, "asset_sha256": sha256(destination),
                               "start_seconds": 0, "duration_seconds": duration or "whole"})
    shutil.copyfile(args.d2r_repository / "LICENSE", output / "D2R-LICENSE.txt")
    (output / "provenance.json").write_text(json.dumps(provenance, indent=2) + "\n", encoding="utf-8")
    print(f"Imported {len(provenance)} cues and loops into {output}")


if __name__ == "__main__":
    main()
