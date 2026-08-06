using System.ComponentModel;
using System.Runtime.InteropServices;
using BootForge.DeviceManagement.Native.Structures;
using Microsoft.Win32.SafeHandles;

namespace BootForge.DeviceManagement.Native;

internal sealed class VolumeExtentResolver
{
    private const int MaximumExtentCount = 1024;

    public IReadOnlySet<int> GetDiskNumbers(string volumeName)
    {
        string devicePath = volumeName.TrimEnd('\\');

        using SafeFileHandle handle = Kernel32.CreateFile(
            devicePath,
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
                $"Unable to open volume '{volumeName}'.");
        }

        int extentOffset = checked(
            (int)Marshal.OffsetOf<VolumeDiskExtents>(
                nameof(VolumeDiskExtents.FirstExtent)));

        int extentSize = Marshal.SizeOf<DiskExtent>();
        int bufferSize = Marshal.SizeOf<VolumeDiskExtents>();

        nint buffer = Marshal.AllocHGlobal(bufferSize);

        try
        {
            bool success = QueryExtents(
                handle,
                buffer,
                bufferSize);

            if (!success &&
                Marshal.GetLastPInvokeError() ==
                Kernel32.ErrorMoreData)
            {
                uint extentCount = checked(
                    (uint)Marshal.ReadInt32(buffer));

                if (extentCount == 0 ||
                    extentCount > MaximumExtentCount)
                {
                    throw new InvalidDataException(
                        "The volume returned an invalid extent count.");
                }

                bufferSize = checked(
                    extentOffset +
                    (int)extentCount * extentSize);

                buffer = Marshal.ReAllocHGlobal(
                    buffer,
                    bufferSize);

                success = QueryExtents(
                    handle,
                    buffer,
                    bufferSize);
            }

            if (!success)
            {
                throw new Win32Exception(
                    Marshal.GetLastPInvokeError(),
                    $"Unable to resolve volume '{volumeName}'.");
            }

            uint count = checked(
                (uint)Marshal.ReadInt32(buffer));

            if (count > MaximumExtentCount)
            {
                throw new InvalidDataException(
                    "The volume returned too many disk extents.");
            }

            HashSet<int> diskNumbers = [];

            for (int index = 0; index < count; index++)
            {
                nint extentPointer = nint.Add(
                    buffer,
                    checked(extentOffset + index * extentSize));

                DiskExtent extent =
                    Marshal.PtrToStructure<DiskExtent>(
                        extentPointer);

                diskNumbers.Add(
                    checked((int)extent.DiskNumber));
            }

            return diskNumbers;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool QueryExtents(
        SafeFileHandle handle,
        nint buffer,
        int bufferSize)
    {
        return Kernel32.DeviceIoControl(
            handle,
            Kernel32.IoctlVolumeGetVolumeDiskExtents,
            inputBuffer: 0,
            inputBufferSize: 0,
            buffer,
            checked((uint)bufferSize),
            out _,
            overlapped: 0);
    }
}
