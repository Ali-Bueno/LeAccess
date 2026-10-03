"""Recreate the bounded embedded cues from the user's D2R recordings using FFmpeg."""
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
        subprocess.run([
            "ffmpeg", "-nostdin", "-v", "error", "-y", "-i", str(source),
            "-t", str(duration), "-map_metadata", "-1", "-vn", "-ac", "1", "-ar", "48000",
            "-c:a", "pcm_s16le", "-fflags", "+bitexact", "-flags:a", "+bitexact", str(destination),
        ], check=True)
        provenance.append({"asset": destination.name, "source": "sounds/" + relative,
                           "source_sha256": sha256(source), "asset_sha256": sha256(destination),
                           "start_seconds": 0, "duration_seconds": duration})
    shutil.copyfile(args.d2r_repository / "LICENSE", output / "D2R-LICENSE.txt")
    (output / "provenance.json").write_text(json.dumps(provenance, indent=2) + "\n", encoding="utf-8")
    print(f"Imported {len(provenance)} bounded cues into {output}")


if __name__ == "__main__":
    main()
