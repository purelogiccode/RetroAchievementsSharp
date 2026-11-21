# What's new

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
- **CI and security** — GitHub Actions CI on Windows/Linux/macOS; Dependabot,
  CodeQL, secret scanning, push protection, and private vulnerability
  reporting.

Full notes: [docs/ReleaseNotes.md](docs/ReleaseNotes.md).  
Changelog: [CHANGELOG.md](CHANGELOG.md).
