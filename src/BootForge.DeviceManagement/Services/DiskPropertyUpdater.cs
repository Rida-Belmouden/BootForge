using System.ComponentModel;
using System.Runtime.InteropServices;
using BootForge.Core.Interfaces;
using BootForge.Core.Models;
using BootForge.DeviceManagement.Native;
using Microsoft.Win32.SafeHandles;

namespace BootForge.DeviceManagement.Services;

public sealed class DiskPropertyUpdater
    : IDiskPropertyUpdater
{
    public void Update(PhysicalDisk disk)
    {
        ArgumentNullException.ThrowIfNull(disk);

        using SafeFileHandle handle = Kernel32.CreateFile(
            disk.DevicePath,
            desiredAccess: 0,
            Kernel32.FileShareRead | Kernel32.FileShareWrite,
            securityAttributes: 0,
            Kernel32.OpenExisting,
            flagsAndAttributes: 0,
            templateFile: 0);

        if (handle.IsInvalid)
        {
            throw new Win32Exception(
                Marshal.GetLastPInvokeError(),
                "Unable to reopen the target disk for refresh.");
        }

        bool success = Kernel32.DeviceIoControl(
            handle,
            Kernel32.IoctlDiskUpdateProperties,
            inputBuffer: 0,
            inputBufferSize: 0,
            outputBuffer: 0,
            outputBufferSize: 0,
            out _,
            overlapped: 0);

        if (!success)
        {
            throw new Win32Exception(
                Marshal.GetLastPInvokeError(),
                "The image was verified, but Windows could not refresh the target disk layout.");
        }
    }
}
