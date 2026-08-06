using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace BootForge.DeviceManagement.Native;

internal static partial class Kernel32
{
    internal const uint FileShareRead = 0x00000001;
    internal const uint FileShareWrite = 0x00000002;
    internal const uint GenericRead = 0x80000000;
    internal const uint GenericWrite = 0x40000000;
    internal const uint FileFlagWriteThrough = 0x80000000;
    internal const uint FileFlagOverlapped = 0x40000000;
    internal const uint OpenExisting = 3;

    internal const uint IoctlStorageQueryProperty = 0x002D1400;
    internal const uint IoctlDiskGetLengthInfo = 0x0007405C;
    internal const uint IoctlDiskGetDriveGeometryEx = 0x000700A0;
    internal const uint IoctlDiskUpdateProperties = 0x00070140;
    internal const uint IoctlStorageGetDeviceNumber = 0x002D1080;
    internal const uint IoctlStorageEjectMedia = 0x002D4808;
    internal const uint IoctlVolumeGetVolumeDiskExtents =
        0x00560000;

    internal const uint FsctlLockVolume = 0x00090018;
    internal const uint FsctlUnlockVolume = 0x0009001C;
    internal const uint FsctlDismountVolume = 0x00090020;

    internal const int ErrorNoMoreFiles = 18;
    internal const int ErrorMoreData = 234;
    internal const int ErrorInvalidFunction = 1;
    internal const int ErrorNotReady = 21;

    [LibraryImport(
        "kernel32.dll",
        EntryPoint = "CreateFileW",
        SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16)]
    internal static partial SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        nint securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        nint templateFile);

    [LibraryImport(
        "kernel32.dll",
        EntryPoint = "DeviceIoControl",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeviceIoControl(
        SafeFileHandle deviceHandle,
        uint ioControlCode,
        nint inputBuffer,
        uint inputBufferSize,
        nint outputBuffer,
        uint outputBufferSize,
        out uint bytesReturned,
        nint overlapped);

    [LibraryImport(
        "kernel32.dll",
        EntryPoint = "FindFirstVolumeW",
        SetLastError = true)]
    private static unsafe partial nint FindFirstVolumeNative(
        char* volumeName,
        uint bufferLength);

    [LibraryImport(
        "kernel32.dll",
        EntryPoint = "FindNextVolumeW",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool FindNextVolumeNative(
        nint findVolumeHandle,
        char* volumeName,
        uint bufferLength);

    [LibraryImport(
        "kernel32.dll",
        EntryPoint = "FindVolumeClose",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool FindVolumeClose(
        nint findVolumeHandle);

    internal static unsafe nint FindFirstVolume(
        char[] volumeName)
    {
        fixed (char* buffer = volumeName)
        {
            return FindFirstVolumeNative(
                buffer,
                (uint)volumeName.Length);
        }
    }

    internal static unsafe bool FindNextVolume(
        nint findVolumeHandle,
        char[] volumeName)
    {
        fixed (char* buffer = volumeName)
        {
            return FindNextVolumeNative(
                findVolumeHandle,
                buffer,
                (uint)volumeName.Length);
        }
    }
}
