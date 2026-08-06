using System.Runtime.InteropServices;

namespace BootForge.DeviceManagement.Native.Structures;

[StructLayout(LayoutKind.Sequential)]
internal struct StorageDeviceNumber
{
    public uint DeviceType;

    public uint DeviceNumber;

    public uint PartitionNumber;
}