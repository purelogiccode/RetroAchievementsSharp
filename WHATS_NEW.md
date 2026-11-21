# What's new

## 1.1.1 — 2026-11-21

- **.NET 10/11 only** — the library, CLI, and tests now target `net10.0`
  and `net11.0`; .NET 8/9 support was dropped because CHDSharp 1.7.1
  requires `Microsoft.Extensions.Logging.Abstractions` 11. Stay on 1.1.0
  if you need net8.0/net9.0.
- **CHDSharp 1.7.1** — fixes FLAC frame metadata for one-frame-per-hunk CD
  audio.
- **CLI .NET global tool** —
  `dotnet tool install --global RetroAchievementsSharp.Cli`.
- **winget** — `winget install PureLogicCode.RetroAchievementsSharp`.
- **Release automation** — `v*` tags publish the library, CLI tool, and
  symbols to NuGet.org; GitHub releases carry only the platform bundles.

## 1.1.0 — 2026-11-21

- **PlayStation 3 hashing** — `.iso`/`.chd` discs, extracted retail folders,
  installed PKG layouts, flat `EBOOT.BIN`, and `.m3u` playlists.
- **Differential CHDs** — child CHDs now locate and open their parent by
  header MD5/SHA1, fixing `Could not open track` for parent/child sets.
- **rcheevos 12.5.0 / RAHasher 1.8.4 sync** — buffered CD fallback, Arcade
  folder hashing, Arcade `.zip` single-`.neo` handling.
- **`scan --console` / `scan --out`** — forced-console batch hashing and
  atomic manifest output; JSON includes the full file path.
- **CLI binary renamed** to `RetroAchievementsSharp.exe` on Windows and
  `RetroAchievementsSharp` on Linux/macOS.
- **.NET 8/9/10/11 support** for the NuGet library; release CLI bundles
  include the .NET 11 runtime.
- **Release bundles** — Windows `.zip`; Linux `.tar.gz`/`.deb`/`.rpm`;
  macOS `.dmg`/`.tar.gz`; x64 and arm64.
- **Install options** — Windows `winget`, the `RetroAchievementsSharp.Cli`
  .NET global tool, the NuGet library, or a self-contained bundle.
- **CI and security** — GitHub Actions CI on Windows/Linux/macOS; Dependabot,
  CodeQL, secret scanning, push protection, and private vulnerability
  reporting.

Full notes: [docs/ReleaseNotes.md](docs/ReleaseNotes.md).  
Changelog: [CHANGELOG.md](CHANGELOG.md).
