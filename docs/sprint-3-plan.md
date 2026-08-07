# Sprint 3: Alpha hardening

**Dates:** August 7–21, 2026  
**Milestone:** [Sprint 3 - Alpha hardening](https://github.com/Rida-Belmouden/BootForge/milestone/1)  
**Target:** `v0.1.0-alpha.2`

## Baseline

BootForge `v0.1.0-alpha.1` is published as a portable Windows x64 prerelease.
At planning time there are no external feedback reports. The first sprint
priority is therefore to make future reports reproducible and diagnosable
without collecting telemetry or exposing sensitive device information.

## Sprint goal

Make BootForge alpha failures understandable, safely reportable, and
repeatable, then complete a documented acceptance matrix before Alpha 2.

## Backlog

| Priority | Work item | Size | Outcome |
| --- | --- | --- | --- |
| Continuous | [#7 Alpha feedback tracker](https://github.com/Rida-Belmouden/BootForge/issues/7) | Ongoing | Central test results and linked defects |
| High | [#8 Diagnostics logging and export](https://github.com/Rida-Belmouden/BootForge/issues/8) | Large | Privacy-safe support bundle |
| High | [#9 Actionable error categories](https://github.com/Rida-Belmouden/BootForge/issues/9) | Medium | Stable messages and recovery guidance |
| Medium | [#10 Write completion summary](https://github.com/Rida-Belmouden/BootForge/issues/10) | Medium | Durable success, failure, and cancellation result |
| High | [#11 Alpha 2 acceptance matrix](https://github.com/Rida-Belmouden/BootForge/issues/11) | Medium | Recorded manual and automated release evidence |

## Suggested sequence

1. Define diagnostic events, redaction rules, and the support-bundle schema.
2. Add error categories so logs and user-facing messages share stable codes.
3. Add the completion summary using the same operation result model.
4. Execute the acceptance matrix and file blocking defects in the feedback
   tracker.
5. Publish `v0.1.0-alpha.2` only when the exit criteria are satisfied.

## Definition of done

- Changes are merged through a reviewed pull request.
- Release builds complete with no warnings.
- Automated tests cover new logic and failure paths.
- Logs and exported diagnostics contain no full serial numbers or personal
  paths.
- User-facing errors provide a safe next action.
- Documentation and the acceptance matrix reflect actual behavior.
- CI produces a verified portable package.

## Exit criteria for Alpha 2

- No open critical data-loss or system-disk safety defect.
- All high-priority Sprint 3 issues are closed or explicitly deferred.
- Windows 10 and Windows 11 acceptance cases are recorded.
- At least one BIOS, UEFI, hybrid ISO, MBR IMG, and GPT IMG case is recorded.
- Cancellation, verification failure, and safe eject cases are recorded.
- The release ZIP and SHA-256 checksum pass an independent download check.

## Not in scope

- ISO extraction mode
- Creating or converting MBR and GPT layouts
- FAT32 or NTFS formatting
- Automatic telemetry or background data collection
- Stable `v1.0.0` packaging or code signing
