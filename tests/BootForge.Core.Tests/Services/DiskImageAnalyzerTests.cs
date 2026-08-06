using System.Buffers.Binary;
using BootForge.Core.Enums;
using BootForge.Core.Models;
using BootForge.Infrastructure.Services;

namespace BootForge.Core.Tests.Services;

public sealed class DiskImageAnalyzerTests
{
    private const int SectorSize = 2048;

    private readonly DiskImageAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_BiosAndUefiIso_DetectsBothFirmwareTypes()
    {
        string filePath = CreateBootableIso(
            includeUefiSection: true);

        try
        {
            DiskImageAnalysis result =
                _analyzer.Analyze(filePath, "ISO");

            Assert.True(result.IsRecognized);
            Assert.True(result.IsBootable);
            Assert.Equal(
                BootFirmwareSupport.Bios |
                BootFirmwareSupport.Uefi,
                result.FirmwareSupport);
            Assert.Equal(
                DiskImageKind.Iso9660,
                result.ImageKind);
            Assert.Equal(
                DiskPartitionScheme.None,
                result.PartitionScheme);
            Assert.False(result.IsHybridImage);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Analyze_HybridIso_DetectsEmbeddedMbrLayout()
    {
        string filePath = CreateBootableIso(
            includeUefiSection: true,
            includeHybridMbr: true);

        try
        {
            DiskImageAnalysis result =
                _analyzer.Analyze(filePath, "ISO");

            Assert.True(result.IsBootable);
            Assert.True(result.IsHybridImage);
            Assert.Equal(
                DiskImageKind.Iso9660,
                result.ImageKind);
            Assert.Equal(
                DiskPartitionScheme.Mbr,
                result.PartitionScheme);
            Assert.Contains(
                "Hybrid ISO",
                result.Description,
                StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Analyze_NonBootableIso_IsRecognizedButBlocked()
    {
        byte[] image = new byte[24 * SectorSize];
        WriteVolumeDescriptor(
            image,
            sector: 16,
            type: 1);

        WriteVolumeDescriptor(
            image,
            sector: 17,
            type: 255);

        string filePath = WriteTemporaryFile(image, ".iso");

        try
        {
            DiskImageAnalysis result =
                _analyzer.Analyze(filePath, "ISO");

            Assert.True(result.IsRecognized);
            Assert.False(result.IsBootable);
            Assert.Equal(
                BootFirmwareSupport.None,
                result.FirmwareSupport);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Analyze_MbrImage_DetectsBiosSupport()
    {
        byte[] image = new byte[4096];
        image[510] = 0x55;
        image[511] = 0xAA;
        image[446 + 4] = 0x0C;

        string filePath = WriteTemporaryFile(image, ".img");

        try
        {
            DiskImageAnalysis result =
                _analyzer.Analyze(filePath, "IMG");

            Assert.True(result.IsBootable);
            Assert.Equal(
                BootFirmwareSupport.Bios,
                result.FirmwareSupport);
            Assert.Equal(
                DiskImageKind.RawDisk,
                result.ImageKind);
            Assert.Equal(
                DiskPartitionScheme.Mbr,
                result.PartitionScheme);
            Assert.False(result.IsHybridImage);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Analyze_GptImage_DetectsUefiSupport()
    {
        byte[] image = new byte[4096];
        WriteGptLayout(
            image,
            includeEfiSystemPartition: true);

        string filePath = WriteTemporaryFile(image, ".img");

        try
        {
            DiskImageAnalysis result =
                _analyzer.Analyze(filePath, "IMG");

            Assert.True(result.IsBootable);
            Assert.Equal(
                BootFirmwareSupport.Uefi,
                result.FirmwareSupport);
            Assert.Equal(
                DiskImageKind.RawDisk,
                result.ImageKind);
            Assert.Equal(
                DiskPartitionScheme.Gpt,
                result.PartitionScheme);
            Assert.False(result.IsHybridImage);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Analyze_GptWithoutEfiPartition_IsBlocked()
    {
        byte[] image = new byte[4096];
        WriteGptLayout(
            image,
            includeEfiSystemPartition: false);

        string filePath = WriteTemporaryFile(image, ".img");

        try
        {
            DiskImageAnalysis result =
                _analyzer.Analyze(filePath, "IMG");

            Assert.True(result.IsRecognized);
            Assert.False(result.IsBootable);
            Assert.Equal(
                BootFirmwareSupport.None,
                result.FirmwareSupport);
            Assert.Equal(
                DiskImageKind.RawDisk,
                result.ImageKind);
            Assert.Equal(
                DiskPartitionScheme.Gpt,
                result.PartitionScheme);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Analyze_GptWithCorruptHeader_IsNotRecognized()
    {
        byte[] image = new byte[4096];
        WriteGptLayout(
            image,
            includeEfiSystemPartition: true);
        image[512 + 40] ^= 0x01;

        string filePath = WriteTemporaryFile(image, ".img");

        try
        {
            DiskImageAnalysis result =
                _analyzer.Analyze(filePath, "IMG");

            Assert.False(result.IsRecognized);
            Assert.False(result.IsBootable);
            Assert.Equal(
                DiskPartitionScheme.None,
                result.PartitionScheme);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateBootableIso(
        bool includeUefiSection,
        bool includeHybridMbr = false)
    {
        byte[] image = new byte[32 * SectorSize];

        WriteVolumeDescriptor(
            image,
            sector: 16,
            type: 1);

        WriteVolumeDescriptor(
            image,
            sector: 17,
            type: 0);

        int bootRecordOffset = 17 * SectorSize;
        "EL TORITO SPECIFICATION"u8.CopyTo(
            image.AsSpan(bootRecordOffset + 7));

        BinaryPrimitives.WriteUInt32LittleEndian(
            image.AsSpan(bootRecordOffset + 71, 4),
            20);

        WriteVolumeDescriptor(
            image,
            sector: 18,
            type: 255);

        int catalogOffset = 20 * SectorSize;
        image[catalogOffset] = 0x01;
        image[catalogOffset + 1] = 0x00;
        image[catalogOffset + 30] = 0x55;
        image[catalogOffset + 31] = 0xAA;
        image[catalogOffset + 32] = 0x88;

        ushort checksum = CalculateValidationChecksum(
            image.AsSpan(catalogOffset, 32));

        BinaryPrimitives.WriteUInt16LittleEndian(
            image.AsSpan(catalogOffset + 28, 2),
            checksum);

        if (includeUefiSection)
        {
            image[catalogOffset + 64] = 0x91;
            image[catalogOffset + 65] = 0xEF;

            BinaryPrimitives.WriteUInt16LittleEndian(
                image.AsSpan(catalogOffset + 66, 2),
                1);

            image[catalogOffset + 96] = 0x88;
        }

        if (includeHybridMbr)
        {
            image[510] = 0x55;
            image[511] = 0xAA;
            image[446 + 4] = 0x17;
        }

        return WriteTemporaryFile(image, ".iso");
    }

    private static void WriteVolumeDescriptor(
        byte[] image,
        int sector,
        byte type)
    {
        int offset = sector * SectorSize;
        image[offset] = type;
        "CD001"u8.CopyTo(image.AsSpan(offset + 1, 5));
        image[offset + 6] = 1;
    }

    private static void WriteGptLayout(
        byte[] image,
        bool includeEfiSystemPartition)
    {
        const int headerOffset = 512;
        const int partitionEntriesOffset = 1024;

        image[510] = 0x55;
        image[511] = 0xAA;
        image[446 + 4] = 0xEE;

        "EFI PART"u8.CopyTo(
            image.AsSpan(headerOffset, 8));
        BinaryPrimitives.WriteUInt32LittleEndian(
            image.AsSpan(headerOffset + 8, 4),
            0x00010000);
        BinaryPrimitives.WriteUInt32LittleEndian(
            image.AsSpan(headerOffset + 12, 4),
            92);
        BinaryPrimitives.WriteUInt64LittleEndian(
            image.AsSpan(headerOffset + 24, 8),
            1);
        BinaryPrimitives.WriteUInt64LittleEndian(
            image.AsSpan(headerOffset + 72, 8),
            2);
        BinaryPrimitives.WriteUInt32LittleEndian(
            image.AsSpan(headerOffset + 80, 4),
            1);
        BinaryPrimitives.WriteUInt32LittleEndian(
            image.AsSpan(headerOffset + 84, 4),
            128);

        if (includeEfiSystemPartition)
        {
            byte[] efiSystemPartitionTypeGuid =
            [
                0x28, 0x73, 0x2A, 0xC1,
                0x1F, 0xF8,
                0xD2, 0x11,
                0xBA, 0x4B,
                0x00, 0xA0, 0xC9, 0x3E, 0xC9, 0x3B
            ];

            efiSystemPartitionTypeGuid.CopyTo(
                image,
                partitionEntriesOffset);
        }

        uint headerCrc = CalculateCrc32(
            image.AsSpan(headerOffset, 92));
        BinaryPrimitives.WriteUInt32LittleEndian(
            image.AsSpan(headerOffset + 16, 4),
            headerCrc);
    }

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

    private static ushort CalculateValidationChecksum(
        ReadOnlySpan<byte> validationEntry)
    {
        ushort sum = 0;

        for (int offset = 0; offset < 32; offset += 2)
        {
            sum = unchecked(
                (ushort)(sum +
                    BinaryPrimitives.ReadUInt16LittleEndian(
                        validationEntry.Slice(offset, 2))));
        }

        return unchecked((ushort)(0 - sum));
    }

    private static string WriteTemporaryFile(
        byte[] content,
        string extension)
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}{extension}");

        File.WriteAllBytes(filePath, content);
        return filePath;
    }
}
