using System.ComponentModel;
using System.Runtime.InteropServices;
using BootForge.Core.Interfaces;
using BootForge.Core.Models;
using BootForge.DeviceManagement.Native;
using Microsoft.Win32.SafeHandles;

namespace BootForge.DeviceManagement.Services;

public sealed class RawDiskStreamFactory
    : IRawDiskStreamFactory
{
    private const int StreamBufferSize = 4096;

    public Stream OpenWrite(PhysicalDisk disk)
    {
        ArgumentNullException.ThrowIfNull(disk);

        if (!disk.IsSelectable)
        {
            throw new InvalidOperationException(
                "The target disk is blocked by the safety policy.");
        }

        SafeFileHandle handle = Kernel32.CreateFile(
            disk.DevicePath,
            Kernel32.GenericRead | Kernel32.GenericWrite,
            Kernel32.FileShareRead | Kernel32.FileShareWrite,
            securityAttributes: 0,
            Kernel32.OpenExisting,
            Kernel32.FileFlagWriteThrough |
            Kernel32.FileFlagOverlapped,
            templateFile: 0);

        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastPInvokeError();
            handle.Dispose();

            throw new Win32Exception(
                error,
                $"Unable to open {disk.DevicePath} for raw writing.");
        }

        try
        {
            FileStream stream = new(
                handle,
                FileAccess.ReadWrite,
                StreamBufferSize,
                isAsync: true);

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }
}
