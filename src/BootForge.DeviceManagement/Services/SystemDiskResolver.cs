using BootForge.Core.Interfaces;
using BootForge.DeviceManagement.Native;

namespace BootForge.DeviceManagement.Services;

public sealed class SystemDiskResolver : ISystemDiskResolver
{
    private readonly VolumeDiskNumberResolver _resolver = new();

    public int? GetSystemDiskNumber()
    {
        string systemDirectory = Environment.SystemDirectory;

        return _resolver.TryResolveFromPath(systemDirectory);
    }

    public int? GetBootDiskNumber()
    {
        string windowsDirectory =
            Environment.GetFolderPath(
                Environment.SpecialFolder.Windows);

        return _resolver.TryResolveFromPath(windowsDirectory);
    }
}