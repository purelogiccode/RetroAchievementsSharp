# Installation

## Prerequisites

- **.NET 11 SDK** — all three projects multi-target the portable
  `net8.0;net9.0;net10.0;net11.0` TFMs (`global.json` pins the .NET 11 SDK;
  .NET 10 SDK is required only to test the `net10.0` target). Install from
  <https://dotnet.microsoft.com/download> or your package manager
  (`apt install dotnet-sdk-10.0`, `winget install Microsoft.DotNet.SDK.10`, …).
- **No native dependencies** — the engine is 100 % managed. `CHDSharp` and
  `VideoGameFileSystemParser` are pure managed NuGet packages.
- **Windows, Linux, macOS** all work for building; the CLI publishes for
  Windows x64/arm64, Linux x64/arm64, and macOS x64/arm64 (see
  [Publishing](publishing.md)). Release CLI bundles include the .NET 11
  runtime; the NuGet library targets .NET 8, 9, 10, and 11.
- The **parity test suite additionally needs a C-built oracle binary on
  Windows** — see [Building the oracles](../development/oracles.md). Without
  one, the Tier-2 parity cases skip (the ported vectors still run).

`RetroAchievementsSharp` is also available as a **NuGet package**
(`dotnet add package RetroAchievementsSharp`) for use from any net8.0+ application —
see [Packaging the library](publishing.md#packaging-the-library).

## Install the CLI

Windows Package Manager (portable, no .NET install required):

```powershell
winget install PureLogicCode.RetroAchievementsSharp
```

.NET global tool (Windows, Linux, macOS with .NET 8+):

```bash
dotnet tool install --global RetroAchievementsSharp.Cli
retroachievementssharp NES game.nes
```

The self-contained single-file bundles for Windows, Linux, and macOS remain
available from the [GitHub releases](https://github.com/purelogiccode/RetroAchievementsSharp/releases);
see [Publishing](publishing.md) for the bundle formats.

## Clone & build

```bash
git clone https://github.com/purelogiccode/RetroAchievementsSharp.git
cd RetroAchievementsSharp
dotnet build RetroAchievementsSharp.sln -c Release
```

The solution contains three projects (plus a manual-only slow test project
kept out of the solution):

| Project | Kind | Purpose |
|---|---|---|
| `RetroAchievementsSharp` | class library | the hashing engine (public API mirror of `include/rc_hash.h`) |
| `RetroAchievementsSharp.Cli` | console app | the RAHasher-compatible command line (`RetroAchievementsSharp.exe`) |
| `RetroAchievementsSharp.Tests` | xUnit (fast) | ported rcheevos vectors + engine/CLI unit tests — in the solution |
| `RetroAchievementsSharp.Slow.Tests` | xUnit (slow) | parity harness vs. C oracles, real-ROM, RVZ, published-DB — run manually, not in the solution |

NuGet dependencies:

| Package | Version | License | Role |
|---|---|---|---|
| `CHDSharp` | 1.6.3 | MIT | CHD V1–V5 reading (`ChdFile`, `Tracks`, metadata) |
| `RVZSharp` | 1.2.0 | GPL-2.0-or-later | GameCube/Wii RVZ/WIA live hashing (`RvzFilereader`) |
| `VideoGameFileSystemParser` | 1.4.0 | MIT | alternative ISO9660/UDF backend behind `FileSystemResolver` |
| `Serilog` | 4.4.0 | Apache-2.0 | logging |

## Run the test suite

```bash
dotnet test RetroAchievementsSharp.sln -c Release            # fast suite
dotnet test RetroAchievementsSharp.Slow.Tests -c Release     # slow suite (parity; manual)
```

Expected result: **415 passed, 0 failed per TFM on the fast suite**
(net8.0, net9.0, net10.0, net11.0), **172 passed on the slow suite**. The suites
have several tiers — see [Testing](../development/testing.md) for details
and how to filter them.

## Verify the build

```bash
RetroAchievementsSharp.Cli/bin/Release/net11.0/RetroAchievementsSharp.exe GB <any-file>
```

prints the 32-character MD5 of the file (the same hash RAHasher 1.8.4
prints). No-args prints the console table and usage:

```bash
RetroAchievementsSharp.Cli/bin/Release/net11.0/RetroAchievementsSharp.exe
```

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `error NETSDK1005: Assets file ... not found` | run `dotnet restore` once |
| parity tests report `SKIPPED` | no usable oracle found on a Windows host — build one (see [oracles](../development/oracles.md)) or set `RASHARP_ORACLE` |
| build fails on a warning | `TreatWarningsAsErrors` is on by design; fix the warning rather than suppressing it |
| `RetroAchievementsSharp.exe` not found under `bin/Release/net11.0-windows` | the projects target portable `net8.0;net9.0;net10.0;net11.0` — look under `bin/Release/<tfm>/` (any of the four works) |
