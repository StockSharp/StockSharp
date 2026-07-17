#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]

README_FILES = {
    "en": ROOT / "README.md",
    "ru": ROOT / "README.ru.md",
    "zh": ROOT / "README.zh.md",
}

COMMON_SECTIONS = {
    "crypto": {
        "en": "Crypto exchanges",
        "ru": "\u041a\u0440\u0438\u043f\u0442\u043e\u0431\u0438\u0440\u0436\u0438",
        "zh": "\u52a0\u5bc6\u8d27\u5e01\u4ea4\u6613\u6240",
    },
    "dex": {
        "en": "DEX exchanges",
        "ru": "DEX exchanges",
        "zh": "DEX exchanges",
    },
    "stock": {
        "en": "Stock, Futures and Options",
        "ru": "\u0410\u043a\u0446\u0438\u0438, \u0444\u044c\u044e\u0447\u0435\u0440\u0441\u044b \u0438 \u043e\u043f\u0446\u0438\u043e\u043d\u044b",
        "zh": "\u80a1\u7968\u3001\u671f\u8d27\u548c\u671f\u6743",
    },
    "forex": {
        "en": "Forex",
        "ru": "\u0424\u043e\u0440\u0435\u043a\u0441",
        "zh": "\u5916\u6c47",
    },
}

KNOWN_LOCAL_SECTIONS = {
    "ru": ["\u0420\u043e\u0441\u0441\u0438\u0439\u0441\u043a\u0438\u0439 \u0440\u044b\u043d\u043e\u043a"],
}

AVAILABLE_TRAILERS = [
    " and many others.",
    " and many others",
    " \u0438 \u043c\u043d\u043e\u0433\u0438\u0435 \u0434\u0440\u0443\u0433\u0438\u0435.",
    " \u0438 \u043c\u043d\u043e\u0433\u0438\u0435 \u0434\u0440\u0443\u0433\u0438\u0435",
    " \u7b49\u7b49\u3002",
    " \u7b49\u7b49",
]

LOGO_RE = re.compile(r'logos/([^"/]+)')


def read(path: Path) -> list[str]:
    return path.read_text(encoding="utf-8-sig").splitlines()


def parse_available_connections(lines: list[str]) -> list[str] | None:
    for line in lines:
        line = line.strip()
        if not line.startswith("**") or "Binance, MT4, MT5" not in line or ":" not in line:
            continue

        value = line.split(":", 1)[1].strip()
        for trailer in AVAILABLE_TRAILERS:
            if value.endswith(trailer):
                value = value[: -len(trailer)].strip()
                break

        return [item.strip() for item in value.rstrip(".\u3002").split(",") if item.strip()]

    return None


def section_bounds(lines: list[str], title: str) -> tuple[int, int]:
    heading = f"## {title}"
    start = None

    for index, line in enumerate(lines):
        if line.strip().startswith(heading):
            start = index + 1
            break

    if start is None:
        raise ValueError(f"section not found: {title}")

    end = len(lines)
    for index in range(start, len(lines)):
        if lines[index].startswith("## "):
            end = index
            break

    return start, end


def parse_connector_rows(lines: list[str], title: str) -> list[tuple[str, str | None]]:
    start, end = section_bounds(lines, title)
    rows: list[tuple[str, str | None]] = []

    for line in lines[start:end]:
        if not line.startswith('|<img src="./Media/logos/'):
            continue

        cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
        if len(cells) < 2:
            continue

        logo = LOGO_RE.search(cells[0])
        rows.append((cells[1], logo.group(1) if logo else None))

    return rows


def parse_all_connector_names(lines_by_lang: dict[str, list[str]]) -> dict[str, list[str]]:
    names: dict[str, list[str]] = {lang: [] for lang in lines_by_lang}

    for lang, lines in lines_by_lang.items():
        names[lang].extend(parse_available_connections(lines) or [])

    for titles in COMMON_SECTIONS.values():
        for lang, title in titles.items():
            names[lang].extend(name for name, _ in parse_connector_rows(lines_by_lang[lang], title))

    for lang, titles in KNOWN_LOCAL_SECTIONS.items():
        for title in titles:
            names[lang].extend(name for name, _ in parse_connector_rows(lines_by_lang[lang], title))

    return names


