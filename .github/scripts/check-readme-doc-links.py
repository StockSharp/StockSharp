#!/usr/bin/env python3
from __future__ import annotations

import argparse
import os
import re
import sys
from pathlib import Path, PurePosixPath
from urllib.parse import unquote, urlsplit


ROOT = Path(__file__).resolve().parents[2]

README_FILES = {
    "en": ROOT / "README.md",
    "ru": ROOT / "README.ru.md",
    "zh": ROOT / "README.zh.md",
}

DOC_LINK_RE = re.compile(
    r'https://doc\.stocksharp\.com/[^\s<>"\')\]]+',
    re.IGNORECASE,
)


def default_doc_root() -> Path:
    configured = os.environ.get("STOCKSHARP_DOC_ROOT")
    if configured:
        return Path(configured)

    return ROOT.parent / "doc"


def read(path: Path) -> list[str]:
    return path.read_text(encoding="utf-8-sig").splitlines()


def resolve_doc_path(url: str, doc_root: Path) -> tuple[str | None, Path | None]:
    parsed = urlsplit(url)
    relative = unquote(parsed.path).strip("/")
    if not relative:
        return None, None

    route = PurePosixPath(relative)
    language = route.parts[0] if route.parts else None

    if route.suffix.lower() == ".html":
        route = route.with_suffix(".md")
        return language, doc_root.joinpath(*route.parts)

    if not route.suffix:
        return language, doc_root.joinpath(*route.parts)

    return language, None


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate doc.stocksharp.com links in the localized root READMEs against the doc repository.",
    )
    parser.add_argument(
        "--doc-root",
        type=Path,
        default=default_doc_root(),
        help="Path to the StockSharp/doc checkout (default: ../doc or STOCKSHARP_DOC_ROOT).",
    )
    args = parser.parse_args()

    doc_root = args.doc_root.expanduser().resolve()
    if not doc_root.is_dir():
        print(f"ERROR doc repository not found: {doc_root}")
        return 2

    ok = True
    total = 0

    for expected_language, readme_path in README_FILES.items():
        file_ok = True
        checked = 0

        for line_number, line in enumerate(read(readme_path), 1):
            for match in DOC_LINK_RE.finditer(line):
                url = match.group(0)
                language, target = resolve_doc_path(url, doc_root)
                if target is None:
                    continue

                checked += 1
                total += 1

                if language != expected_language:
                    ok = False
                    file_ok = False
                    print(
                        f"DIFF {readme_path.name}:{line_number}: "
                        f"expected /{expected_language}/ documentation URL, got {url}"
                    )

                if not target.exists():
                    ok = False
                    file_ok = False
                    try:
                        expected = target.relative_to(doc_root)
                    except ValueError:
                        expected = target
                    print(
                        f"MISSING {readme_path.name}:{line_number}: {url}\n"
                        f"  expected: {expected}"
                    )

        print(f"{'OK' if file_ok else 'CHECK'} {readme_path.name}: checked={checked}")

    if ok:
        print(f"OK README documentation links: checked={total}, doc-root={doc_root}")

    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
