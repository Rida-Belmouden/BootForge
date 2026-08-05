using System.Runtime.InteropServices;
using BootForge.DeviceManagement.Native.Enums;

namespace BootForge.DeviceManagement.Native.Structures;

[StructLayout(LayoutKind.Sequential)]
internal struct StorageDeviceDescriptor
{
    public uint Version;

    public uint Size;

    public byte DeviceType;

    public byte DeviceTypeModifier;

    [MarshalAs(UnmanagedType.U1)]
    public bool RemovableMedia;

    [MarshalAs(UnmanagedType.U1)]
    public bool CommandQueueing;

    public uint VendorIdOffset;

    public uint ProductIdOffset;

    public uint ProductRevisionOffset;

    public uint SerialNumberOffset;

    public StorageBusType BusType;

    public uint RawPropertiesLength;
}