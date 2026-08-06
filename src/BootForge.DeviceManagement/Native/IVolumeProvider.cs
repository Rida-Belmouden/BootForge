namespace BootForge.DeviceManagement.Native;

internal interface IVolumeProvider
{
    IReadOnlyList<IVolumeHandle> OpenVolumes();
}
