using BootForge.Core.Models;

namespace BootForge.Core.Interfaces;

public interface IStorageDeviceService
{
    IReadOnlyList<StorageDevice> GetRemovableDevices();
}