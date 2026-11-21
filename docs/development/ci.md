# CI, security, and releases

## CI

`.github/workflows/ci.yml` runs on pushes to `master`, pull requests, and
manual dispatch:

- `test` matrix — Ubuntu, Windows, and macOS, with .NET 8/9/10/11 installed;
  restores, builds (warnings are errors), and runs the fast xUnit suite on
  every target framework.
- `package` — packs `RetroAchievementsSharp` (`.nupkg` + `.snupkg`) and
  uploads it as a workflow artifact.

The slow parity/real-ROM suite is not run in CI because it needs local C
oracles, ROM libraries, and 3DS keys. Run it manually before a release.

CodeQL default setup, Dependabot alerts/security updates, secret scanning,
push protection, and private vulnerability reporting are enabled on the
GitHub repository. See `SECURITY.md` for reporting.

## Release bundles

`.github/workflows/release.yml` builds the release artifacts on native
runners:

| Platform | Runner(s) | Bundles |
|---|---|---|
| Windows x64/arm64 | `windows-latest` | `.zip` |
| Linux x64 | `ubuntu-24.04` | `.deb`, `.rpm`, `.tar.gz` |
| Linux arm64 | `ubuntu-24.04-arm` | `.deb`, `.rpm`, `.tar.gz` |
| macOS x64 | `macos-15-intel` | `.dmg`, `.tar.gz` |
| macOS arm64 | `macos-15` | `.dmg`, `.tar.gz` |

All CLI binaries are published with:

```text
-f net11.0 --self-contained true
-p:PublishSingleFile=true
-p:IncludeNativeLibrariesForSelfExtract=true
```

The published apphost is copied to the user-facing name
`RetroAchievementsSharp.exe` (Windows) or `RetroAchievementsSharp`
(Linux/macOS). The NuGet package is packed with the release version and
attached too.

Bundles are always staged in
`RetroAchievementsSharp/bin/Release/` on each runner and uploaded as
workflow artifacts. When a `v*` tag is pushed, the final job attaches every
bundle to the GitHub release.

A manual `workflow_dispatch` run builds and uploads artifacts **without**
creating a release. Do not push a release tag until the maintainer asks.

## Local bundles

On Windows:

```powershell
./scripts/build-release.ps1 -Version 1.1.0
```

This produces Windows `.zip`, Linux `.tar.gz`, `.deb`/`.rpm` (when `nfpm` is
installed), macOS `.tar.gz`, and the NuGet package in
`RetroAchievementsSharp/bin/Release/`. That folder is persistent: scripts
must never delete files inside it. DMG bundles require macOS tooling and are
produced by CI. AppImage/Flatpak metadata is kept for a possible later
release but is not published by default.

## Security configuration

Repository security settings are managed from the GitHub **Security** tab:

- private vulnerability reporting;
- Dependabot alerts and security updates (weekly grouped PRs configured in
  `.github/dependabot.yml`);
- secret scanning and push protection;
- CodeQL default setup.

Do not commit secrets, `aes_keys.txt`, `seeddb.bin`, or copyrighted ROM
images. The built-in API keys are intentionally obfuscated.
