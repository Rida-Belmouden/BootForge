using System.Runtime.InteropServices;
using BootForge.DeviceManagement.Native.Structures;

namespace BootForge.Core.Tests.Native;

public sealed class DiskGeometryStructureTests
{
    [Fact]
    public void DiskGeometry_MatchesWindowsLayout()
    {
        Assert.Equal(24, Marshal.SizeOf<DiskGeometry>());
        Assert.Equal(
            new nint(8),
            Marshal.OffsetOf<DiskGeometry>(
                nameof(DiskGeometry.MediaType)));
    }

    [Fact]
    public void DiskGeometryEx_DiskSizeStartsAfterGeometry()
    {
        Assert.Equal(32, Marshal.SizeOf<DiskGeometryEx>());
        Assert.Equal(
            new nint(24),
            Marshal.OffsetOf<DiskGeometryEx>(
                nameof(DiskGeometryEx.DiskSize)));
    }
}
