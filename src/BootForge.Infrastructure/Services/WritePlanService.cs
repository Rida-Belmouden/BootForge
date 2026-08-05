using BootForge.Core.Interfaces;
using BootForge.Core.Models;

namespace BootForge.Infrastructure.Services;

public sealed class WritePlanService : IWritePlanService
{
    private readonly IDiskImageService _diskImageService;
    private readonly IPhysicalDiskService _physicalDiskService;

    public WritePlanService(
        IDiskImageService diskImageService,
        IPhysicalDiskService physicalDiskService)
    {
        _diskImageService = diskImageService;
        _physicalDiskService = physicalDiskService;
    }

    public WritePlan Create(
        DiskImage selectedImage,
        PhysicalDisk selectedDisk)
    {
        ArgumentNullException.ThrowIfNull(selectedImage);
        ArgumentNullException.ThrowIfNull(selectedDisk);

        DiskImage currentImage =
            _diskImageService.Load(selectedImage.FilePath);

        if (currentImage.SizeInBytes != selectedImage.SizeInBytes ||
            currentImage.LastModifiedUtc !=
            selectedImage.LastModifiedUtc)
        {
            throw new InvalidOperationException(
                "The selected image changed after it was loaded.");
        }

        PhysicalDisk? currentDisk =
            _physicalDiskService
                .GetPhysicalDisks()
                .SingleOrDefault(
                    disk =>
                        disk.DiskNumber ==
                        selectedDisk.DiskNumber);

        if (currentDisk is null)
        {
            throw new InvalidOperationException(
                "The selected target disk is no longer available.");
        }

        if (!HasSameIdentity(selectedDisk, currentDisk))
        {
            throw new InvalidOperationException(
                "The target disk changed after it was selected.");
        }

        if (!currentDisk.IsSelectable)
        {
            throw new InvalidOperationException(
                "The target disk is blocked by the safety policy.");
        }

        if (!currentImage.FitsOn(currentDisk))
        {
            throw new InvalidOperationException(
                "The image is larger than the target disk.");
        }

        return new WritePlan
        {
            Image = currentImage,
            TargetDisk = currentDisk,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static bool HasSameIdentity(
        PhysicalDisk selected,
        PhysicalDisk current)
    {
        return selected.DiskNumber == current.DiskNumber &&
               selected.DevicePath == current.DevicePath &&
               selected.SizeInBytes == current.SizeInBytes &&
               selected.Vendor == current.Vendor &&
               selected.Product == current.Product &&
               selected.SerialNumber == current.SerialNumber &&
               selected.BusType == current.BusType;
    }
}
