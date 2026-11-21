# Security Policy

## Supported versions

| Version | Supported |
|---|---|
| `master` / latest release | ✅ |
| Older releases | Best effort only; please reproduce on the latest release |

## Reporting a vulnerability

**Do not open a public GitHub issue for security problems.**

Use GitHub's private vulnerability reporting:

- https://github.com/purelogiccode/RetroAchievementsSharp/security/advisories/new
- Or open the repository's **Security** tab and choose **Report a vulnerability**.

Please include:

- A description of the issue and its impact.
- Steps to reproduce (minimal file / command if possible).
- Affected version, OS, architecture, and .NET runtime.
- Whether the issue requires a malicious game file, archive, CHD, `.cue`, or
  other untrusted input.
- Any suggested fix or mitigation.

Do **not** attach copyrighted ROMs, CHDs, 3DS keys, `aes_keys.txt`,
`seeddb.bin`, API keys, or personal data. A minimal synthetic reproducer or a
hash/header excerpt is enough.

## What to expect

This is a volunteer-maintained project. We aim to:

- Acknowledge a report within **3 business days**.
- Provide an initial assessment within **10 business days**.
- Coordinate disclosure and credit with the reporter.

Please give us a reasonable opportunity to fix and release before public
disclosure. We will credit reporters who want credit.

## Scope

Security-relevant areas include:

- The hashing engine and CLI when processing untrusted files, archives,
  playlists, cue sheets, CHDs, RVZ/WIA images, and 3DS content.
- Memory-safety / denial-of-service issues in parsers and decompression paths.
- Command injection, path traversal, unsafe temporary-file handling, and
  symlink/reparse-point issues.
- Credential/API-key exposure in logs, bug reports, telemetry, or build output.
- Supply-chain issues in the build/release workflow.

Out of scope:

- Issues in upstream dependencies that should be reported upstream
  (rcheevos, CHDSharp, RVZSharp, VideoGameFileSystemParser, Serilog). We will
  still coordinate and update our pins.
- Emulator/game compatibility bugs, incorrect hashes caused by a corrupt or
  modified game image, or unsupported formats.
- Social engineering and physical attacks.

## Security features enabled

The GitHub repository uses private vulnerability reporting, Dependabot alerts
and security updates, secret scanning with push protection, and CodeQL code
scanning. CI builds with warnings treated as errors and publishes release
artifacts only from tagged/manual workflow runs.
