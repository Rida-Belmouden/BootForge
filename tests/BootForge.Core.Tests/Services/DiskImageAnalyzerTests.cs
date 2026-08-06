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
        "EFI PART"u8.CopyTo(image.AsSpan(512, 8));

        string filePath = WriteTemporaryFile(image, ".img");

        try
        {
            DiskImageAnalysis result =
                _analyzer.Analyze(filePath, "IMG");

            Assert.True(result.IsBootable);
            Assert.Equal(
                BootFirmwareSupport.Uefi,
                result.FirmwareSupport);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateBootableIso(
        bool includeUefiSection)
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
