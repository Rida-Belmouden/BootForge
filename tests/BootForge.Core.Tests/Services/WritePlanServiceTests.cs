using BootForge.Core.Enums;
using BootForge.Core.Interfaces;
using BootForge.Core.Models;
using BootForge.Infrastructure.Services;

namespace BootForge.Core.Tests.Services;

public sealed class WritePlanServiceTests
{
    private readonly DiskImageService _imageService = new();

    [Fact]
    public void Create_UnchangedSafeSelection_ReturnsFreshPlan()
    {
        string filePath = CreateTemporaryImage(2048);

        try
        {
            DiskImage image = _imageService.Load(filePath);
            PhysicalDisk disk = CreateDisk(sizeInBytes: 4096);

            WritePlanService service = CreateService(disk);

            WritePlan plan = service.Create(image, disk);

            Assert.Equal(image.FilePath, plan.Image.FilePath);
            Assert.Equal(disk, plan.TargetDisk);
            Assert.True(
                plan.CreatedAtUtc <= DateTimeOffset.UtcNow);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Create_ImageChangedAfterSelection_Throws()
    {
        string filePath = CreateTemporaryImage(2048);

        try
        {
            DiskImage image = _imageService.Load(filePath);
            File.WriteAllBytes(filePath, new byte[3072]);

            PhysicalDisk disk = CreateDisk(sizeInBytes: 4096);
            WritePlanService service = CreateService(disk);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () => service.Create(image, disk));

            Assert.Contains("image changed", exception.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Create_TargetIdentityChanged_Throws()
    {
        string filePath = CreateTemporaryImage(2048);

        try
        {
            DiskImage image = _imageService.Load(filePath);
            PhysicalDisk selectedDisk =
                CreateDisk(sizeInBytes: 4096);

            PhysicalDisk replacement =
                selectedDisk with
                {
                    Product = "Different device"
                };

            WritePlanService service =
                CreateService(replacement);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () => service.Create(
                        image,
                        selectedDisk));

            Assert.Contains(
                "target disk changed",
                exception.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Create_DiskBecomesUnsafe_Throws()
    {
        string filePath = CreateTemporaryImage(2048);

        try
        {
            DiskImage image = _imageService.Load(filePath);
            PhysicalDisk selectedDisk =
                CreateDisk(sizeInBytes: 4096);

            PhysicalDisk blockedDisk =
                selectedDisk with
                {
                    Safety = new DiskSafetyAssessment
                    {
                        Status = DiskSafetyStatus.SystemDisk,
                        IsSelectable = false,
                        IsSystemDisk = true,
                        Reason = "System disk"
                    }
                };

            WritePlanService service =
                CreateService(blockedDisk);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () => service.Create(
                        image,
                        selectedDisk));

            Assert.Contains(
                "blocked",
                exception.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Create_ImageLargerThanDisk_Throws()
    {
        string filePath = CreateTemporaryImage(4096);

        try
        {
            DiskImage image = _imageService.Load(filePath);
            PhysicalDisk disk = CreateDisk(sizeInBytes: 2048);
            WritePlanService service = CreateService(disk);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () => service.Create(image, disk));

            Assert.Contains(
                "larger than the target",
                exception.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private WritePlanService CreateService(
        params PhysicalDisk[] disks)
    {
        return new WritePlanService(
            _imageService,
            new StubPhysicalDiskService(disks));
    }

    private static PhysicalDisk CreateDisk(long sizeInBytes)
    {
        return new PhysicalDisk
        {
            DiskNumber = 2,
            DevicePath = @"\\.\PhysicalDrive2",
            Vendor = "Test",
            Product = "USB",
            SerialNumber = "123",
            BusType = "Usb",
            SizeInBytes = sizeInBytes,
            Safety = new DiskSafetyAssessment
            {
                Status = DiskSafetyStatus.SafeUsbDevice,
                IsSelectable = true,
                Reason = "Safe test disk"
            }
        };
    }

    private static string CreateTemporaryImage(int length)
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.img");

        File.WriteAllBytes(filePath, new byte[length]);

        return filePath;
    }

    private sealed class StubPhysicalDiskService(
        IReadOnlyList<PhysicalDisk> disks)
        : IPhysicalDiskService
    {
        public IReadOnlyList<PhysicalDisk> GetPhysicalDisks()
        {
            return disks;
        }
    }
}
