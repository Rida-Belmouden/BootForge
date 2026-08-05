using BootForge.Core.Enums;
using BootForge.Core.Interfaces;
using BootForge.Core.Models;
using BootForge.Infrastructure.Services;

namespace BootForge.Core.Tests.Services;

public sealed class WriteOperationServiceTests
{
    [Fact]
    public async Task WriteAsync_LocksRevalidatesAndCopiesImage()
    {
        byte[] content = Enumerable
            .Range(0, 4096)
            .Select(index => (byte)(index % 251))
            .ToArray();

        string filePath = CreateTemporaryImage(content);

        try
        {
            WritePlan plan = CreatePlan(filePath, content.Length);
            TrackingVolumeLock volumeLock = new();
            StubVolumeLockService lockService =
                new(volumeLock);

            StubWritePlanService planService = new(
                () =>
                {
                    Assert.True(volumeLock.IsAcquired);
                    Assert.False(volumeLock.IsDisposed);
                    return plan;
                });

            TrackingMemoryStream destination = new();
            StubRawDiskStreamFactory streamFactory =
                new(destination);

            WriteOperationService service = new(
                lockService,
                planService,
                streamFactory,
                new ImageWriter(bufferSize: 512));

            await service.WriteAsync(plan);

            Assert.Equal(content, destination.ToArray());
            Assert.True(destination.IsDisposed);
            Assert.True(volumeLock.IsDisposed);
            Assert.Equal(1, planService.CallCount);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task WriteAsync_RevalidationFailureNeverOpensRawDisk()
    {
        string filePath =
            CreateTemporaryImage(new byte[1024]);

        try
        {
            WritePlan plan = CreatePlan(filePath, 1024);
            TrackingVolumeLock volumeLock = new();

            StubWritePlanService planService = new(
                () => throw new InvalidOperationException(
                    "Target changed."));

            StubRawDiskStreamFactory streamFactory =
                new(new TrackingMemoryStream());

            WriteOperationService service = new(
                new StubVolumeLockService(volumeLock),
                planService,
                streamFactory,
                new ImageWriter());

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.WriteAsync(plan));

            Assert.False(streamFactory.WasOpened);
            Assert.True(volumeLock.IsDisposed);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static WritePlan CreatePlan(
        string filePath,
        long imageSize)
    {
        DiskImage image = new()
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            Format = "IMG",
            SizeInBytes = imageSize,
            LastModifiedUtc = File.GetLastWriteTimeUtc(filePath)
        };

        PhysicalDisk disk = new()
        {
            DiskNumber = 2,
            DevicePath = @"\\.\PhysicalDrive2",
            Vendor = "Test",
            Product = "USB",
            SerialNumber = "123",
            BusType = "Usb",
            SizeInBytes = imageSize * 2,
            Safety = new DiskSafetyAssessment
            {
                Status = DiskSafetyStatus.SafeUsbDevice,
                IsSelectable = true,
                Reason = "Safe test disk"
            }
        };

        return new WritePlan
        {
            Image = image,
            TargetDisk = disk,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static string CreateTemporaryImage(
        byte[] content)
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.img");

        File.WriteAllBytes(filePath, content);

        return filePath;
    }

    private sealed class StubVolumeLockService(
        TrackingVolumeLock volumeLock)
        : IVolumeLockService
    {
        public IVolumeLock AcquireForDisk(int diskNumber)
        {
            volumeLock.IsAcquired = true;
            return volumeLock;
        }
    }

    private sealed class TrackingVolumeLock : IVolumeLock
    {
        public bool IsAcquired { get; set; }

        public bool IsDisposed { get; private set; }

        public int VolumeCount => 1;

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class StubWritePlanService(
        Func<WritePlan> create)
        : IWritePlanService
    {
        public int CallCount { get; private set; }

        public WritePlan Create(
            DiskImage selectedImage,
            PhysicalDisk selectedDisk)
        {
            CallCount++;
            return create();
        }
    }

    private sealed class StubRawDiskStreamFactory(
        TrackingMemoryStream destination)
        : IRawDiskStreamFactory
    {
        public bool WasOpened { get; private set; }

        public Stream OpenWrite(PhysicalDisk disk)
        {
            WasOpened = true;
            return destination;
        }
    }

    private sealed class TrackingMemoryStream : MemoryStream
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
