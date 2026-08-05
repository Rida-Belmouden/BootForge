using BootForge.Core.Models;

namespace BootForge.Core.Tests.Models;

public sealed class StorageDeviceTests
{
    [Fact]
    public void DisplayName_WithLabel_ReturnsFormattedDeviceName()
    {
        StorageDevice device = new()
        {
            Name = "E:\\",
            RootDirectory = "E:\\",
            VolumeLabel = "KINGSTON",
            TotalSize = 32L * 1024 * 1024 * 1024,
            IsReady = true,
            IsRemovable = true
        };

        Assert.Equal(
            "KINGSTON (E:\\) - 32 GB",
            device.DisplayName);
    }

    [Fact]
    public void DisplayName_WithoutLabel_UsesDefaultDescription()
    {
        StorageDevice device = new()
        {
            Name = "F:\\",
            RootDirectory = "F:\\",
            TotalSize = 16L * 1024 * 1024 * 1024,
            IsReady = true,
            IsRemovable = true
        };

        Assert.Equal(
            "Removable drive (F:\\) - 16 GB",
            device.DisplayName);
    }
}