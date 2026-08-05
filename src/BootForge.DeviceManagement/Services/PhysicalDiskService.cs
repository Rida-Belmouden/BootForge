using BootForge.Core.Interfaces;
using BootForge.Core.Models;
using BootForge.DeviceManagement.Native;

namespace BootForge.DeviceManagement.Services;

public sealed class PhysicalDiskService : IPhysicalDiskService
{
    private const int MaximumDiskProbeCount = 64;

    private readonly PhysicalDiskReader _reader = new();

    public IReadOnlyList<PhysicalDisk> GetPhysicalDisks()
    {
        List<PhysicalDisk> disks = [];

        for (int diskNumber = 0;
             diskNumber < MaximumDiskProbeCount;
             diskNumber++)
        {
            PhysicalDisk? disk = _reader.TryRead(diskNumber);

            if (disk is not null)
            {
                disks.Add(disk);
            }
        }

        return disks;
    }
}