from __future__ import annotations

import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).with_name("check-solution-connectors.py")


class CheckSolutionConnectorsTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.workspace = Path(self.temp_dir.name)
        self.repo_root = self.workspace / "StockSharp (GitHub)"
        self.connectors_root = self.workspace / "Connectors"
        self.solution = self.repo_root / "StockSharp.slnx"
        self.repo_root.mkdir()
        self.connectors_root.mkdir()

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def create_project(self, directory: str, project: str | None = None) -> str:
        project_name = project or directory
        project_path = self.connectors_root / directory / f"{project_name}.csproj"
        project_path.parent.mkdir()
        project_path.write_text("<Project />\n", encoding="utf-8")
        return f"../Connectors/{directory}/{project_name}.csproj"

    def write_solution(self, projects: list[str]) -> None:
        entries = "\n".join(f'    <Project Path="{path}" />' for path in projects)
        self.solution.write_text(
            f'<Solution>\n  <Folder Name="/Connectors/">\n{entries}\n  </Folder>\n</Solution>\n',
            encoding="utf-8",
        )

    def run_script(self) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
                sys.executable,
                str(SCRIPT),
                "--solution",
                str(self.solution),
                "--connectors-root",
                str(self.connectors_root),
            ],
            check=False,
            capture_output=True,
            text=True,
        )

    def test_accepts_complete_solution_and_ignores_tests_project(self) -> None:
        projects = [
            self.create_project("Alpha"),
            self.create_project("Beta", "BetaConnector"),
        ]
        self.create_project("Tests")
        self.write_solution(projects)

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("OK solution connectors: projects=2", result.stdout)

    def test_reports_missing_stale_and_duplicate_projects(self) -> None:
        alpha = self.create_project("Alpha")
        beta = self.create_project("Beta")
        stale = "../Connectors/Removed/Removed.csproj"
        self.write_solution([alpha, alpha, stale])

        result = self.run_script()

        self.assertEqual(1, result.returncode)
        self.assertIn(f"MISSING {beta}", result.stdout)
        self.assertIn(f"STALE {stale}", result.stdout)
        self.assertIn(f"DUPLICATE {alpha}", result.stdout)


if __name__ == "__main__":
    unittest.main()
