"""Recreate the embedded cues and full loops from the user's D2R recordings using FFmpeg."""
import argparse
import hashlib
import json
import pathlib
import shutil
import subprocess


# Cue-length choices preserve short natural attacks without occupying the shared
# navigation output for the original recordings' 10–15 second loops.
CLIPS = (
    ("gold", "item loop sounds/loot/gold.ogg", 0.30),
    ("equipment", "item loop sounds/loot/normal equipment.ogg", 0.30),
    ("chest", "item loop sounds/loot/chest.ogg", 0.24),
    ("stash", "item loop sounds/loot/stash.ogg", 0.24),
    ("portal", "item loop sounds/exploration/portal.ogg", 0.30),
    ("zone_transition", "item loop sounds/exploration/zone transition.ogg", 0.30),
    ("discovery", "one shot sounds/exploration/discovered.ogg", 0.45),
    ("unexplored_room", "item loop sounds/exploration/unexplored room.ogg", 0.35),
    ("inside_unexplored_room", "item loop sounds/exploration/inside unexplored room.ogg", 0.35),
)

# Whole recordings for the continuous world loop mixer (D2RAccess item_audio.cpp loops each file).
LOOPS = (
    ("gold", "item loop sounds/loot/gold.ogg"),
    ("equipment", "item loop sounds/loot/normal equipment.ogg"),
    ("chest", "item loop sounds/loot/chest.ogg"),
    ("stash", "item loop sounds/loot/stash.ogg"),
    ("portal", "item loop sounds/exploration/portal.ogg"),
    ("zone_transition", "item loop sounds/exploration/zone transition.ogg"),
)


def convert(source, destination, duration=None):
    limit = ["-t", str(duration)] if duration else []
    subprocess.run([
        "ffmpeg", "-nostdin", "-v", "error", "-y", "-i", str(source), *limit,
        "-map_metadata", "-1", "-vn", "-ac", "1", "-ar", "48000",
        "-c:a", "pcm_s16le", "-fflags", "+bitexact", "-flags:a", "+bitexact", str(destination),
    ], check=True)


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("d2r_repository", type=pathlib.Path)
    args = parser.parse_args()
    output = pathlib.Path(__file__).resolve().parents[1] / "LeAccess" / "Sounds"
    output.mkdir(parents=True, exist_ok=True)
    provenance = []
    for name, relative, duration in CLIPS:
        source = args.d2r_repository / "sounds" / relative
        destination = output / (name + ".wav")
        convert(source, destination, duration)
        provenance.append({"asset": destination.name, "source": "sounds/" + relative,
                           "source_sha256": sha256(source), "asset_sha256": sha256(destination),
                           "start_seconds": 0, "duration_seconds": duration})
    loops = output / "Loops"
    loops.mkdir(exist_ok=True)
    for name, relative in LOOPS:
        source = args.d2r_repository / "sounds" / relative
        destination = loops / (name + ".wav")
        convert(source, destination)
        provenance.append({"asset": "Loops/" + destination.name, "source": "sounds/" + relative,
                           "source_sha256": sha256(source), "asset_sha256": sha256(destination),
                           "start_seconds": 0, "duration_seconds": "whole"})
    shutil.copyfile(args.d2r_repository / "LICENSE", output / "D2R-LICENSE.txt")
    (output / "provenance.json").write_text(json.dumps(provenance, indent=2) + "\n", encoding="utf-8")
    print(f"Imported {len(provenance)} cues and loops into {output}")


if __name__ == "__main__":
    main()
