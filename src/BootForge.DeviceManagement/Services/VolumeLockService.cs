using BootForge.Core.Interfaces;
using BootForge.DeviceManagement.Native;

namespace BootForge.DeviceManagement.Services;

public sealed class VolumeLockService : IVolumeLockService
{
    private readonly IVolumeProvider _volumeProvider;

    public VolumeLockService()
        : this(new NativeVolumeProvider())
    {
    }

    internal VolumeLockService(
        IVolumeProvider volumeProvider)
    {
        _volumeProvider = volumeProvider;
    }

    public IVolumeLock AcquireForDisk(int diskNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(
            diskNumber);

        IReadOnlyList<IVolumeHandle> allVolumes =
            _volumeProvider.OpenVolumes();

        List<IVolumeHandle> targetVolumes = [];

        foreach (IVolumeHandle volume in allVolumes)
        {
            if (volume.DiskNumbers.Contains(diskNumber))
            {
                targetVolumes.Add(volume);
            }
            else
            {
                volume.Dispose();
            }
        }

        try
        {
            foreach (IVolumeHandle volume in targetVolumes)
            {
                volume.Lock();
            }

            foreach (IVolumeHandle volume in targetVolumes)
            {
                volume.Dismount();
            }

            return new AcquiredVolumeLock(targetVolumes);
        }
        catch
        {
            DisposeAll(targetVolumes);
            throw;
        }
    }

    private static void DisposeAll(
        IEnumerable<IVolumeHandle> volumes)
    {
        foreach (IVolumeHandle volume in volumes.Reverse())
        {
            volume.Dispose();
        }
    }

    private sealed class AcquiredVolumeLock(
        IReadOnlyList<IVolumeHandle> volumes)
        : IVolumeLock
    {
        private bool _isDisposed;

        public int VolumeCount => volumes.Count;

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            DisposeAll(volumes);
        }
    }
}
