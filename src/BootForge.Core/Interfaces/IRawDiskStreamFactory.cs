using BootForge.Core.Models;

namespace BootForge.Core.Interfaces;

public interface IRawDiskStreamFactory
{
    Stream OpenWrite(PhysicalDisk disk);
}