def compare_ordered(label: str, values: dict[str, list[str]], *, allow_duplicates: bool = False) -> bool:
    base_lang = "en"
    base = values[base_lang]
    base_set = set(base)
    ok = True

    for lang, current in values.items():
        current_set = set(current)
        missing = [item for item in base if item not in current_set]
        extra = [item for item in current if item not in base_set]
        duplicates = [] if allow_duplicates else sorted({item for item in current if current.count(item) > 1})
        order_mismatch = current != base

        if missing or extra or duplicates or order_mismatch:
            ok = False
            print(f"DIFF {label} {lang}: count={len(current)}")
            if missing:
                print(f"  missing: {', '.join(missing)}")
            if extra:
                print(f"  extra: {', '.join(extra)}")
            if duplicates:
                print(f"  duplicates: {', '.join(duplicates)}")
            if order_mismatch and not missing and not extra:
                for index, (expected, actual) in enumerate(zip(base, current), 1):
                    if expected != actual:
                        print(f"  first mismatch #{index}: expected {expected!r}, actual {actual!r}")
                        break
        else:
            print(f"OK {label} {lang}: count={len(current)}")

    return ok


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--metadata-since",
        help="Also check that connector metadata commits since the specified git date are present in every root README.",
    )
    args = parser.parse_args()

    lines_by_lang = {lang: read(path) for lang, path in README_FILES.items()}
    ok = True

    available = {
        lang: parse_available_connections(lines)
        for lang, lines in lines_by_lang.items()
    }

    missing_available = [lang for lang, items in available.items() if items is None]
    if missing_available:
        ok = False
        print(f"Missing Available connections block: {', '.join(missing_available)}")
    else:
        ok &= compare_ordered(
            "available-connections",
            {lang: items or [] for lang, items in available.items()},
        )

    for section_id, titles in COMMON_SECTIONS.items():
        section_names: dict[str, list[str]] = {}
        section_logos: dict[str, list[str]] = {}

        for lang, title in titles.items():
            rows = parse_connector_rows(lines_by_lang[lang], title)
            section_names[lang] = [name for name, _ in rows]
            section_logos[lang] = [logo or "" for _, logo in rows]

        ok &= compare_ordered(f"{section_id}-names", section_names)
        ok &= compare_ordered(f"{section_id}-logos", section_logos, allow_duplicates=True)

    for lang, titles in KNOWN_LOCAL_SECTIONS.items():
        for title in titles:
            rows = parse_connector_rows(lines_by_lang[lang], title)
            print(f"INFO local-only {lang}/{title}: count={len(rows)}")

    if args.metadata_since:
        ok &= compare_recent_metadata(args.metadata_since, lines_by_lang)

    return 0 if ok else 1


def compare_recent_metadata(since: str, lines_by_lang: dict[str, list[str]]) -> bool:
    output = subprocess.check_output(
        ["git", "log", f"--since={since}", "--format=%H%x09%s"],
        cwd=ROOT,
        encoding="utf-8",
        errors="replace",
    )

    commits: list[tuple[str, str]] = []
    for line in output.splitlines():
        if "\t" not in line:
            continue

        commit, subject = line.split("\t", 1)
        match = re.match(r"Add (.+) connector metadata$", subject)
        if match:
            commits.append((commit[:9], match.group(1)))

    ok = True
    names_by_lang = parse_all_connector_names(lines_by_lang)
    print(f"INFO metadata commits since {since}: count={len(commits)}")

    for commit, name in commits:
        needle = name.casefold()
        missing_langs = [
            lang
            for lang, names in names_by_lang.items()
            if not any(needle in connector.casefold() for connector in names)
        ]

        if missing_langs:
            ok = False
            print(f"DIFF metadata {commit} {name}: missing in {', '.join(missing_langs)}")
        else:
            print(f"OK metadata {commit} {name}")

    return ok


if __name__ == "__main__":
    sys.exit(main())
