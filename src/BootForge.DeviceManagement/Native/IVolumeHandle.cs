namespace BootForge.DeviceManagement.Native;

internal interface IVolumeHandle : IDisposable
{
    string Name { get; }

    IReadOnlySet<int> DiskNumbers { get; }

    void Lock();

    void Dismount();
}
