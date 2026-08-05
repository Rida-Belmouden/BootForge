using System.Runtime.InteropServices;

namespace BootForge.DeviceManagement.Native.Structures;

[StructLayout(LayoutKind.Sequential)]
internal struct VolumeDiskExtents
{
    public uint NumberOfDiskExtents;

    public DiskExtent FirstExtent;
}
