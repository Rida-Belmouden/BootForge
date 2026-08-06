using System.Buffers.Binary;
using System.Text;
using BootForge.Core.Enums;
using BootForge.Core.Interfaces;
using BootForge.Core.Models;

namespace BootForge.Infrastructure.Services;

public sealed class DiskImageAnalyzer : IDiskImageAnalyzer
{
    private const int IsoSectorSize = 2048;
    private const int FirstVolumeDescriptorSector = 16;
    private const int MaximumVolumeDescriptorCount = 48;
    private const int BootCatalogEntrySize = 32;

    public DiskImageAnalysis Analyze(
        string filePath,
        string format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        using FileStream stream = File.Open(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        return format.Equals(
                "ISO",
                StringComparison.OrdinalIgnoreCase)
            ? AnalyzeIso(stream)
            : AnalyzeRawImage(stream);
    }

    private static DiskImageAnalysis AnalyzeIso(
        FileStream stream)
    {
        bool hasPrimaryDescriptor = false;
        uint? bootCatalogSector = null;
        byte[] descriptor = new byte[IsoSectorSize];

        for (int index = 0;
             index < MaximumVolumeDescriptorCount;
             index++)
        {
            long offset = checked(
                (FirstVolumeDescriptorSector + index) *
                (long)IsoSectorSize);

            if (!ReadAt(stream, offset, descriptor))
            {
                break;
            }

            if (!descriptor.AsSpan(1, 5)
                    .SequenceEqual("CD001"u8))
            {
                continue;
            }

            byte descriptorType = descriptor[0];
            hasPrimaryDescriptor |= descriptorType == 1;

            if (descriptorType == 0 &&
                IsElToritoBootRecord(descriptor))
            {
                bootCatalogSector =
                    BinaryPrimitives.ReadUInt32LittleEndian(
                        descriptor.AsSpan(71, 4));
            }

            if (descriptorType == 255)
            {
                break;
            }
        }

        if (!hasPrimaryDescriptor)
        {
            return DiskImageAnalysis.Unknown;
        }

        BootFirmwareSupport support =
            bootCatalogSector.HasValue
                ? ReadBootCatalog(
                    stream,
                    bootCatalogSector.Value)
                : BootFirmwareSupport.None;

        return CreateAnalysis(
            isRecognized: true,
            support,
            "ISO 9660");
    }

    private static DiskImageAnalysis AnalyzeRawImage(
        FileStream stream)
    {
        byte[] sectors = new byte[1024];

        if (!ReadAt(stream, 0, sectors))
        {
            return DiskImageAnalysis.Unknown;
        }

        bool hasMbrSignature =
            sectors[510] == 0x55 &&
            sectors[511] == 0xAA;

        bool hasMbrPartition = false;

        for (int index = 0; index < 4; index++)
        {
            int partitionOffset = 446 + index * 16;
            hasMbrPartition |=
                sectors[partitionOffset + 4] != 0;
        }

        bool hasGpt =
            sectors.AsSpan(512, 8)
                .SequenceEqual("EFI PART"u8);

        BootFirmwareSupport support =
            BootFirmwareSupport.None;

        if (hasMbrSignature && hasMbrPartition)
        {
            support |= BootFirmwareSupport.Bios;
        }

        if (hasGpt)
        {
            support |= BootFirmwareSupport.Uefi;
        }

        return support == BootFirmwareSupport.None
            ? DiskImageAnalysis.Unknown
            : CreateAnalysis(
                isRecognized: true,
                support,
                hasGpt ? "GPT disk image" : "MBR disk image");
    }

    private static BootFirmwareSupport ReadBootCatalog(
        FileStream stream,
        uint catalogSector)
    {
        byte[] catalog = new byte[IsoSectorSize];
        long offset = checked(
            catalogSector * (long)IsoSectorSize);

        if (!ReadAt(stream, offset, catalog) ||
            !IsValidCatalog(catalog))
        {
            return BootFirmwareSupport.None;
        }

        BootFirmwareSupport support =
            BootFirmwareSupport.None;

        if (catalog[BootCatalogEntrySize] == 0x88)
        {
            support |= PlatformToFirmware(catalog[1]);
        }

        int entryOffset = BootCatalogEntrySize * 2;

        while (entryOffset + BootCatalogEntrySize <=
               catalog.Length)
        {
            byte header = catalog[entryOffset];

            if (header is not (0x90 or 0x91))
            {
                entryOffset += BootCatalogEntrySize;
                continue;
            }

            byte platform = catalog[entryOffset + 1];
            ushort entryCount =
                BinaryPrimitives.ReadUInt16LittleEndian(
                    catalog.AsSpan(entryOffset + 2, 2));

            for (int index = 0; index < entryCount; index++)
            {
                int bootEntryOffset = checked(
                    entryOffset +
                    (index + 1) * BootCatalogEntrySize);

                if (bootEntryOffset >= catalog.Length)
                {
                    break;
                }

                if (catalog[bootEntryOffset] == 0x88)
                {
                    support |= PlatformToFirmware(platform);
                }
            }

            entryOffset = checked(
                entryOffset +
                (entryCount + 1) * BootCatalogEntrySize);

            if (header == 0x91)
            {
                break;
            }
        }

        return support;
    }

    private static bool IsElToritoBootRecord(
        byte[] descriptor)
    {
        string identifier = Encoding.ASCII
            .GetString(descriptor, 7, 32)
            .TrimEnd('\0', ' ');

        return identifier.Equals(
            "EL TORITO SPECIFICATION",
            StringComparison.Ordinal);
    }

    private static bool IsValidCatalog(byte[] catalog)
    {
        if (catalog[0] != 0x01 ||
            catalog[30] != 0x55 ||
            catalog[31] != 0xAA)
        {
            return false;
        }

        ushort checksum = 0;

        for (int offset = 0;
             offset < BootCatalogEntrySize;
             offset += sizeof(ushort))
        {
            checksum = unchecked(
                (ushort)(checksum +
                    BinaryPrimitives.ReadUInt16LittleEndian(
                        catalog.AsSpan(offset, 2))));
        }

        return checksum == 0;
    }

    private static BootFirmwareSupport PlatformToFirmware(
        byte platform)
    {
        return platform switch
        {
            0x00 => BootFirmwareSupport.Bios,
            0xEF => BootFirmwareSupport.Uefi,
            _ => BootFirmwareSupport.None
        };
    }

    private static DiskImageAnalysis CreateAnalysis(
        bool isRecognized,
        BootFirmwareSupport support,
        string imageType)
    {
        string firmware = support switch
        {
            BootFirmwareSupport.Bios => "BIOS",
            BootFirmwareSupport.Uefi => "UEFI",
            BootFirmwareSupport.Bios |
                BootFirmwareSupport.Uefi => "BIOS + UEFI",
            _ => "not bootable"
        };

        return new DiskImageAnalysis
        {
            IsRecognized = isRecognized,
            IsBootable = support != BootFirmwareSupport.None,
            FirmwareSupport = support,
            Description = $"{imageType} · {firmware}"
        };
    }

    private static bool ReadAt(
        FileStream stream,
        long offset,
        byte[] buffer)
    {
        if (offset < 0 ||
            offset + buffer.Length > stream.Length)
        {
            return false;
        }

        stream.Position = offset;
        stream.ReadExactly(buffer);
        return true;
    }
}
