using System.Runtime.InteropServices;
using BootForge.DeviceManagement.Native.Structures;
using Microsoft.Win32.SafeHandles;

namespace BootForge.DeviceManagement.Native;

internal sealed class VolumeDiskNumberResolver
{
    public int? TryResolveFromPath(string path)
    {
        string? root = Path.GetPathRoot(path);

        if (string.IsNullOrWhiteSpace(root))
        {
            return null;
        }

        string volumePath = $@"\\.\{root.TrimEnd('\\')}";

        using SafeFileHandle handle = Kernel32.CreateFile(
            volumePath,
            desiredAccess: 0,
            Kernel32.FileShareRead | Kernel32.FileShareWrite,
            securityAttributes: 0,
            Kernel32.OpenExisting,
            flagsAndAttributes: 0,
            templateFile: 0);

        if (handle.IsInvalid)
        {
            return null;
        }

        int outputSize = Marshal.SizeOf<StorageDeviceNumber>();
        nint outputBuffer = Marshal.AllocHGlobal(outputSize);

        try
        {
            bool success = Kernel32.DeviceIoControl(
                handle,
                Kernel32.IoctlStorageGetDeviceNumber,
                inputBuffer: 0,
                inputBufferSize: 0,
                outputBuffer,
                (uint)outputSize,
                out _,
                overlapped: 0);

            if (!success)
            {
                return null;
            }

            StorageDeviceNumber deviceNumber =
                Marshal.PtrToStructure<StorageDeviceNumber>(outputBuffer);

            return checked((int)deviceNumber.DeviceNumber);
        }
        finally
        {
            Marshal.FreeHGlobal(outputBuffer);
        }
    }
}