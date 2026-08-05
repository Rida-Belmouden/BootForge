using BootForge.Core.Enums;
using BootForge.Core.Interfaces;
using BootForge.Core.Models;

namespace BootForge.DeviceManagement.Services;

public sealed class DiskSafetyClassifier : IDiskSafetyClassifier
{
    private static readonly HashSet<string> InternalBusTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Ata",
            "Atapi",
            "Sata",
            "Nvme",
            "Scsi",
            "Sas",
            "Raid",
            "Spaces",
            "Scm",
            "Ufs"
        };

    public DiskSafetyAssessment Classify(
        PhysicalDisk disk,
        int? systemDiskNumber,
        int? bootDiskNumber)
    {
        bool isSystemDisk =
            systemDiskNumber.HasValue &&
            disk.DiskNumber == systemDiskNumber.Value;

        bool isBootDisk =
            bootDiskNumber.HasValue &&
            disk.DiskNumber == bootDiskNumber.Value;

        if (isSystemDisk || isBootDisk)
        {
            return new DiskSafetyAssessment
            {
                Status = DiskSafetyStatus.SystemDisk,
                IsSelectable = false,
                IsSystemDisk = isSystemDisk,
                IsBootDisk = isBootDisk,
                Reason =
                    "This disk contains the running Windows installation."
            };
        }

        if (!systemDiskNumber.HasValue ||
            !bootDiskNumber.HasValue)
        {
            return new DiskSafetyAssessment
            {
                Status = DiskSafetyStatus.Unknown,
                IsSelectable = false,
                Reason =
                    "Windows disk information could not be resolved, so this disk cannot be selected safely."
            };
        }

        if (disk.SizeInBytes <= 0)
        {
            return new DiskSafetyAssessment
            {
                Status = DiskSafetyStatus.Unavailable,
                IsSelectable = false,
                Reason = "The disk capacity could not be determined."
            };
        }

        if (disk.BusType.Equals(
                "Usb",
                StringComparison.OrdinalIgnoreCase))
        {
            return new DiskSafetyAssessment
            {
                Status = DiskSafetyStatus.SafeUsbDevice,
                IsSelectable = true,
                Reason =
                    "The disk uses the USB bus and is not the Windows system disk."
            };
        }

        if (InternalBusTypes.Contains(disk.BusType))
        {
            return new DiskSafetyAssessment
            {
                Status = DiskSafetyStatus.InternalDisk,
                IsSelectable = false,
                Reason =
                    $"The disk uses the internal {disk.BusType} bus."
            };
        }

        return new DiskSafetyAssessment
        {
            Status = DiskSafetyStatus.UnsupportedBusType,
            IsSelectable = false,
            Reason =
                $"The bus type '{disk.BusType}' is not currently approved."
        };
    }
}
