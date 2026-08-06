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
        PartitionLayout partitionLayout =
            ReadPartitionLayout(stream);
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
            DiskImageKind.Iso9660,
            partitionLayout.Scheme,
            isHybridImage:
                partitionLayout.Scheme !=
                DiskPartitionScheme.None,
            partitionLayout.Scheme ==
                DiskPartitionScheme.None
                ? "ISO 9660"
                : "Hybrid ISO 9660");
    }

    private static DiskImageAnalysis AnalyzeRawImage(
        FileStream stream)
    {
        PartitionLayout partitionLayout =
            ReadPartitionLayout(stream);

        BootFirmwareSupport support =
            BootFirmwareSupport.None;

        if (partitionLayout.HasLegacyMbrPartition)
        {
            support |= BootFirmwareSupport.Bios;
        }

        if (partitionLayout.HasEfiSystemPartition)
        {
            support |= BootFirmwareSupport.Uefi;
        }

        return partitionLayout.Scheme ==
            DiskPartitionScheme.None
            ? DiskImageAnalysis.Unknown
            : CreateAnalysis(
                isRecognized: true,
                support,
                DiskImageKind.RawDisk,
                partitionLayout.Scheme,
                isHybridImage: false,
                partitionLayout.Scheme ==
                    DiskPartitionScheme.Gpt
                    ? "GPT disk image"
                    : "MBR disk image");
    }

    private static PartitionLayout ReadPartitionLayout(
        FileStream stream)
    {
        byte[] sectors = new byte[1024];

        if (!ReadAt(stream, 0, sectors))
        {
            return PartitionLayout.None;
        }

        bool hasMbrSignature =
            sectors[510] == 0x55 &&
            sectors[511] == 0xAA;
        bool hasLegacyMbrPartition = false;

        if (hasMbrSignature)
        {
            for (int index = 0; index < 4; index++)
            {
                int partitionOffset = 446 + index * 16;
                byte partitionType =
                    sectors[partitionOffset + 4];

                hasLegacyMbrPartition |=
                    partitionType is not (0 or 0xEE);
            }
        }

        bool hasGpt = TryReadGptHeader(
            sectors,
            out ulong partitionEntriesLba,
            out uint partitionEntryCount,
            out uint partitionEntrySize);
        bool hasEfiSystemPartition =
            hasGpt &&
            ContainsEfiSystemPartition(
                stream,
                partitionEntriesLba,
                partitionEntryCount,
                partitionEntrySize);

        DiskPartitionScheme scheme = hasGpt
            ? DiskPartitionScheme.Gpt
            : hasLegacyMbrPartition
                ? DiskPartitionScheme.Mbr
                : DiskPartitionScheme.None;

        return new PartitionLayout(
            scheme,
            hasLegacyMbrPartition,
            hasEfiSystemPartition);
    }

    private static bool TryReadGptHeader(
        byte[] sectors,
        out ulong partitionEntriesLba,
        out uint partitionEntryCount,
        out uint partitionEntrySize)
    {
        const int headerOffset = 512;
        ReadOnlySpan<byte> header =
            sectors.AsSpan(headerOffset, 512);

        partitionEntriesLba =
            BinaryPrimitives.ReadUInt64LittleEndian(
                header.Slice(72, 8));
        partitionEntryCount =
            BinaryPrimitives.ReadUInt32LittleEndian(
                header.Slice(80, 4));
        partitionEntrySize =
            BinaryPrimitives.ReadUInt32LittleEndian(
                header.Slice(84, 4));

        uint revision =
            BinaryPrimitives.ReadUInt32LittleEndian(
                header.Slice(8, 4));
        uint headerSize =
            BinaryPrimitives.ReadUInt32LittleEndian(
                header.Slice(12, 4));
        uint storedHeaderCrc =
            BinaryPrimitives.ReadUInt32LittleEndian(
                header.Slice(16, 4));
        ulong currentLba =
            BinaryPrimitives.ReadUInt64LittleEndian(
                header.Slice(24, 8));

        bool fieldsAreValid =
            header[..8].SequenceEqual("EFI PART"u8) &&
            revision >> 16 == 1 &&
            headerSize is >= 92 and <= 512 &&
            currentLba == 1 &&
            partitionEntriesLba >= 2 &&
            partitionEntryCount > 0 &&
            partitionEntrySize is >= 128 and <= 4096 &&
            partitionEntrySize % 8 == 0;

        if (!fieldsAreValid)
        {
            return false;
        }

        byte[] headerForCrc =
            header[..checked((int)headerSize)].ToArray();
        headerForCrc.AsSpan(16, 4).Clear();

        return CalculateCrc32(headerForCrc) ==
            storedHeaderCrc;
    }

    private static bool ContainsEfiSystemPartition(
        FileStream stream,
        ulong partitionEntriesLba,
        uint partitionEntryCount,
        uint partitionEntrySize)
    {
        const uint maximumEntriesToInspect = 1024;
        uint entriesToInspect = Math.Min(
            partitionEntryCount,
            maximumEntriesToInspect);
        byte[] entry = new byte[partitionEntrySize];

        for (uint index = 0;
             index < entriesToInspect;
             index++)
        {
            long offset;

            try
            {
                offset = checked(
                    (long)partitionEntriesLba * 512 +
                    (long)index * partitionEntrySize);
            }
            catch (OverflowException)
            {
                return false;
            }

            if (!ReadAt(stream, offset, entry))
            {
                return false;
            }

            if (entry.AsSpan(0, 16)
                .SequenceEqual(
                    EfiSystemPartitionTypeGuid))
            {
                return true;
            }
        }

        return false;
    }

    private static ReadOnlySpan<byte>
        EfiSystemPartitionTypeGuid =>
        [
            0x28, 0x73, 0x2A, 0xC1,
            0x1F, 0xF8,
            0xD2, 0x11,
            0xBA, 0x4B,
            0x00, 0xA0, 0xC9, 0x3E, 0xC9, 0x3B
        ];

    private static uint CalculateCrc32(
        ReadOnlySpan<byte> content)
    {
        const uint polynomial = 0xEDB88320;
        uint crc = uint.MaxValue;

        foreach (byte value in content)
        {
            crc ^= value;

            for (int bit = 0; bit < 8; bit++)
            {
                uint mask = unchecked(
                    (uint)-(int)(crc & 1));
                crc = (crc >> 1) ^
                    (polynomial & mask);
            }
        }

        return ~crc;
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
        DiskImageKind imageKind,
        DiskPartitionScheme partitionScheme,
        bool isHybridImage,
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
            ImageKind = imageKind,
            PartitionScheme = partitionScheme,
            IsHybridImage = isHybridImage,
            Description = $"{imageType} · {firmware}"
        };
    }

    private readonly record struct PartitionLayout(
        DiskPartitionScheme Scheme,
        bool HasLegacyMbrPartition,
        bool HasEfiSystemPartition)
    {
        public static PartitionLayout None { get; } =
            new(
                DiskPartitionScheme.None,
                HasLegacyMbrPartition: false,
                HasEfiSystemPartition: false);
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
