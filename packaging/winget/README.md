# Winget manifests

The `PureLogicCode.RetroAchievementsSharp` winget package installs the
self-contained Windows x64/arm64 CLI zips as a portable package. The command
is `retroachievementssharp`.

The manifests for the current release are under `1.1.0/`.

## Validate

```powershell
winget validate --manifest packaging/winget/1.1.0
winget show --manifest packaging/winget/1.1.0
```

`winget install --manifest` additionally requires the `LocalManifestFiles`
feature to be enabled by an administrator:

```powershell
winget settings --enable LocalManifestFiles
```

## Submit

The initial manifest is submitted to
[`microsoft/winget-pkgs`](https://github.com/microsoft/winget-pkgs) as a pull
request. Use `wingetcreate` or the GitHub API to open the PR; the repository
CI validates the manifest, installers, URLs, and installation.

## Automation

`.github/workflows/release.yml` has a `winget` job that runs after the GitHub
release is published for a `v*` tag. It:

1. downloads the pinned `wingetcreate.exe` and verifies its SHA256;
2. calls `wingetcreate update PureLogicCode.RetroAchievementsSharp` with the
   two Windows release zips and the new version;
3. submits the manifest update PR to `microsoft/winget-pkgs`.

The job is non-blocking, so a winget outage cannot fail the release. It
requires the repository Actions secret `WINGET_TOKEN`, a **classic** GitHub
PAT with the `public_repo` scope. Fine-grained tokens cannot open pull
requests against `microsoft/winget-pkgs`.

The first manifest must be merged before automatic updates can find the
package. Until then, the `winget` job fails with a clear log message.
