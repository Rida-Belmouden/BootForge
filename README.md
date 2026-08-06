# BootForge

BootForge is a Windows application for creating bootable USB drives from ISO and IMG disk images.

## Project status

BootForge is currently under active development.

Every pull request is built and tested on Windows. Tagged versions are
published as portable Windows releases with a SHA-256 checksum.

See [the release guide](docs/releasing.md) for the versioning and release
process.

## Current capabilities

- Detect physical disks and block unsafe targets
- Analyze ISO and IMG boot structures
- Detect BIOS, UEFI, hybrid ISO, MBR, and GPT compatibility
- Write images in raw mode and verify the result byte by byte
- Safely eject a completed target
- Build and package the application with GitHub Actions

See [image compatibility](docs/image-compatibility.md) for detection details
and current limitations.

## Planned features

- ISO extraction mode
- Configurable MBR and GPT partition schemes
- FAT32 and NTFS formatting
- Multilingual user interface

## Technology stack

- C#
- .NET 10
- WPF
- MVVM
- Windows API
- xUnit

## Repository structure

```text
src/
  BootForge.App/
  BootForge.Core/
  BootForge.DeviceManagement/
  BootForge.Infrastructure/

tests/
  BootForge.Core.Tests/

docs/
```

## Building and testing

BootForge requires the .NET 10 SDK on Windows.

```powershell
dotnet restore BootForge.slnx
dotnet build BootForge.slnx --configuration Release --no-restore
dotnet test tests/BootForge.Core.Tests/BootForge.Core.Tests.csproj --configuration Release --no-build
```

To produce the portable Windows package:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/package.ps1 -Version 0.1.0-dev
```

The ZIP package and its SHA-256 checksum are written to `artifacts/`.

## Safety notice

BootForge performs low-level disk operations. Development versions must only be tested with disposable USB drives containing no important data.
