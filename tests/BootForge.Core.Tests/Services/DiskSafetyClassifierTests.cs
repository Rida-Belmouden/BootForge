using BootForge.Core.Enums;
using BootForge.Core.Models;
using BootForge.DeviceManagement.Services;

namespace BootForge.Core.Tests.Services;

public sealed class DiskSafetyClassifierTests
{
    private readonly DiskSafetyClassifier _classifier = new();

    [Fact]
    public void Classify_SystemDisk_BlocksSelection()
    {
        PhysicalDisk disk = CreateDisk(
            diskNumber: 0,
            busType: "Nvme");

        DiskSafetyAssessment result =
            _classifier.Classify(
                disk,
                systemDiskNumber: 0,
                bootDiskNumber: 0);

        Assert.Equal(
            DiskSafetyStatus.SystemDisk,
            result.Status);

        Assert.False(result.IsSelectable);
        Assert.True(result.IsSystemDisk);
        Assert.True(result.IsBootDisk);
    }

    [Fact]
    public void Classify_UsbDisk_AllowsSelection()
    {
        PhysicalDisk disk = CreateDisk(
            diskNumber: 2,
            busType: "Usb");

        DiskSafetyAssessment result =
            _classifier.Classify(
                disk,
                systemDiskNumber: 0,
                bootDiskNumber: 0);

        Assert.Equal(
            DiskSafetyStatus.SafeUsbDevice,
            result.Status);

        Assert.True(result.IsSelectable);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(0, null)]
    [InlineData(null, null)]
    public void Classify_UnresolvedWindowsDisk_BlocksSelection(
        int? systemDiskNumber,
        int? bootDiskNumber)
    {
        PhysicalDisk disk = CreateDisk(
            diskNumber: 2,
            busType: "Usb");

        DiskSafetyAssessment result =
            _classifier.Classify(
                disk,
                systemDiskNumber,
                bootDiskNumber);

        Assert.Equal(
            DiskSafetyStatus.Unknown,
            result.Status);

        Assert.False(result.IsSelectable);
    }

    [Fact]
    public void Classify_UsbDiskWithUnknownCapacity_BlocksSelection()
    {
        PhysicalDisk disk = CreateDisk(
            diskNumber: 2,
            busType: "Usb",
            sizeInBytes: 0);

        DiskSafetyAssessment result =
            _classifier.Classify(
                disk,
                systemDiskNumber: 0,
                bootDiskNumber: 0);

        Assert.Equal(
            DiskSafetyStatus.Unavailable,
            result.Status);

        Assert.False(result.IsSelectable);
    }

    [Theory]
    [InlineData("Nvme")]
    [InlineData("Sata")]
    [InlineData("Ata")]
    [InlineData("Scsi")]
    public void Classify_InternalDisk_BlocksSelection(
        string busType)
    {
        PhysicalDisk disk = CreateDisk(
            diskNumber: 1,
            busType);

        DiskSafetyAssessment result =
            _classifier.Classify(
                disk,
                systemDiskNumber: 0,
                bootDiskNumber: 0);

        Assert.Equal(
            DiskSafetyStatus.InternalDisk,
            result.Status);

        Assert.False(result.IsSelectable);
    }

    [Fact]
    public void Classify_UnknownBus_BlocksSelection()
    {
        PhysicalDisk disk = CreateDisk(
            diskNumber: 4,
            busType: "Unknown");

        DiskSafetyAssessment result =
            _classifier.Classify(
                disk,
                systemDiskNumber: 0,
                bootDiskNumber: 0);

        Assert.Equal(
            DiskSafetyStatus.UnsupportedBusType,
            result.Status);

        Assert.False(result.IsSelectable);
    }

    private static PhysicalDisk CreateDisk(
        int diskNumber,
        string busType,
        long sizeInBytes =
            32L * 1024 * 1024 * 1024)
    {
        return new PhysicalDisk
        {
            DiskNumber = diskNumber,
            DevicePath =
                $@"\\.\PhysicalDrive{diskNumber}",
            BusType = busType,
            SizeInBytes = sizeInBytes
        };
    }
}
