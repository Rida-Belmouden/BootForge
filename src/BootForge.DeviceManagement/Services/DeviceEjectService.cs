using System.ComponentModel;
using System.Runtime.InteropServices;
using BootForge.Core.Interfaces;
using BootForge.Core.Models;
using BootForge.DeviceManagement.Native;
using Microsoft.Win32.SafeHandles;

namespace BootForge.DeviceManagement.Services;

public sealed class DeviceEjectService
    : IDeviceEjectService
{
    public void Eject(PhysicalDisk disk)
    {
        ArgumentNullException.ThrowIfNull(disk);

        if (!disk.IsSelectable)
        {
            throw new InvalidOperationException(
                "Only an approved removable target can be ejected.");
        }

        using SafeFileHandle handle = Kernel32.CreateFile(
            disk.DevicePath,
            Kernel32.GenericRead,
            Kernel32.FileShareRead | Kernel32.FileShareWrite,
            securityAttributes: 0,
            Kernel32.OpenExisting,
            flagsAndAttributes: 0,
            templateFile: 0);

        if (handle.IsInvalid)
        {
            throw new Win32Exception(
                Marshal.GetLastPInvokeError(),
                "Unable to open the target for safe removal.");
        }

        bool success = Kernel32.DeviceIoControl(
            handle,
            Kernel32.IoctlStorageEjectMedia,
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
                "Windows could not eject this USB device. Use Safely Remove Hardware instead.");
        }
    }
}
