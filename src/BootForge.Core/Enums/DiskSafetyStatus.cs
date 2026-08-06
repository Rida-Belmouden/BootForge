namespace BootForge.Core.Enums;

public enum DiskSafetyStatus
{
    Unknown = 0,
    SafeUsbDevice = 1,
    SystemDisk = 2,
    InternalDisk = 3,
    UnsupportedBusType = 4,
    Unavailable = 5
}