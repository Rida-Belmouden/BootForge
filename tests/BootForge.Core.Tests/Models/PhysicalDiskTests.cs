using BootForge.Core.Models;

namespace BootForge.Core.Tests.Models;

public sealed class PhysicalDiskTests
{
    [Fact]
    public void DisplayName_WithVendorAndProduct_ReturnsFullName()
    {
        PhysicalDisk disk = new()
        {
            DiskNumber = 2,
            DevicePath = @"\\.\PhysicalDrive2",
            Vendor = "Kingston",
            Product = "DataTraveler",
            BusType = "Usb",
            SizeInBytes = 32L * 1024 * 1024 * 1024
        };

        Assert.Equal(
            "Kingston DataTraveler — Disk 2 — 32 GB",
            disk.DisplayName);
    }

    [Fact]
    public void DisplayName_WithoutDescriptor_UsesDiskNumber()
    {
        PhysicalDisk disk = new()
        {
            DiskNumber = 3,
            DevicePath = @"\\.\PhysicalDrive3",
            SizeInBytes = 16L * 1024 * 1024 * 1024
        };

        Assert.Equal(
            "Physical Disk 3 — Disk 3 — 16 GB",
            disk.DisplayName);
    }
}