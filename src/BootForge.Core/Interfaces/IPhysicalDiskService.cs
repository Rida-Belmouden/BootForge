using BootForge.Core.Models;

namespace BootForge.Core.Interfaces;

public interface IPhysicalDiskService
{
    IReadOnlyList<PhysicalDisk> GetPhysicalDisks();
}