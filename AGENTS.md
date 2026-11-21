# AGENTS.md

Instructions for AI agents and contributors working on **RetroAchievementsSharp**.

## What this project is

A native C# port of the RetroAchievements hashing engine used by RAHasher.
The behavioral source of truth is:

- `References/RAHasher-1.8.4` — CLI behavior (`RAHasher.cpp`, `Util.cpp`,
  `Hash3DS.cpp`, `HashCHD.cpp`)
- `References/rcheevos-12.5.0` — hashing algorithms (`src/rhash`)
- `References/dolphin-master` — RVZ/WIA reference material

Parity is the primary requirement. Do not “improve” hashing behavior, error
messages, verbose output, or CLI quirks unless the reference implementation
changed or the change is an explicit, documented hardening (for example a
managed-code crash guard). Every behavioral change needs a test and a
CHANGELOG entry.

## Repository layout

| Path | Contents |
|---|---|
| `RetroAchievementsSharp/` | Hashing library (net8.0, net9.0, net10.0, net11.0) |
| `RetroAchievementsSharp.Cli/` | Console application; output binary is `RetroAchievementsSharp.exe` |
| `RetroAchievementsSharp.Tests/` | Fast xUnit suite |
| `RetroAchievementsSharp.Slow.Tests/` | Manual parity / real-ROM / RVZ suite |
| `docs/` | MkDocs site source |
| `References/` | Read-only GPL reference material; never shipped, never edit |
| `packaging/` | Linux package metadata (deb/rpm; optional AppImage/Flatpak files kept for later) |
| `scripts/` | Local release helpers |
| `RetroAchievementsSharp/bin/Release/` | Local release-bundle drop folder |

`RetroAchievementsSharp/bin/Release/` is intentionally persistent: **never
delete files there**. Release scripts may add or overwrite their own outputs,
but must not clean the directory.

## Build and test

Use the .NET 11 SDK (see `global.json`) and build every target framework:

```powershell
dotnet restore RetroAchievementsSharp.sln
dotnet build RetroAchievementsSharp.sln -c Release
dotnet test RetroAchievementsSharp.sln -c Release
```

The fast suite must be green on **net8.0, net9.0, net10.0, and net11.0**.
Warnings are errors (`TreatWarningsAsErrors=true`).

The slow suite is manual and needs local oracles / ROM libraries:

```powershell
dotnet test RetroAchievementsSharp.Slow.Tests -c Release
```

Never disable parity tests or weaken assertions to make a build pass.

## CLI output

The CLI assembly name is `RetroAchievementsSharp`; the Windows apphost is
`RetroAchievementsSharp.exe`, and the Unix executable is
`RetroAchievementsSharp`. The project directory is still
`RetroAchievementsSharp.Cli/` — do not rename it without updating the
solution, project references, scripts, and docs.

## Release bundles

The next release contains six self-contained, single-file builds (the .NET 11
runtime is bundled; no runtime install is required on the target):

| Platform | Bundle |
|---|---|
| Windows x64 / arm64 | `.zip` |
| Linux x64 / arm64 | `.deb`, `.rpm`, `.tar.gz` |
| macOS x64 / arm64 | `.dmg`, `.tar.gz` |

All CLI bundle names use the same `v` prefix:

```text
retroachievementssharp_v<version>_<rid>.<ext>          # zip / tar.gz / dmg
retroachievementssharp_v<version>_<deb-arch>.deb
retroachievementssharp_v<version>-1.<rpm-arch>.rpm
```

where `<rid>` is `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`,
`macos-x64`, or `macos-arm64`. The NuGet package keeps the standard
`RetroAchievementsSharp.<version>.nupkg` naming required by NuGet tooling.

Build locally:

```powershell
./scripts/build-release.ps1 -Version 1.1.0
```

CI uses `.github/workflows/release.yml`. It builds bundles on native runners
and attaches them to the GitHub release when a `v*` tag is pushed. Manual
`workflow_dispatch` runs only produce workflow artifacts.

**Do not create or push a release tag unless the maintainer explicitly asks.**
A tag is what publishes the GitHub release.

Every shipped archive/dmg/package must contain `LICENSE`,
`THIRD-PARTY-NOTICES.md`, and `README.md` (GPL compliance and source link).

## Code conventions

- Nullable is enabled; XML docs are required on public members.
- Keep the 1:1 port comments when editing ported code; point to the C
  function/file being mirrored.
- Match the existing `PascalCase` / `_camelCase` style.
- Avoid new dependencies unless they are necessary and license-compatible.
  The project is GPL-2.0-or-later; rcheevos remains MIT.
- Tests use synthetic fixtures where possible. Do not commit ROMs, CHDs,
  3DS keys (`aes_keys.txt`, `seeddb.bin`), or other copyrighted game data.
- If you touch CLI output, run the parity suite and keep stdout/stderr
  byte-identical to the reference oracle.

## Documentation

Update `CHANGELOG.md` and the relevant `docs/` page for behavioral changes.
`docs/getting-started/publishing.md` documents local package/bundle creation.
The docs site is built from `docs/` by `.github/workflows/pages.yml`.

## Security

See `SECURITY.md`. Do not put real API keys, tokens, 3DS keys, or personal
paths in commits, tests, issues, or logs. The built-in bug-report/telemetry
keys are deliberately obfuscated and must not be replaced with plaintext
credentials.
