using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace BootForge.DeviceManagement.Native;

internal sealed class NativeVolumeHandle : IVolumeHandle
{
    private SafeFileHandle? _handle;
    private bool _isLocked;
    private bool _isDisposed;

    public NativeVolumeHandle(
        string name,
        IReadOnlySet<int> diskNumbers)
    {
        Name = name;
        DiskNumbers = diskNumbers;
    }

    public string Name { get; }

    public IReadOnlySet<int> DiskNumbers { get; }

    public void Lock()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_isLocked)
        {
            return;
        }

        string devicePath = Name.TrimEnd('\\');

        SafeFileHandle handle = Kernel32.CreateFile(
            devicePath,
            Kernel32.GenericRead | Kernel32.GenericWrite,
            Kernel32.FileShareRead | Kernel32.FileShareWrite,
            securityAttributes: 0,
            Kernel32.OpenExisting,
            flagsAndAttributes: 0,
            templateFile: 0);

        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastPInvokeError();
            handle.Dispose();

            throw new Win32Exception(
                error,
                $"Unable to open volume '{Name}' for locking.");
        }

        _handle = handle;

        if (!SendControlCode(Kernel32.FsctlLockVolume))
        {
            int error = Marshal.GetLastPInvokeError();
            Dispose();

            throw new Win32Exception(
                error,
                $"Unable to lock volume '{Name}'. Close any files using the target drive and try again.");
        }

        _isLocked = true;
    }

    public void Dismount()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!_isLocked || _handle is null)
        {
            throw new InvalidOperationException(
                $"Volume '{Name}' must be locked before it is dismounted.");
        }

        if (!SendControlCode(Kernel32.FsctlDismountVolume))
        {
            throw new Win32Exception(
                Marshal.GetLastPInvokeError(),
                $"Unable to dismount volume '{Name}'.");
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_handle is not null &&
            _isLocked &&
            !_handle.IsInvalid)
        {
            SendControlCode(Kernel32.FsctlUnlockVolume);
        }

        _isLocked = false;
        _handle?.Dispose();
        _handle = null;
    }

    private bool SendControlCode(uint controlCode)
    {
        return Kernel32.DeviceIoControl(
            _handle!,
            controlCode,
            inputBuffer: 0,
            inputBufferSize: 0,
            outputBuffer: 0,
            outputBufferSize: 0,
            out _,
            overlapped: 0);
    }
}
