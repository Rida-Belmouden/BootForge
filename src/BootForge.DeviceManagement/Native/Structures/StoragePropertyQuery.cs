using System.Runtime.InteropServices;
using BootForge.DeviceManagement.Native.Enums;

namespace BootForge.DeviceManagement.Native.Structures;

[StructLayout(LayoutKind.Sequential)]
internal struct StoragePropertyQuery
{
    public StoragePropertyId PropertyId;

    public StorageQueryType QueryType;

    public byte AdditionalParameters;
}