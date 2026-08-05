using System.ComponentModel;
using System.Runtime.InteropServices;

namespace BootForge.DeviceManagement.Native;

internal sealed class NativeVolumeProvider : IVolumeProvider
{
    private const int VolumeNameBufferLength = 1024;

    private readonly VolumeExtentResolver _extentResolver = new();

    public IReadOnlyList<IVolumeHandle> OpenVolumes()
    {
        List<IVolumeHandle> volumes = [];
        char[] volumeNameBuffer =
            new char[VolumeNameBufferLength];

        nint searchHandle =
            Kernel32.FindFirstVolume(volumeNameBuffer);

        if (searchHandle == new nint(-1))
        {
            throw new Win32Exception(
                Marshal.GetLastPInvokeError(),
                "Unable to enumerate Windows volumes.");
        }

        try
        {
            while (true)
            {
                string volumeName =
                    ReadVolumeName(volumeNameBuffer);

                TryAddVolume(volumes, volumeName);

                Array.Clear(volumeNameBuffer);

                if (Kernel32.FindNextVolume(
                        searchHandle,
                        volumeNameBuffer))
                {
                    continue;
                }

                int error = Marshal.GetLastPInvokeError();

                if (error == Kernel32.ErrorNoMoreFiles)
                {
                    break;
                }

                throw new Win32Exception(
                    error,
                    "Unable to continue enumerating Windows volumes.");
            }

            return volumes;
        }
        catch
        {
            foreach (IVolumeHandle volume in volumes)
            {
                volume.Dispose();
            }

            throw;
        }
        finally
        {
            Kernel32.FindVolumeClose(searchHandle);
        }
    }

    private void TryAddVolume(
        ICollection<IVolumeHandle> volumes,
        string volumeName)
    {
        try
        {
            IReadOnlySet<int> diskNumbers =
                _extentResolver.GetDiskNumbers(volumeName);

            volumes.Add(
                new NativeVolumeHandle(
                    volumeName,
                    diskNumbers));
        }
        catch (Win32Exception exception)
            when (exception.NativeErrorCode is
                  Kernel32.ErrorInvalidFunction or
                  Kernel32.ErrorNotReady)
        {
            // Volumes without media and unsupported virtual volumes
            // cannot contain the selected writable USB disk.
        }
    }

    private static string ReadVolumeName(char[] buffer)
    {
        int terminatorIndex = Array.IndexOf(buffer, '\0');

        if (terminatorIndex <= 0)
        {
            throw new InvalidDataException(
                "Windows returned an invalid volume name.");
        }

        return new string(
            buffer,
            startIndex: 0,
            terminatorIndex);
    }
}
