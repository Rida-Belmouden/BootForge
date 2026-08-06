using BootForge.Core.Enums;
using BootForge.Core.Models;
using BootForge.DeviceManagement.Services;

namespace BootForge.Core.Tests.Services;

public sealed class DeviceEjectServiceTests
{
    [Fact]
    public void Eject_BlockedDisk_NeverAttemptsNativeEject()
    {
        PhysicalDisk disk = new()
        {
            DiskNumber = 0,
            DevicePath = @"\\.\PhysicalDrive0",
            BusType = "Nvme",
            SizeInBytes = 1024,
            Safety = new DiskSafetyAssessment
            {
                Status = DiskSafetyStatus.SystemDisk,
                IsSelectable = false,
                IsSystemDisk = true,
                Reason = "System disk"
            }
        };

        DeviceEjectService service = new();

        Assert.Throws<InvalidOperationException>(
            () => service.Eject(disk));
    }
}
