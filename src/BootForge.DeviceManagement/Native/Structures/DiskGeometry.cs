using System.Runtime.InteropServices;

namespace BootForge.DeviceManagement.Native.Structures;

[StructLayout(LayoutKind.Sequential)]
internal struct DiskGeometry
{
    public long Cylinders;

    public int MediaType;

    public uint TracksPerCylinder;

    public uint SectorsPerTrack;

    public uint BytesPerSector;
}
