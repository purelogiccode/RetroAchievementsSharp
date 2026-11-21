# Release Notes — RetroAchievementsSharp 1.1.1

**A native C# port of the RAHasher 1.8.4 hashing engine (rcheevos 12.5.0)
that produces 100% identical RetroAchievements hashes** — the same hashes
the [RetroAchievements](https://retroachievements.org) website and its
clients use to identify ROMs and disc images.

## What's new in 1.1.1

- **.NET 10 and 11 only** — the library, CLI, and tests now target
  `net10.0;net11.0`. The `net8.0`/`net9.0` targets were dropped because
  CHDSharp 1.7.1 requires `Microsoft.Extensions.Logging.Abstractions` 11,
  which has no compatible .NET 8/9 runtime asset. Consumers still on
  .NET 8 or 9 should stay on 1.1.0.
- **CHDSharp 1.7.1** — fixes FLAC frame metadata for one-frame-per-hunk CD
  audio; no public API or encoder-output changes.
- **CLI .NET global tool and winget** — install with
  `dotnet tool install --global RetroAchievementsSharp.Cli` on any OS, or
  `winget install PureLogicCode.RetroAchievementsSharp` on Windows.
- **Release automation** — `v*` tags publish the library, CLI tool, and
  symbols to NuGet.org; GitHub releases attach only the platform bundles.

## Highlights

- **PlayStation 3 support** — hash PS3 `.iso`/`.chd` discs, extracted retail
  folders, installed PKG layouts, flat `EBOOT.BIN` files, and `.m3u`
  playlists. PS3 is now in the CLI console table (id 82).
- **Differential CHD support** — child CHDs automatically find their parent
  in the same directory by matching the parent MD5/SHA1 hashes. This fixes
  the `Could not open track` failure for parent/child CHD sets.
- **rcheevos 12.5.0 / RAHasher 1.8.4 parity** — including buffered
  `.cue`/`.m3u`/`.iso`/`.chd` fallback hashing, Arcade folder hashing, and
  Arcade `.zip` single-`.neo` handling.
- **`scan --console` and `scan --out`** — force one console for a whole
  batch and write manifests atomically; JSON rows include the full path.
- **Byte-exact parity with the C engine** — **439/439 fast tests + 172/172
  slow tests** green on `net10.0` and `net11.0`, including the Tier-2
  parity harness against source-built C oracles.
- **Cross-platform release bundles** — Windows, Linux, and macOS x64/arm64,
  with the .NET 11 runtime bundled so no runtime install is required.
- **Project automation and security** — GitHub Actions CI, release bundles,
  Dependabot, CodeQL, secret scanning, push protection, and private
  vulnerability reporting.

## Requirements

- **.NET 10 or 11** for the library (older runtimes are not supported).
- No native dependencies — 100% managed.

## Supported formats

Raw ROMs (all cartridge consoles), `.zip` (pre-loaded ROM, Arduboy FX,
DOSZ/Zip64/DOSC), `.m3u` playlists, discs (`.cue`/`.bin`/`.iso`/`.gdi`/`.chd`,
GameCube/Wii `.rvz`/`.wia`), PS3 `.iso`/`.chd` and extracted/PKG folders,
3DS `.cia`/`.3ds`/`.3dsx` (keys via `-s` / `Hash3Ds.InitHash3Ds`), and Neo
Geo `.neo` carts.

Console list: NES/Famicom, SNES/SFC, N64, GB/GBC/GBA, Master System, Mega
Drive/Genesis, Game Gear, 32X, SG-1000, PCE/TG-16, PCE-CD, Saturn, Sega CD,
Dreamcast, PS1, PS2, PSP, PS3, 3DO, PC-FX, Jaguar(+CD), Neo Geo
Pocket(+Color), Neo Geo CD, NDS/DSi/3DS — the full rcheevos console table,
including classic microcomputers.

## Downloads

| Asset | Platform |
|---|---|
| `retroachievementssharp_v1.1.1_win-x64.zip` | Windows x64 (self-contained, single-file) |
| `retroachievementssharp_v1.1.1_win-arm64.zip` | Windows arm64 (self-contained, single-file) |
| `retroachievementssharp_v1.1.1_linux-x64.tar.gz` | Linux x64 (portable) |
| `retroachievementssharp_v1.1.1_linux-arm64.tar.gz` | Linux arm64 (portable) |
| `retroachievementssharp_v1.1.1_amd64.deb` | Debian/Ubuntu x64 |
| `retroachievementssharp_v1.1.1_arm64.deb` | Debian/Ubuntu arm64 |
| `retroachievementssharp_v1.1.1-1.x86_64.rpm` | Fedora/RHEL x64 |
| `retroachievementssharp_v1.1.1-1.aarch64.rpm` | Fedora/RHEL arm64 |
| `retroachievementssharp_v1.1.1_macos-x64.dmg` | macOS Intel |
| `retroachievementssharp_v1.1.1_macos-arm64.dmg` | macOS Apple Silicon |
| `retroachievementssharp_v1.1.1_macos-x64.tar.gz` | macOS Intel (portable CLI) |
| `retroachievementssharp_v1.1.1_macos-arm64.tar.gz` | macOS Apple Silicon (portable CLI) |

The library, CLI tool, and symbols are published to
[NuGet.org](https://www.nuget.org/packages/RetroAchievementsSharp); the
`.nupkg`/`.snupkg` files are not attached to the GitHub release.

Each CLI archive/package includes `LICENSE`, `THIRD-PARTY-NOTICES.md`, and
`README.md` alongside the binary.

## CLI installation

```powershell
# Windows Package Manager
winget install PureLogicCode.RetroAchievementsSharp
```

```bash
# .NET global tool (Windows, Linux, macOS with .NET 10+)
dotnet tool install --global RetroAchievementsSharp.Cli
```

The self-contained bundles require no .NET installation.

## CLI quick start

```bash
retroachievementssharp NES game.nes
retroachievementssharp PS1 disc.cue
retroachievementssharp PS3 game.iso
retroachievementssharp -s C:\RetroArch\system 3DS game.cia
retroachievementssharp scan --console PS1 --out hashes.json C:\Roms\PSX
```

## Library quick start

```csharp
using RetroAchievementsSharp;

if (RcHash.GenerateFromFile(out string hash, ConsoleIds.RcConsoleNintendo, "game.nes"))
    Console.WriteLine(hash); // 32 hex chars — matches the published RA hash
```

## Credits

- **[LeXofLeviafan](https://github.com/LeXofLeviafan/)** — author of the
  RALibretro RAHasher CLI this project is behaviorally compatible with and
  that we use as the reference oracle in our parity test suite.
- **[RetroAchievements](https://retroachievements.org)** / [rcheevos](https://github.com/RetroAchievements/rcheevos)
  — the hashing engine, ported 1:1 (MIT).

## Links

- Repository: <https://github.com/purelogiccode/RetroAchievementsSharp>
- Docs: <https://purelogiccode.github.io/RetroAchievementsSharp/>
- NuGet: <https://www.nuget.org/packages/RetroAchievementsSharp>
- License: GPL-2.0-or-later (see `LICENSE` and `THIRD-PARTY-NOTICES.md`)

## Changelog

See [CHANGELOG.md](https://github.com/purelogiccode/RetroAchievementsSharp/blob/master/CHANGELOG.md).
