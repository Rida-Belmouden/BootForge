using BootForge.Core.Models;

namespace BootForge.Core.Interfaces;

public interface IDiskImageService
{
    DiskImage Load(string filePath);
}
