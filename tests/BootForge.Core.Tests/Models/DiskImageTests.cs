using BootForge.Core.Models;

namespace BootForge.Core.Tests.Models;

public sealed class DiskImageTests
{
    [Theory]
    [InlineData(1024, 2048, true)]
    [InlineData(2048, 2048, true)]
    [InlineData(2049, 2048, false)]
    public void FitsOn_ComparesImageAndDiskCapacity(
        long imageSize,
        long diskSize,
        bool expected)
    {
        DiskImage image = new()
        {
            FilePath = "image.iso",
            FileName = "image.iso",
            Format = "ISO",
            SizeInBytes = imageSize
        };

        PhysicalDisk disk = new()
        {
            DiskNumber = 2,
            DevicePath = @"\\.\PhysicalDrive2",
            SizeInBytes = diskSize
        };

        Assert.Equal(expected, image.FitsOn(disk));
    }
}
