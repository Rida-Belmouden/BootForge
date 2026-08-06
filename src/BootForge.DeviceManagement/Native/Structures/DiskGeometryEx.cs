using System.Runtime.InteropServices;

namespace BootForge.DeviceManagement.Native.Structures;

[StructLayout(LayoutKind.Sequential)]
internal struct DiskGeometryEx
{
    public DiskGeometry Geometry;

    public long DiskSize;
}
