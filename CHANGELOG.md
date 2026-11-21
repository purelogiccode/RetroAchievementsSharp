# Changelog

All notable changes to **RetroAchievementsSharp** are documented here (keep-a-changelog format).

The project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
For the port history, see [ReleaseNotes.md](docs/ReleaseNotes.md) and the
`docs/` site.

## [Unreleased]

### Changed
- Release workflow publishes the NuGet package and symbols to NuGet.org on
  every `v*` tag (using the `NUGET_API_KEY` Actions secret).

## [1.1.0] - 2026-11-21

### Added
- **PlayStation 3 hashing** (rcheevos 12.5.0): `.iso`/`.chd` discs hash
  `PS3_GAME/PARAM.SFO` and `PS3_GAME/USRDIR/EBOOT.BIN`; extracted retail
  discs, installed PKG folders, flat `EBOOT.BIN` layouts, and `.m3u`
  playlists are supported. The CLI console table now lists PS3 (id 82).
- **Differential CHD support** (RAHasher 1.8.4): a child CHD that needs its
  parent now finds the matching parent in the same directory by comparing
  the header MD5/SHA1 hashes. This fixes the `Could not open track` failure
  for parent/child CHD sets.
- **Arcade extracted-romset hashing** (rcheevos 12.5.0): a path without an
  extension is hashed by folder name, like the corresponding zip.
- **Buffered CD fallback** (rcheevos 12.5.0): buffered `.cue`/`.m3u`/`.iso`/
  `.chd` inputs ignore the buffer and reopen the media from disk.
- **CI, release automation, and security policy**: GitHub Actions CI on
  Windows/Linux/macOS; release bundles for Windows/Linux/macOS x64+arm64;
  Dependabot, CodeQL, secret scanning, and private vulnerability reporting.
- `scan --console` and `scan --out` for forced-console batch hashing and
  atomic manifest files; JSON rows include the full `path`.

### Changed
- Synced the hashing engine to **RAHasher 1.8.4 / rcheevos 12.5.0**.
- Arcade `.zip` handling matches 1.8.4: a single `.neo` entry is hashed by
  content; every other zip is hashed by filename.
- CLI binary is now `RetroAchievementsSharp.exe` on Windows and
  `RetroAchievementsSharp` on Linux/macOS. Release bundles use the
  `retroachievementssharp_v<version>_<rid>` naming convention.
- All projects target `net8.0;net9.0;net10.0;net11.0`; the library package is
  compatible with .NET 8, 9, 10, and 11.
- CHDSharp 1.6.3 is used on every TFM. CHDSharp 1.7.x pulls
  `Microsoft.Extensions.Logging.Abstractions` 11, whose
  `System.Diagnostics.DiagnosticSource` 11 dependency breaks
  `System.Net.Http` on .NET 9; 1.7.0/1.7.1 had no public API or reader
  changes (TFM support and encoder fixes only).
- Test suite: **439/439 fast tests + 172/172 slow tests** green on each TFM.
- Parity harness prefers the CLI built for the test assembly's own TFM.

## [1.0.0] - 2026-08-15

### Added
- Complete native C# port of the RAHasher 1.8.3 / rcheevos 12.4.0 hashing
  engine: cartridge (NES/FDS, SNES, N64, NDS/DSi, PCE, SCV, 7800, Lynx,
  Arduboy, Neo Geo `.neo`, Arcade), disc (PSX, PS2, PSP, Saturn, Sega CD,
  PCE-CD, PC-FX, 3DO, Jaguar CD, Neo Geo CD, Dreamcast, GameCube, Wii), CHD,
  RVZ/WIA (GameCube/Wii, hashed live via RVZSharp — no rvz→iso conversion),
  zip (Arduboy FX, DOSZ/DOSC), m3u, and 3DS (CIA/NCCH/3DSX with
  `aes_keys.txt`/`seeddb.bin`).
- CLI with byte-identical behavior to RAHasher 1.8.3, plus the `scan`,
  `consoles`, `checkkeys`, `identify`, and `fetch-db` subcommands.
- NuGet package `RetroAchievementsSharp` 1.0.0 (net8.0;net9.0;net10.0, GPL-2.0-or-later,
  SourceLink, XML docs on every public member, symbols, package validation);
  every public member of the library is documented (CS1591 is an error).
- Test suite: all ported rcheevos vectors, synthetic 3DS fixtures, a parity
  harness vs. source-built C oracles, RVZ-vs-ISO equality on real
  GameCube/Wii discs, and real-ROM parity — **587/587 green on net8.0,
  net9.0, and net10.0**.
- All logging routed through Serilog; the CLI forwards Warning+ events to
  the bug report API and reports usage telemetry at launch (both
  disable-able via environment variables — see `docs/development/logging.md`).
- Docs site on GitHub Pages (MkDocs Material, left-nav sidebar) deployed
  automatically by CI, plus a hand-maintained GitHub wiki.

### Fixed
- Three real CLI parity bugs caught by the harness (arg-count guard crash,
  wildcard path construction, usage banner blank line), and the inherited
  12.2.1 `merge_callbacks` bug synced to 12.4.0.
- `TestHash3Ds` flakiness under parallel multi-TFM test runs (shared temp
  dirs replaced with per-instance dirs).

### Removed
- Dead code: the unused `HashEngine.NotYetImplemented` stub and
  `HashEngine.MergeCallbacks` (the C merge lives in `rc_hash_reset_iterator`,
  ported as `HashEngine.ResetIterator`).

### Changed
- License: the library links RVZSharp (GPL-2.0-or-later, Dolphin-derived),
  so RetroAchievementsSharp is distributed under GPL-2.0-or-later (the ported rcheevos
  engine stays MIT and is credited in THIRD-PARTY-NOTICES.md).

---

[Unreleased]: https://github.com/purelogiccode/RetroAchievementsSharp/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/purelogiccode/RetroAchievementsSharp/releases/tag/v1.1.0
[1.0.0]: https://github.com/purelogiccode/RetroAchievementsSharp/releases/tag/v1.0.0