namespace BootForge.Core.Interfaces;

public interface IVolumeLockService
{
    IVolumeLock AcquireForDisk(int diskNumber);
}
