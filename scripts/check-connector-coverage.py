#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

from console_utf8 import force_utf8_stdio


ROOT = Path(__file__).resolve().parent.parent

README_FILES = {
    "en": "README.md",
    "ru": "README.ru.md",
    "zh": "README.zh.md",
}

# A connector points at its documentation with [Doc("topics/api/connectors/<group>/<page>.html")]
# and the README row links the very same path behind a language prefix, so the pair identifies
# the connector without depending on its display name or logo.
DOC_ATTRIBUTE_RE = re.compile(r'\[\s*Doc\s*\(\s*"([^"]+)"\s*\)\s*\]')
ROW_PREFIX = '|<img src="./Media/logos/'
ROW_LINK_RE = re.compile(r'https://doc\.stocksharp\.com/[a-z]+/(topics/[^"\s]+)')

# Connectors of the Russian market are listed in the Russian README alone.
RUSSIAN_GROUP = "russia"
RUSSIAN_LANGUAGE = "ru"

# Connectors implemented in the applications repository, which is not checked out
# here. Their rows are as required as any other, so the pages they document are
# named instead of discovered.
EXTERNAL_SOURCE = "applications repository"
EXTERNAL_CONNECTORS = {
    "topics/api/connectors/common/fast_protocol.html",
    "topics/api/connectors/forex/dukascopy.html",
    "topics/api/connectors/russia/finam.html",
    "topics/api/connectors/russia/mfd.html",
    "topics/api/connectors/russia/micex.html",
    "topics/api/connectors/russia/plaza.html",
    "topics/api/connectors/russia/simba.html",
    "topics/api/connectors/russia/spb_exchange.html",
    "topics/api/connectors/russia/twime.html",
    "topics/api/connectors/stock_market/yahoo.html",
}

BUILD_OUTPUT = {"bin", "obj"}


def read(path: Path) -> list[str]:
    return path.read_text(encoding="utf-8-sig").splitlines()


def documentation_group(doc_path: str) -> str:
    parts = doc_path.split("/")
    return parts[3] if len(parts) > 3 else ""


def discover_documented_connectors(connectors_root: Path) -> dict[str, str]:
    """Map every documented connector to the source file declaring it."""
    if not connectors_root.is_dir():
        raise ValueError(f"connector repository not found: {connectors_root}")

    connectors: dict[str, str] = {}

    for source in sorted(connectors_root.rglob("*.cs")):
        if BUILD_OUTPUT & set(source.parts):
            continue

        text = source.read_text(encoding="utf-8-sig", errors="replace")
        for doc_path in DOC_ATTRIBUTE_RE.findall(text):
            connectors.setdefault(doc_path, source.relative_to(connectors_root).as_posix())

    return connectors


def read_readme_rows(path: Path) -> dict[str, str]:
    """Map the documentation page of every connector row to the name the row shows."""
    rows: dict[str, str] = {}

    for line in read(path):
        if not line.startswith(ROW_PREFIX):
            continue

        link = ROW_LINK_RE.search(line)
        if link is None:
            continue

        cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
        rows[link.group(1)] = cells[1] if len(cells) > 1 else ""

    return rows


def expected_connectors(connectors: dict[str, str], language: str) -> set[str]:
    if language == RUSSIAN_LANGUAGE:
        return set(connectors)

    return {doc for doc in connectors if documentation_group(doc) != RUSSIAN_GROUP}


def main() -> int:
    force_utf8_stdio()

    parser = argparse.ArgumentParser(
        description="Check that every documented connector has a row in the root READMEs.",
    )
    parser.add_argument(
        "--readme-root",
        type=Path,
        default=ROOT,
        help="Repository holding the root READMEs (default: the repository this script lives in).",
    )
    parser.add_argument(
        "--connectors-root",
        type=Path,
        default=ROOT.parent / "Connectors",
        help="Connector repository root (default: ../Connectors).",
    )
    args = parser.parse_args()

    readme_root = args.readme_root.expanduser().resolve()
    connectors_root = args.connectors_root.expanduser().resolve()

    try:
        connectors = discover_documented_connectors(connectors_root)
    except ValueError as error:
        print(f"ERROR {error}")
        return 2

    if not connectors:
        print(f"ERROR no documented connector found: {connectors_root}")
        return 2

    for doc_path in EXTERNAL_CONNECTORS:
        connectors.setdefault(doc_path, EXTERNAL_SOURCE)

    missing = 0
    stale = 0

    for language, file_name in README_FILES.items():
        readme = readme_root / file_name

        if not readme.is_file():
            print(f"ERROR README not found: {readme}")
            return 2

        rows = read_readme_rows(readme)
        expected = expected_connectors(connectors, language)

        for doc_path in sorted(expected - set(rows)):
            missing += 1
            print(f"MISSING {language} {doc_path} ({connectors[doc_path]})")

        for doc_path in sorted(set(rows) - set(connectors)):
            stale += 1
            print(f"STALE {language} {rows[doc_path]}: {doc_path}")

    if missing or stale:
        print(
            "DIFF connector coverage: "
            f"connectors={len(connectors)}, missing={missing}, stale={stale}"
        )
        return 1

    print(f"OK connector coverage: connectors={len(connectors)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
