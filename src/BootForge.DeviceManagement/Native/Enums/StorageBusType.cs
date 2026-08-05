namespace BootForge.DeviceManagement.Native.Enums;

internal enum StorageBusType : byte
{
    Unknown = 0,
    Scsi = 1,
    Atapi = 2,
    Ata = 3,
    FireWire = 4,
    Ssa = 5,
    Fibre = 6,
    Usb = 7,
    Raid = 8,
    Iscsi = 9,
    Sas = 10,
    Sata = 11,
    Sd = 12,
    Mmc = 13,
    Virtual = 14,
    FileBackedVirtual = 15,
    Spaces = 16,
    Nvme = 17,
    Scm = 18,
    Ufs = 19
}