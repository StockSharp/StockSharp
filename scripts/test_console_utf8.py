from __future__ import annotations

import importlib.util
import os
import subprocess
import sys
import types
import unittest
from pathlib import Path

from console_utf8 import force_utf8_stdio

# A failing assertion here quotes the very text that an ANSI console cannot
# encode, so the test runner needs the same treatment as the scripts.
force_utf8_stdio()

SCRIPTS = Path(__file__).parent
ROOT = SCRIPTS.parent

# The Windows runner pipes script output through the ANSI code page, which on an
# English image is cp1252 and has no glyph for the localized connector names.
ANSI_CONSOLE = {"PYTHONIOENCODING": "cp1252", "PYTHONUTF8": "0"}

# "Russian market" and "Crypto exchanges": the alphabets the READMEs are written in.
SAMPLE_TEXT = (
    "Российский рынок"
    " / 加密货币交易所"
)


def child_source(*statements: str) -> str:
    """Build a program that configures the console and then prints."""
    return "\n".join(
        [
            "import sys",
            "sys.path.insert(0, sys.argv[1])",
            "from console_utf8 import force_utf8_stdio",
            "force_utf8_stdio()",
            *statements,
        ]
    )


def run_on_ansi_console(args: list[str]) -> subprocess.CompletedProcess[bytes]:
    return subprocess.run(
        [sys.executable, *args],
        cwd=ROOT,
        env=os.environ | ANSI_CONSOLE,
        check=False,
        capture_output=True,
    )


def load_script(name: str) -> types.ModuleType:
    spec = importlib.util.spec_from_file_location(name.replace("-", "_"), SCRIPTS / f"{name}.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class ConsoleUtf8Tests(unittest.TestCase):
    def test_stdout_and_stderr_survive_an_ansi_console(self) -> None:
        child = child_source("print(sys.argv[2])", "print(sys.argv[2], file=sys.stderr)")
        result = run_on_ansi_console(["-c", child, str(SCRIPTS), SAMPLE_TEXT])

        self.assertEqual(0, result.returncode, result.stderr.decode("utf-8", "replace"))
        self.assertEqual(SAMPLE_TEXT, result.stdout.decode("utf-8").strip())
        self.assertEqual(SAMPLE_TEXT, result.stderr.decode("utf-8").strip())

    def test_stderr_survives_text_that_utf8_cannot_encode(self) -> None:
        child = child_source(r"print('\udcff', file=sys.stderr)")
        result = run_on_ansi_console(["-c", child, str(SCRIPTS)])

        self.assertEqual(0, result.returncode, result.stderr.decode("utf-8", "replace"))
        self.assertIn(rb"\udcff", result.stderr)

    def test_check_scripts_configure_their_console(self) -> None:
        scripts = sorted(SCRIPTS.glob("check-*.py"))
        self.assertTrue(scripts)

        for script in scripts:
            with self.subTest(script=script.name):
                source = script.read_text(encoding="utf-8")
                configured = (
                    "from console_utf8 import force_utf8_stdio" in source
                    and "force_utf8_stdio()" in source
                )
                self.assertTrue(configured, f"{script.name} must call force_utf8_stdio() before printing")

    def test_readme_connectors_reports_localized_section_on_ansi_console(self) -> None:
        result = run_on_ansi_console([str(SCRIPTS / "check-readme-connectors.py")])
        title = load_script("check-readme-connectors").KNOWN_LOCAL_SECTIONS["ru"][0]

        self.assertEqual("", result.stderr.decode("utf-8", "replace"))
        self.assertIn(f"INFO local-only ru/{title}".encode(), result.stdout)


if __name__ == "__main__":
    unittest.main()
