# AGENTS.md -- StockSharp (GitHub)

## What this is

The public core of **StockSharp (S#)** -- the free .NET trading platform (`github.com/StockSharp`).
This repo holds the API library and engine: messages/business entities, the `Algo` stack
(indicators, strategies, testing, export/import, compilation, analytics, GPU), the matching
engine, diagram core, reporting, localization and media generators, plus `Samples` and `Tests`.
Broker connectors live in a **separate sibling repo** (`StockSharp/Connectors`), not here.

Workspace-wide agent rules (English in code/commits, Russian in chat, TDD, never auto-push,
Ecng-first, nullable/`Lock`/enum style, etc.) live in the configs repo: `configs/instructions.md`.
This file only covers what is specific to this repository.

## Build, test, run

Targets **.NET 10** (`net10.0`, primary) and **.NET 6** (`net6.0`, legacy); both SDKs are
installed in CI. There are three solutions plus a separate one for samples:

| File | What it contains |
|---|---|
| `StockSharp.slnx` | Everything -- core projects, samples and **every `../Connectors/*` project** (WPF/Avalonia surface). Local use; CI does not build it. |
| `StockSharp_CI.slnx` | Core projects, samples and the few connectors the samples use. The Windows CI build. |
| `StockSharp_Tests.slnx` | Core projects + `Tests` only. Cross-platform; the CI build on Linux/macOS and the test run on every OS. |
| `Samples/Samples.slnx` | The example projects under `Samples/`. |

Commands verified from `.github/workflows/dotnet.yml`:

```bash
# Windows CI build -- core, samples and the connectors the samples need
dotnet build StockSharp_CI.slnx --configuration Release

# Cross-platform build + test (the CI path on ubuntu/windows/macos)
dotnet build StockSharp_Tests.slnx --configuration Release
dotnet test  StockSharp_Tests.slnx --no-build --configuration Release
```

Tests use **MSTest** (`MSTest.TestAdapter`/`TestFramework`), **Moq**, and `Ecng.UnitTesting`;
the `Tests` project targets `net10.0` only. Follow the workspace test-run rule: run with a
filter and a timeout, e.g. `dotnet test StockSharp_Tests.slnx --filter <Name>`.

## Layout

- `Messages/`, `BusinessEntities/` -- protocol messages and domain entities.
- `Algo/`, `Algo.Strategies/`, `Algo.Indicators/`, `Algo.Testing/`, `Algo.Export/`,
  `Algo.Import/`, `Algo.Compilation/`, `Algo.Analytics*/`, `Algo.Gpu/` -- the engine.
- `MatchingEngine/`, `Diagram.Core/`, `Reporting/`, `Configuration/`, `Charting.Interfaces/`,
  `Alerts.Interfaces/`.
- `Localization*/`, `Media*/` -- resources and their source generators.
- `Samples/` -- API usage examples (own solution). `Tests/` -- the test suite.
- `.github/workflows/dotnet.yml` -- the only CI workflow. `scripts/` -- the validators its
  `validate` job runs before anything is built (see "CI validators" below).

## Conventions (repo-specific)

- **No `Directory.Build.props`.** Shared MSBuild config lives in `props/common_*.props`
  files that each `.csproj` imports explicitly (`props/common_target_net.props`,
  `props/common_target_tests.props`, etc.). The sibling repositories import them from there too.
- **Centralized versions.** Dependency and framework versions are defined in
  `props/common_versions.props` (`NetTfm`, `EcngVer`, `StockSharpVer`, and per-package `*Ver`).
  Reference packages via those MSBuild variables -- do not hardcode version numbers in a csproj.
- **Assembly/package naming** comes from `props/common_meta.props`: `AssemblyName` = `StockSharp.<ProjectName>`.
- **Product version** (`5.0.0`) is set once in `props/common_meta.props`.

## Localization strings -- run the validators after every edit

`Localization/strings.json` is the English base; each `Localization.Langs/<xx>/strings.json`
(38 languages) carries the same keys. CI checks all 39 files in the `validate` job, before
anything is built, so a formatting slip in one of them fails the whole run.

**After any edit of a `strings.json` -- a new key, a changed text, a translation -- run from the
repo root, in Windows PowerShell (`powershell`, the shell CI uses):**

```powershell
./scripts/sort-localization-strings.ps1          # rewrites all 39 files into the canonical form
./scripts/sort-localization-strings.ps1 -Check   # CI step: must end with "Localization strings are sorted."
./scripts/validate-localization-strings.ps1      # CI step: every language has as many strings as the base
```

- **Do not format these files by hand.** The canonical form is whatever the sort script writes:
  keys in ordinal order, two-space indent, and `'`, `&`, `<`, `>` written as `\u0027`, `\u0026`,
  `\u003c`, `\u003e`. `-Check` prints `UNSORTED <file>` for *any* difference from that form, not
  only for a wrong order -- one raw apostrophe in a French or Uzbek string is enough.
- **A new key goes into all 39 files in the same change, translated** (workspace rule 6). The
  count check fails on a language that misses it.
- Then run the tests that read the files (keys and `{0}` holes are the same in every language,
  the generated `LocalizedStrings` surface matches the resource):
  `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~LocalizationTests|FullyQualifiedName~LocalizedStringsGeneratorTests"`
- Commit only when both CI steps pass locally. Review the diff the sort script made: it must
  touch nothing but the strings you edited.

## CI validators

The `validate` job of `.github/workflows/dotnet.yml` runs these before the build. Run the ones
that match what you touched; all of them take seconds.

| You touched | Run |
|---|---|
| `Localization/strings.json`, `Localization.Langs/*/strings.json` | the three localization commands above |
| a connector project, `StockSharp.slnx` | `python scripts/check-solution-connectors.py` -- the solution lists every project of `../Connectors` |
| `README.md`, `README.ru.md`, `README.zh.md`, connector logos | `python scripts/check-readme-connectors.py` (same connectors, order and logos in all three), `python scripts/check-connector-coverage.py` (every documented connector has a row) |
| a `doc.stocksharp.com` link in a README | `python scripts/check-readme-doc-links.py` -- needs the `StockSharp/doc` repo beside this one as `../doc`, or its path in `STOCKSHARP_DOC_ROOT` |
| anything under `scripts/` | `python -m unittest discover -s scripts -p "test_*.py"` |

## Gotchas / do not break

- **This repo's folder must be named `StockSharp (GitHub)`** and the `StockSharp/Connectors`
  repo must sit **beside it as `../Connectors`.** `StockSharp.slnx` and `StockSharp_CI.slnx` reference
  `../Connectors/*.csproj` and the connectors reference back into this repo, so the two must be
  siblings with this exact folder name (see the comment in `dotnet.yml`). `props/common_meta.props`
  also expects sibling `../StockSharpApps`, `../Web`, `../Connectors` paths.
- **`StockSharp.slnx` and `StockSharp_CI.slnx` build on Windows only** (desktop/connector
  surface). For anything meant to work cross-platform, build/test through `StockSharp_Tests.slnx`.
- **Three READMEs stay in sync.** `README.md`, `README.ru.md`, `README.zh.md` are kept
  consistent and validated by `scripts/check-readme-connectors.py` and
  `scripts/check-readme-doc-links.py`; when editing connector lists or doc links, update all three.
- **Connectors are not in this repo.** Add or fix a broker/exchange connector in
  `StockSharp/Connectors`, not here.
