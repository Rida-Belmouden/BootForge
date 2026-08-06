# Releasing BootForge

BootForge uses semantic versions and Git tags to create GitHub releases.
Development builds use `0.1.0-dev`. Continuous-integration packages use a
version such as `0.1.0-ci.42`.

## Release channels

- Preview: `v0.1.0-alpha.1`, `v0.1.0-beta.1`, or `v0.1.0-rc.1`
- Stable: `v0.1.0`

A tag containing a suffix is automatically marked as a pre-release.

## Before tagging

1. Merge the release changes into `main`.
2. Confirm that the CI workflow is green.
3. Test detection, writing, verification, and safe ejection with a disposable
   USB drive.
4. Confirm that the version follows the intended release channel.

## Create a release

Create and push an annotated tag from the latest `main` commit:

```powershell
git switch main
git pull --ff-only
git tag -a v0.1.0-alpha.1 -m "BootForge v0.1.0-alpha.1"
git push origin v0.1.0-alpha.1
```

The release workflow validates the tag, builds and tests the application, then
publishes:

- A self-contained portable Windows x64 ZIP package
- A SHA-256 checksum for that package
- Automatically generated GitHub release notes

Do not move or reuse a published tag. If a release needs corrections, create a
new patch or pre-release version.
