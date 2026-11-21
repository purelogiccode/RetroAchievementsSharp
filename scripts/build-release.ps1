#requires -Version 7.0
<#
.SYNOPSIS
    Builds RetroAchievementsSharp release bundles into a persistent drop folder.

.DESCRIPTION
    Builds self-contained, single-file .NET 11 CLI binaries and the bundles that
    can be produced on the current host. Windows zips, Linux tar.gz/deb/rpm, and
    the NuGet package are built on Windows when the required tools are present.
    AppImage/Flatpak bundles require Linux; DMGs require macOS; those are built
    by .github/workflows/release.yml on native runners.

    The output folder is never cleaned or deleted. Existing unrelated files are
    left untouched; bundles with the same name are overwritten.

.PARAMETER Version
    Bundle version without a leading "v". Defaults to <Version> in
    RetroAchievementsSharp/RetroAchievementsSharp.csproj.

.PARAMETER OutputRoot
    Drop folder. Defaults to RetroAchievementsSharp/bin/Release in the repo.

.EXAMPLE
    ./scripts/build-release.ps1 -Version 1.1.0
#>
param(
    [string]$Version,
    [string]$OutputRoot,
    [switch]$SkipNuGet
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

if (-not $Version) {
    [xml]$project = Get-Content (Join-Path $repoRoot 'RetroAchievementsSharp/RetroAchievementsSharp.csproj')
    $Version = $project.Project.PropertyGroup.Version | Select-Object -First 1
}
$Version = $Version.TrimStart('v')

if (-not $OutputRoot) {
    $OutputRoot = Join-Path $repoRoot 'RetroAchievementsSharp/bin/Release'
}

New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null
$OutputRoot = (Resolve-Path $OutputRoot).Path
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("retroachievementssharp-release-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

$tfm = 'net11.0'
$publishProps = @(
    '-c', 'Release',
    '-f', $tfm,
    '--self-contained', 'true',
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:EnableCompressionInSingleFile=true',
    '-p:DebugType=None',
    '-p:DebugSymbols=false'
)

function Publish-Cli([string]$rid) {
    $destination = Join-Path $tempRoot "publish-$rid"
    Write-Host "Publishing $rid ..."
    & dotnet publish (Join-Path $repoRoot 'RetroAchievementsSharp.Cli/RetroAchievementsSharp.Cli.csproj') @publishProps -r $rid -o $destination | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $rid" }
    return $destination
}

function New-Stage([string]$name, [string]$binary) {
    $stage = Join-Path $tempRoot "stage-$name"
    New-Item -ItemType Directory -Force -Path $stage | Out-Null
    Copy-Item $binary $stage
    Copy-Item (Join-Path $repoRoot 'LICENSE') $stage
    Copy-Item (Join-Path $repoRoot 'THIRD-PARTY-NOTICES.md') $stage
    Copy-Item (Join-Path $repoRoot 'README.md') $stage
    return $stage
}

try {
    # ---- Windows -----------------------------------------------------------
    foreach ($rid in @('win-x64', 'win-arm64')) {
        $publish = Publish-Cli $rid
        $stage = New-Stage $rid (Join-Path $publish 'RetroAchievementsSharp.Cli.exe')
        Move-Item (Join-Path $stage 'RetroAchievementsSharp.Cli.exe') (Join-Path $stage 'RetroAchievementsSharp.exe')
        $zip = Join-Path $OutputRoot "retroachievementssharp_v${Version}_${rid}.zip"
        Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
        Write-Host "Created $zip"
    }

    # ---- Linux (archives; deb/rpm when nfpm is available) -------------------
    foreach ($rid in @('linux-x64', 'linux-arm64')) {
        $publish = Publish-Cli $rid
        $stage = New-Stage $rid (Join-Path $publish 'RetroAchievementsSharp.Cli')
        Move-Item (Join-Path $stage 'RetroAchievementsSharp.Cli') (Join-Path $stage 'RetroAchievementsSharp')
        $linuxStage = Join-Path $tempRoot "linux-$rid"
        $folder = "retroachievementssharp_v${Version}_${rid}"
        New-Item -ItemType Directory -Force -Path (Join-Path $linuxStage $folder) | Out-Null
        Copy-Item (Join-Path $stage '*') (Join-Path $linuxStage $folder) -Recurse
        $tar = Join-Path $OutputRoot "$folder.tar.gz"
        & tar -C $linuxStage -czf $tar $folder
        if ($LASTEXITCODE -ne 0) { throw "tar failed for $rid" }
        Write-Host "Created $tar"
    }

    # ---- macOS (tar.gz; the DMG needs native macOS tooling) ----------------
    foreach ($rid in @('osx-x64', 'osx-arm64')) {
        $bundleArch = if ($rid -eq 'osx-x64') { 'macos-x64' } else { 'macos-arm64' }
        $publish = Publish-Cli $rid
        $stage = New-Stage $rid (Join-Path $publish 'RetroAchievementsSharp.Cli')
        Move-Item (Join-Path $stage 'RetroAchievementsSharp.Cli') (Join-Path $stage 'RetroAchievementsSharp')
        $macStage = Join-Path $tempRoot "mac-$rid"
        $folder = "retroachievementssharp_v${Version}_${bundleArch}"
        New-Item -ItemType Directory -Force -Path (Join-Path $macStage $folder) | Out-Null
        Copy-Item (Join-Path $stage '*') (Join-Path $macStage $folder) -Recurse
        $tar = Join-Path $OutputRoot "$folder.tar.gz"
        & tar -C $macStage -czf $tar $folder
        if ($LASTEXITCODE -ne 0) { throw "tar failed for $rid" }
        Write-Host "Created $tar"
    }

    $nfpmPath = $null
    $nfpm = Get-Command nfpm -ErrorAction SilentlyContinue
    if ($nfpm) {
        $nfpmPath = $nfpm.Source
    }
    else {
        $go = Get-Command go -ErrorAction SilentlyContinue
        if ($go) {
            $goBin = (& $go.Source env GOPATH).Trim()
            $candidate = Join-Path $goBin 'bin/nfpm.exe'
            if (Test-Path $candidate) {
                $nfpmPath = $candidate
            }
        }
    }

    if ($nfpmPath) {
        foreach ($rid in @('linux-x64', 'linux-arm64')) {
            $arch = if ($rid -eq 'linux-x64') { 'amd64' } else { 'arm64' }
            $debArch = if ($rid -eq 'linux-x64') { 'amd64' } else { 'arm64' }
            $rpmArch = if ($rid -eq 'linux-x64') { 'x86_64' } else { 'aarch64' }
            $env:NFPM_ARCH = $arch
            $env:NFPM_VERSION = $Version
            $env:NFPM_BINARY = Join-Path $tempRoot "publish-$rid/RetroAchievementsSharp.Cli"
            $env:NFPM_LICENSE = Join-Path $repoRoot 'LICENSE'
            $env:NFPM_NOTICES = Join-Path $repoRoot 'THIRD-PARTY-NOTICES.md'
            $env:NFPM_README = Join-Path $repoRoot 'README.md'
            & $nfpmPath package --config (Join-Path $repoRoot 'packaging/linux/nfpm.yaml') --packager deb --target (Join-Path $OutputRoot "retroachievementssharp_v${Version}_${debArch}.deb")
            if ($LASTEXITCODE -ne 0) { throw "nfpm deb failed for $rid" }
            & $nfpmPath package --config (Join-Path $repoRoot 'packaging/linux/nfpm.yaml') --packager rpm --target (Join-Path $OutputRoot "retroachievementssharp_v${Version}-1.${rpmArch}.rpm")
            if ($LASTEXITCODE -ne 0) { throw "nfpm rpm failed for $rid" }
        }
    }
    else {
        Write-Warning 'nfpm was not found; .deb/.rpm bundles were skipped. Install nfpm (go install github.com/goreleaser/nfpm/v2/cmd/nfpm@latest) or use CI.'
    }

    # ---- NuGet library + CLI tool -----------------------------------------
    if (-not $SkipNuGet) {
        Write-Host 'Packing NuGet library ...'
        & dotnet pack (Join-Path $repoRoot 'RetroAchievementsSharp/RetroAchievementsSharp.csproj') -c Release -p:Version=$Version -o $OutputRoot | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'dotnet pack failed' }

        Write-Host 'Packing NuGet CLI tool ...'
        & dotnet pack (Join-Path $repoRoot 'RetroAchievementsSharp.Cli/RetroAchievementsSharp.Cli.csproj') -c Release -p:TargetFrameworks=net8.0 -p:Version=$Version -o $OutputRoot | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'dotnet pack (CLI tool) failed' }
    }

    Write-Host ''
    Write-Host "Bundles were written to: $OutputRoot"
    Write-Host 'DMGs and the arm64 Flatpak (optional) are built by CI on native runners.'
}
finally {
    # Only the temporary staging area is removed. The output folder is never
    # cleaned or deleted.
    if (Test-Path $tempRoot) {
        Remove-Item -Recurse -Force $tempRoot -ErrorAction SilentlyContinue
    }
}
