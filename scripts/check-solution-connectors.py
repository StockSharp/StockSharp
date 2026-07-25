#!/usr/bin/env python3
from __future__ import annotations

import argparse
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent


def normalize_solution_path(path: str) -> str:
    return path.replace("\\", "/")


def discover_connector_projects(connectors_root: Path) -> list[str]:
    if not connectors_root.is_dir():
        raise ValueError(f"connector repository not found: {connectors_root}")

    projects: list[str] = []
    solution_root = f"../{connectors_root.name}"

    for directory in connectors_root.iterdir():
        if not directory.is_dir() or directory.name == "Tests":
            continue

        for project in directory.glob("*.csproj"):
            projects.append(f"{solution_root}/{directory.name}/{project.name}")

    return sorted(projects, key=str.casefold)


def read_solution_connector_projects(solution: Path, connectors_root: Path) -> list[str]:
    if not solution.is_file():
        raise ValueError(f"solution not found: {solution}")

    try:
        root = ET.parse(solution).getroot()
    except ET.ParseError as error:
        raise ValueError(f"invalid solution XML: {solution}: {error}") from error

    prefix = f"../{connectors_root.name}/"
    projects: list[str] = []

    for element in root.iter("Project"):
        path = normalize_solution_path(element.get("Path", ""))
        if path.startswith(prefix):
            projects.append(path)

    return projects


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Check that StockSharp.slnx contains every connector project from the sibling repository.",
    )
    parser.add_argument(
        "--solution",
        type=Path,
        default=ROOT / "StockSharp.slnx",
        help="Solution to validate (default: StockSharp.slnx in the repository root).",
    )
    parser.add_argument(
        "--connectors-root",
        type=Path,
        default=ROOT.parent / "Connectors",
        help="Connector repository root (default: ../Connectors).",
    )
    args = parser.parse_args()

    solution = args.solution.expanduser().resolve()
    connectors_root = args.connectors_root.expanduser().resolve()

    try:
        expected = discover_connector_projects(connectors_root)
        actual = read_solution_connector_projects(solution, connectors_root)
    except ValueError as error:
        print(f"ERROR {error}")
        return 2

    expected_set = set(expected)
    actual_set = set(actual)
    counts = Counter(actual)

    missing = sorted(expected_set - actual_set, key=str.casefold)
    stale = sorted(actual_set - expected_set, key=str.casefold)
    duplicates = sorted(
        (project for project, count in counts.items() if count > 1),
        key=str.casefold,
    )

    for project in missing:
        print(f"MISSING {project}")

    for project in stale:
        print(f"STALE {project}")

    for project in duplicates:
        print(f"DUPLICATE {project}")

    if missing or stale or duplicates:
        print(
            "DIFF solution connectors: "
            f"expected={len(expected)}, actual={len(actual)}, "
            f"missing={len(missing)}, stale={len(stale)}, duplicates={len(duplicates)}"
        )
        return 1

    print(f"OK solution connectors: projects={len(expected)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
