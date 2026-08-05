using BootForge.Core.Models;

namespace BootForge.Core.Interfaces;

public interface IWritePlanService
{
    WritePlan Create(
        DiskImage selectedImage,
        PhysicalDisk selectedDisk);
}
