from __future__ import annotations

import importlib.util
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from console_utf8 import force_utf8_stdio

# Connector names and README rows are localized, and a failing assertion quotes them.
force_utf8_stdio()

SCRIPT = Path(__file__).with_name("check-connector-coverage.py")

README_FILES = {"en": "README.md", "ru": "README.ru.md", "zh": "README.zh.md"}

ADAPTER_SOURCE = """namespace StockSharp.{name};

/// <summary>Adapter.</summary>
[MediaIcon(Media.MediaNames.{icon})]
[Doc("{doc}")]
[Display(
\tResourceType = typeof(LocalizedStrings),
\tName = LocalizedStrings.{name}Key)]
public partial class {name}MessageAdapter : MessageAdapter
{{
}}
"""


def load_script() -> object:
    spec = importlib.util.spec_from_file_location("check_connector_coverage", SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def row(name: str, icon: str, doc: str, lang: str) -> str:
    link = f"https://doc.stocksharp.com/{lang}/{doc}"
    return f'|<img src="./Media/logos/{icon}_logo.svg" height="30" /> |{name} | <a href="{link}" target="_blank">Docs</a> |'


class CheckConnectorCoverageTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        workspace = Path(self.temp_dir.name)
        self.readme_root = workspace / "StockSharp (GitHub)"
        self.connectors_root = workspace / "Connectors"
        self.readme_root.mkdir()
        self.connectors_root.mkdir()
        self.rows = {lang: [] for lang in README_FILES}
        self.add_external_rows()

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def create_connector(self, name: str, doc: str, *, in_build_output: bool = False) -> None:
        directory = self.connectors_root / name
        if in_build_output:
            directory /= "obj"

        directory.mkdir(parents=True)
        source = ADAPTER_SOURCE.format(name=name, icon=name.lower(), doc=doc)
        (directory / f"{name}MessageAdapter_Settings.cs").write_text(source, encoding="utf-8")

    def add_row(self, name: str, doc: str, *, languages: list[str] | None = None) -> None:
        for lang in languages or list(README_FILES):
            self.rows[lang].append(row(name, name.lower(), doc, lang))

    def add_external_rows(self) -> None:
        """The connectors of the applications repository are required of every checkout."""
        script = load_script()

        for index, doc in enumerate(sorted(script.EXTERNAL_CONNECTORS)):
            russian = script.documentation_group(doc) == script.RUSSIAN_GROUP
            self.add_row(f"External{index}", doc, languages=["ru"] if russian else None)

    def drop_row(self, doc: str) -> None:
        for lang, rows in self.rows.items():
            self.rows[lang] = [line for line in rows if doc not in line]

    def add_unlinked_row(self, name: str) -> None:
        for lang in README_FILES:
            self.rows[lang].append(f'|<img src="./Media/logos/{name.lower()}_logo.svg" height="30" /> |{name} | — |')

    def write_readmes(self) -> None:
        for lang, file_name in README_FILES.items():
            body = "\n".join(["## Connectors", "", *self.rows[lang], ""])
            (self.readme_root / file_name).write_text(body, encoding="utf-8")

    def run_script(self) -> subprocess.CompletedProcess[str]:
        self.write_readmes()
        return subprocess.run(
            [
                sys.executable,
                str(SCRIPT),
                "--readme-root",
                str(self.readme_root),
                "--connectors-root",
                str(self.connectors_root),
            ],
            check=False,
            capture_output=True,
            text=True,
            encoding="utf-8",
        )

    def test_accepts_a_connector_documented_in_every_language(self) -> None:
        self.create_connector("Alpha", "topics/api/connectors/stock_market/alpha.html")
        self.add_row("Alpha", "topics/api/connectors/stock_market/alpha.html")

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("OK connector coverage:", result.stdout)

    def test_reports_a_connector_absent_from_one_language(self) -> None:
        self.create_connector("Alpha", "topics/api/connectors/stock_market/alpha.html")
        self.add_row("Alpha", "topics/api/connectors/stock_market/alpha.html", languages=["en", "ru"])

        result = self.run_script()

        self.assertEqual(1, result.returncode)
        self.assertIn("MISSING zh topics/api/connectors/stock_market/alpha.html", result.stdout)
        self.assertNotIn("MISSING en", result.stdout)

    def test_russian_market_connectors_belong_to_the_russian_readme_only(self) -> None:
        self.create_connector("Beta", "topics/api/connectors/russia/beta.html")
        self.add_row("Beta", "topics/api/connectors/russia/beta.html", languages=["ru"])

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_reports_a_row_whose_connector_no_longer_exists(self) -> None:
        self.create_connector("Alpha", "topics/api/connectors/stock_market/alpha.html")
        self.add_row("Alpha", "topics/api/connectors/stock_market/alpha.html")
        self.add_row("Removed", "topics/api/connectors/stock_market/removed.html")

        result = self.run_script()

        self.assertEqual(1, result.returncode)
        self.assertIn("STALE en Removed: topics/api/connectors/stock_market/removed.html", result.stdout)

    def test_ignores_rows_without_a_documentation_link(self) -> None:
        self.create_connector("Alpha", "topics/api/connectors/stock_market/alpha.html")
        self.add_row("Alpha", "topics/api/connectors/stock_market/alpha.html")
        self.add_unlinked_row("Historical")

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_ignores_sources_under_build_output(self) -> None:
        self.create_connector("Alpha", "topics/api/connectors/stock_market/alpha.html")
        self.add_row("Alpha", "topics/api/connectors/stock_market/alpha.html")
        self.create_connector("Stale", "topics/api/connectors/stock_market/stale.html", in_build_output=True)

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_requires_a_row_for_a_connector_from_the_applications_repository(self) -> None:
        external = sorted(load_script().EXTERNAL_CONNECTORS)[0]
        self.create_connector("Alpha", "topics/api/connectors/stock_market/alpha.html")
        self.add_row("Alpha", "topics/api/connectors/stock_market/alpha.html")
        self.drop_row(external)

        result = self.run_script()

        self.assertEqual(1, result.returncode)
        self.assertIn(f"MISSING en {external} (applications repository)", result.stdout)

    def test_fails_when_the_connector_repository_is_absent(self) -> None:
        self.create_connector("Alpha", "topics/api/connectors/stock_market/alpha.html")
        self.add_row("Alpha", "topics/api/connectors/stock_market/alpha.html")
        self.write_readmes()

        result = subprocess.run(
            [
                sys.executable,
                str(SCRIPT),
                "--readme-root",
                str(self.readme_root),
                "--connectors-root",
                str(self.connectors_root / "absent"),
            ],
            check=False,
            capture_output=True,
            text=True,
            encoding="utf-8",
        )

        self.assertEqual(2, result.returncode)
        self.assertIn("ERROR connector repository not found", result.stdout)


if __name__ == "__main__":
    unittest.main()
