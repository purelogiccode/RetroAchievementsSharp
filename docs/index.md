# RetroAchievementsSharp

**A native C# port of the RAHasher 1.8.4 CLI that produces 100 % identical
RetroAchievements hashes — now tracking the current rcheevos release
(12.5.0) as the single source of truth.**

RetroAchievementsSharp is a GPL-2.0-or-later class library + CLI that hashes ROMs and disc
images exactly the way RetroAchievements identifies games: same algorithms,
same constants, same edge cases, byte-for-byte. The engine is a 1:1
translation of the `rc_hash` module of [rcheevos](https://github.com/RetroAchievements/rcheevos),
originally pinned to the RAHasher 1.8.3 submodule commit (`40d916d`,
rcheevos 12.2.1) and since evolved to rcheevos 12.5.0 / RAHasher 1.8.4.

## Highlights

- **100 % parity, proven** — a Tier-2 harness runs `RetroAchievementsSharp.exe` and the
  original C binaries with identical arguments and requires byte-identical
  stdout/stderr and equal exit codes: **90/90 corpus cases green** against
  the source-built C oracle; **439/439 fast tests + 172/172 slow
  tests green on each of net10.0, net11.0** (Debug + Release).
- **82 consoles** in the CLI table — cartridge, disc, encrypted (3DS), CHD
  (including differential parent/child sets), PS3, RVZ/WIA (GameCube/Wii,
  hashed live — no rvz→iso conversion), zip-based (Arduboy FX, DOSZ/DOSC)
  and `.neo` Neo Geo cart formats.
- **Cross-platform** — `RetroAchievementsSharp` is a NuGet library targeting
  `net10.0;net11.0`; the CLI publishes self-contained single-file
  executables for Windows x64/arm64, Linux x64/arm64, and macOS x64/arm64.
- **Honest engineering** — the port reproduces the original's quirks
  (64 MiB whole-file cap, `atoi` console-key fallback, wildcard path
  construction) *and* inherits its bugfixes (the 12.4.0 `merge_callbacks`
  fix was ported the day the C# port's copy of the bug was found).
- **GPL-2.0-or-later** — the ported engine core is MIT (rcheevos, credited
  in `THIRD-PARTY-NOTICES.md`); RVZ/WIA hashing links RVZSharp
  (GPL-2.0-or-later, Dolphin-derived), so the library is distributed under
  GPL-2.0-or-later. The GPL-3.0 RAHasher reference material is used only as
  a behavioral reference and is never shipped.

## Quick start

```bash
dotnet build RetroAchievementsSharp.sln -c Release
dotnet test  RetroAchievementsSharp.sln -c Release

RetroAchievementsSharp.Cli/bin/Release/net11.0/RetroAchievementsSharp.exe NES game.nes
RetroAchievementsSharp.Cli/bin/Release/net11.0/RetroAchievementsSharp.exe PS1 disc.cue
RetroAchievementsSharp.Cli/bin/Release/net11.0/RetroAchievementsSharp.exe '?' unknown.bin
```

## Project status

| Part | Scope | Status |
|---|---|---|
| Part I (phases 0–8) | Port of RAHasher 1.8.3 / rcheevos `40d916d` (12.2.1) | ✅ Complete — 82/82 parity vs the source-built 1.8.3 oracle |
| Part II (phases E0–E6) | Evolution to rcheevos 12.4.0 (single source of truth) | ✅ Complete — 12.4.0 oracle; E5 real-world validation done (real-ROM parity 61/61 + published-hash spot checks 15/15); release-sync playbook published |
| Part III | Sync to RAHasher 1.8.4 / rcheevos 12.5.0 | ✅ Complete — PS3 hashing, differential CHDs, buffered CD fallback, arcade folders |

## Documentation map

- **Getting Started** — [installation](getting-started/installation.md), [CLI usage](getting-started/usage.md), [publishing builds](getting-started/publishing.md)
- **Architecture** — [overview](architecture/overview.md) and deep dives into the
  [engine](architecture/engine.md), [cartridges](architecture/cartridge.md),
  [disc hashing](architecture/disc.md), [zip](architecture/zip.md),
  [3DS encryption](architecture/encrypted.md), the [CLI](architecture/cli.md)
  and the [parity harness](architecture/parity-harness.md)
- **Development** — [testing](development/testing.md), [building the oracles](development/oracles.md),
  the [porting guide](development/porting-guide.md) and the
  [release-sync playbook](development/sync-rcheevos.md)
- **Reference** — [console table](reference/console-table.md),
  [hashing semantics](reference/hashing-semantics.md), [public API](reference/public-api.md),
  [known quirks](reference/known-quirks.md), [parity evidence](reference/parity-evidence.md)
- **Release notes** — [RetroAchievementsSharp 1.1.1](ReleaseNotes.md)

## Repository layout

```
RetroAchievementsSharp.sln                 solution (Core + Cli + Tests)
Directory.Build.props       net10.0/net11.0 multi-targeting, nullable, warnings-as-errors
mkdocs.yml                  this documentation site
docs/                       these pages
RetroAchievementsSharp/               the hashing engine (class library)
RetroAchievementsSharp.Cli/                the RAHasher-compatible command line
RetroAchievementsSharp.Tests/              ported rcheevos vectors + the Tier-2 parity harness
References/                 read-only reference material (never shipped):
                            rcheevos-12.5.0, RAHasher-1.8.4, dolphin-master,
                            C oracle binaries (GPL, local test oracles only)
```

## License

GPL-2.0-or-later — see [license.md](license.md) and `THIRD-PARTY-NOTICES.md`
in the repo. Copyright (c) 2026 Peterson Fernandes and Pure Logic Code.
