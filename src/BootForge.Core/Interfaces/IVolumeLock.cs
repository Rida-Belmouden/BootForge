namespace BootForge.Core.Interfaces;

public interface IVolumeLock : IDisposable
{
    int VolumeCount { get; }
}
