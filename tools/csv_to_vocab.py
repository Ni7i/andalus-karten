#!/usr/bin/env python3
"""
Wandelt eine einfache CSV-/TSV-Tabelle in die vocabulary.json der App um.

Spalten (Kopfzeile erforderlich, Reihenfolge egal):
    kind      "lesson" oder "essential"
    number    Lektionsnummer (1, 2, …)
    title     Titel der Lektion (wird von der ersten Zeile der Lektion übernommen)
    ar        Arabisch
    de        Deutsch
    tr        Umschrift (optional)
    note      Notiz, z. B. Plural (optional)

Beispiel:
    python3 tools/csv_to_vocab.py woerter.csv src/AndalusKarten.App/Resources/Raw/vocabulary.json
"""
import csv
import json
import sys
from collections import OrderedDict


def main(src: str, dst: str) -> None:
    with open(src, encoding="utf-8-sig", newline="") as f:
        sample = f.read(4096)
        f.seek(0)
        dialect = csv.Sniffer().sniff(sample, delimiters=",;\t")
        rows = list(csv.DictReader(f, dialect=dialect))

    lessons: "OrderedDict[tuple[str, int], dict]" = OrderedDict()
    for row in rows:
        ar, de = (row.get("ar") or "").strip(), (row.get("de") or "").strip()
        if not ar or not de:
            continue
        kind = (row.get("kind") or "lesson").strip().lower()
        essential = kind.startswith("e")
        number = int(row["number"])
        key = ("E" if essential else "L", number)
        lesson = lessons.setdefault(key, {
            "id": f"{key[0]}{number:02d}",
            "number": number,
            "title": (row.get("title") or "").strip() or f"Lektion {number}",
            "essential": essential,
            "words": [],
        })
        word = {"id": f"{lesson['id']}-{len(lesson['words']) + 1:03d}", "ar": ar, "de": de}
        for opt in ("tr", "note"):
            if (row.get(opt) or "").strip():
                word[opt] = row[opt].strip()
        lesson["words"].append(word)

    ordered = sorted(lessons.values(), key=lambda l: (l["essential"], l["number"]))
    with open(dst, "w", encoding="utf-8") as f:
        json.dump({"lessons": ordered}, f, ensure_ascii=False, indent=1)
        f.write("\n")

    total = sum(len(l["words"]) for l in ordered)
    print(f"{len(ordered)} Lektionen, {total} Wörter → {dst}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    main(sys.argv[1], sys.argv[2])
