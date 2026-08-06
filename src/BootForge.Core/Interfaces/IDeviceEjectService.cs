using BootForge.Core.Models;

namespace BootForge.Core.Interfaces;

public interface IDeviceEjectService
{
    void Eject(PhysicalDisk disk);
}
