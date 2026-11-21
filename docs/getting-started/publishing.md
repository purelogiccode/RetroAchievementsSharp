# Publishing builds

RetroAchievementsSharp ships two kinds of artifacts:

1. **The `RetroAchievementsSharp` NuGet library** (`net8.0;net9.0;net10.0;net11.0`)
   — for consumers; see [Packaging the library](#packaging-the-library).
2. **Self-contained, single-file CLI executables** — the .NET 11 runtime is
   bundled, so no runtime install is needed on the target. The executable is
   `RetroAchievementsSharp.exe` on Windows and `RetroAchievementsSharp` on
   Linux/macOS.

## Release bundles

The release workflow produces these bundles:

| Platform | Architectures | Formats |
|---|---|---|
| Windows | x64, arm64 | `.zip` |
| Linux | x64, arm64 | `.deb`, `.rpm`, `.tar.gz` |
| macOS | x64, arm64 | `.dmg`, `.tar.gz` |

All CLI bundle names use the same `v` prefix:

```text
retroachievementssharp_v<version>_<rid>.<ext>          # zip / tar.gz / dmg
retroachievementssharp_v<version>_<deb-arch>.deb
retroachievementssharp_v<version>-1.<rpm-arch>.rpm
```

with `<rid>` = `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`,
`macos-x64`, or `macos-arm64`. The NuGet package keeps the standard
`RetroAchievementsSharp.<version>.nupkg` naming required by NuGet tooling.

**GPL compliance:** every bundle contains `LICENSE`,
`THIRD-PARTY-NOTICES.md`, and `README.md` alongside the executable. Never
publish a bare binary.

## Local builds

Windows can build the Windows zips, Linux `.tar.gz`, `.deb`/`.rpm` (when
[nfpm](https://nfpm.goreleaser.com/) is installed), macOS `.tar.gz`, and the
NuGet package:

```powershell
./scripts/build-release.ps1 -Version 1.1.0
```

The script writes bundles to `RetroAchievementsSharp/bin/Release/` and
**never deletes existing files there**. Use a different folder with
`-OutputRoot` if needed.

DMG bundles require macOS tooling, so CI builds them on a native macOS runner.
AppImage/Flatpak metadata remains under `packaging/flatpak/` and
`packaging/linux/` for a possible future release, but is not part of the
default 1.1.0 bundle set.

## Winget

Windows users can install the portable CLI with:

```powershell
winget install PureLogicCode.RetroAchievementsSharp
```

The manifests for the current release live under `packaging/winget/<version>/`.
The release workflow updates the `microsoft/winget-pkgs` manifest with
`wingetcreate` on every `v*` tag. Configure the repository Actions secret
`WINGET_TOKEN` with a **classic** GitHub PAT that has the `public_repo` scope
(fine-grained tokens cannot open PRs against `microsoft/winget-pkgs`).
The first submission must be merged before automatic updates can run.

## CI release workflow

`.github/workflows/release.yml`:

- builds all six RIDs as self-contained single-file .NET 11 binaries;
- creates the Windows/Linux/macOS bundles on native runners;
- uploads every bundle and the NuGet package as workflow artifacts;
- when a `v*` tag is pushed, attaches all of them to the GitHub release.

Manual `workflow_dispatch` runs build and upload artifacts without creating a
release. **Do not create or push a release tag unless the maintainer
explicitly asks.**

## Manual publish commands

The individual publish commands are:

```bash
dotnet publish RetroAchievementsSharp.Cli -c Release -f net11.0 -r win-x64     --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/win-x64
dotnet publish RetroAchievementsSharp.Cli -c Release -f net11.0 -r win-arm64   --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/win-arm64
dotnet publish RetroAchievementsSharp.Cli -c Release -f net11.0 -r linux-x64   --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/linux-x64
dotnet publish RetroAchievementsSharp.Cli -c Release -f net11.0 -r linux-arm64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/linux-arm64
dotnet publish RetroAchievementsSharp.Cli -c Release -f net11.0 -r osx-x64     --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/osx-x64
dotnet publish RetroAchievementsSharp.Cli -c Release -f net11.0 -r osx-arm64   --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/osx-arm64
```

Cross-publishing works from any host: the SDK downloads the matching runtime
pack from NuGet.

| RID | Executable |
|---|---|
| `win-x64`, `win-arm64` | `RetroAchievementsSharp.exe` |
| `linux-x64`, `linux-arm64` | `RetroAchievementsSharp` |
| `osx-x64`, `osx-arm64` | `RetroAchievementsSharp` |

The single file bundles the .NET runtime, the hashing engine, the CLI, and
all managed dependencies. Trimming is intentionally disabled so the tested
behavior is preserved.

## Packaging the library

`RetroAchievementsSharp` is packable and carries complete NuGet metadata:
`net8.0;net9.0;net10.0;net11.0` assemblies, XML docs on every public member
(CS1591 is an error), the GPL-2.0-or-later license and third-party notices, a
package icon, SourceLink (with symbols), repository metadata, and a
`PackageReleaseNotes` link to `CHANGELOG.md`. Package validation runs on
every pack and fails if a public API change would break consumers.

```bash
dotnet pack RetroAchievementsSharp -c Release -o artifacts
```

This produces `RetroAchievementsSharp.<version>.nupkg` and
`RetroAchievementsSharp.<version>.snupkg`.

### Packaging the CLI tool

The CLI is also a .NET global tool (`RetroAchievementsSharp.Cli`, command
`retroachievementssharp`):

```bash
dotnet pack RetroAchievementsSharp.Cli -c Release -p:TargetFrameworks=net8.0 -o artifacts
```

Install it from the local feed:

```bash
dotnet tool install --global --add-source artifacts RetroAchievementsSharp.Cli
retroachievementssharp NES game.nes
```

The release workflow packs the tool for `net8.0` and publishes it alongside
the library.

### Pushing to NuGet.org

The release workflow publishes the library and CLI tool packages to NuGet.org
on every `v*` tag. Configure the repository Actions secret `NUGET_API_KEY`
with a NuGet.org key that has push permission for both
`RetroAchievementsSharp` and `RetroAchievementsSharp.Cli`.

Manual push:

```bash
dotnet nuget push artifacts/RetroAchievementsSharp.1.1.0.nupkg \
  --api-key <NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/RetroAchievementsSharp.1.1.0.snupkg \
  --api-key <NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/RetroAchievementsSharp.Cli.1.1.0.nupkg \
  --api-key <NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

Before a release:

1. Bump `<Version>` in `RetroAchievementsSharp/RetroAchievementsSharp.csproj`
   and `RetroAchievementsSharp.Cli/RetroAchievementsSharp.Cli.csproj`, update
   `Program.Version`, and add a `CHANGELOG.md` entry (Keep a Changelog format).
2. Run the fast suite on every TFM
   (`dotnet test RetroAchievementsSharp.sln -c Release`), then the slow suite
   (`dotnet test RetroAchievementsSharp.Slow.Tests -c Release`).
3. Build the bundles locally with `scripts/build-release.ps1`, or run the
   release workflow manually.
4. When the maintainer approves, push a `v<version>` tag. The release
   workflow creates the GitHub release with all bundles, publishes the
   library and CLI tool to NuGet.org, and updates winget-pkgs.
5. Use a NuGet API key scoped to both package IDs, and a `WINGET_TOKEN`
   classic PAT with `public_repo`; never commit either secret.
