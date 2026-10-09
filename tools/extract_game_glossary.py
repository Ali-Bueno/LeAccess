"""Write the game's own short UI strings in every shipped language as one TSV, for translating mod speech.

Usage: python tools/extract_game_glossary.py <Last Epoch_Data/StreamingAssets/aa/StandaloneWindows64> <output.tsv>
Rows are game string-table entries (key, then one column per game locale code); see reference/localization.md.
"""
from pathlib import Path
import re
import sys

import UnityPy

BUNDLE = re.compile(r"localization-string-tables-(?P<name>.+)_assets_all\.bundle$")
# Long entries are dialogue and descriptions; terminology lives in short labels.
MAX_LENGTH = 90
SKIPPED_TABLES = ("Dialogue", "Cinematics", "Lore")


def clean(text):
    return re.sub(r"\s+", " ", re.sub(r"^\[[a-z]{1,3}\]", "", text or "")).strip()


def main(source, output):
    locales = {}
    for path in sorted(source.glob("localization-string-tables-*_assets_all.bundle")):
        name = BUNDLE.match(path.name).group("name")
        if "(dev)" in name or "(key)" in name:
            continue
        locales[re.findall(r"\(([^()]+)\)", name)[-1]] = path
    codes = sorted(locales, key=lambda code: (code != "en", code))

    keys = {}
    for obj in UnityPy.load(str(source / "localization-assets-shared_assets_all.bundle")).objects:
        if obj.type.name == "MonoBehaviour":
            tree = obj.read_typetree()
            for entry in tree.get("m_Entries", []):
                keys[entry["m_Id"]] = tree["m_TableCollectionName"] + "/" + entry["m_Key"]

    texts = {}
    for code in codes:
        for obj in UnityPy.load(str(locales[code])).objects:
            if obj.type.name == "MonoBehaviour":
                tree = obj.read_typetree()
                table = re.sub(r"_[^_]+$", "", tree.get("m_Name", ""))
                for entry in tree.get("m_TableData", []):
                    texts.setdefault((table, entry["m_Id"]), {})[code] = clean(entry["m_Localized"])

    rows = 0
    with output.open("w", encoding="utf-8") as file:
        file.write("key\t" + "\t".join(codes) + "\n")
        for (table, entry_id), localized in sorted(texts.items(), key=lambda item: (item[0][0], str(item[0][1]))):
            english = localized.get("en", "")
            if not english or len(english) > MAX_LENGTH or table.startswith(SKIPPED_TABLES):
                continue
            key = keys.get(entry_id, f"{table}/{entry_id}")
            file.write(key + "\t" + "\t".join(localized.get(code, "") for code in codes) + "\n")
            rows += 1
    print(f"{rows} rows, locales: {', '.join(codes)}")


if __name__ == "__main__":
    main(Path(sys.argv[1]), Path(sys.argv[2]))
