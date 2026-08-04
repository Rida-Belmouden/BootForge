# BootForge

BootForge is a Windows application for creating bootable USB drives from ISO and IMG disk images.

## Project status

BootForge is currently under active development.

## Planned features

- Detect removable USB drives
- Display detailed device information
- Write ISO and IMG images
- Raw disk writing
- BIOS and UEFI support
- MBR and GPT partition schemes
- FAT32 and NTFS formatting
- Write verification
- Protection against accidental system disk erasure
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

docs/```

## Safety notice

BootForge performs low-level disk operations. Development versions must only be tested with disposable USB drives containing no important data.