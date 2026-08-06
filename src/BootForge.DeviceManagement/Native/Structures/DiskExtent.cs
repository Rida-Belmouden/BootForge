using System.Runtime.InteropServices;

namespace BootForge.DeviceManagement.Native.Structures;

[StructLayout(LayoutKind.Sequential)]
internal struct DiskExtent
{
    public uint DiskNumber;

    public long StartingOffset;

    public long ExtentLength;
}
