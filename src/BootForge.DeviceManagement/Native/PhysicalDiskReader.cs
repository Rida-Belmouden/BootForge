using System.Runtime.InteropServices;
using System.Text;
using BootForge.Core.Models;
using BootForge.DeviceManagement.Native.Structures;
using Microsoft.Win32.SafeHandles;

namespace BootForge.DeviceManagement.Native;

internal sealed class PhysicalDiskReader
{
    private const int DescriptorBufferSize = 4096;

    public PhysicalDisk? TryRead(int diskNumber)
    {
        string devicePath = $@"\\.\PhysicalDrive{diskNumber}";

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
            return null;
        }

        StorageDescriptorResult descriptor = ReadDescriptor(handle);
        long size = ReadDiskLength(handle);

        return new PhysicalDisk
        {
            DiskNumber = diskNumber,
            DevicePath = devicePath,
            Vendor = descriptor.Vendor,
            Product = descriptor.Product,
            Revision = descriptor.Revision,
            SerialNumber = descriptor.SerialNumber,
            BusType = descriptor.BusType,
            IsRemovableMedia = descriptor.IsRemovableMedia,
            SizeInBytes = size
        };
    }

    private static StorageDescriptorResult ReadDescriptor(
        SafeFileHandle handle)
    {
        int querySize = Marshal.SizeOf<StoragePropertyQuery>();
        nint queryBuffer = Marshal.AllocHGlobal(querySize);
        nint outputBuffer = Marshal.AllocHGlobal(DescriptorBufferSize);

        try
        {
            StoragePropertyQuery query = new()
            {
                PropertyId = Enums.StoragePropertyId.StorageDeviceProperty,
                QueryType = Enums.StorageQueryType.PropertyStandardQuery,
                AdditionalParameters = 0
            };

            Marshal.StructureToPtr(query, queryBuffer, false);

            bool success = Kernel32.DeviceIoControl(
                handle,
                Kernel32.IoctlStorageQueryProperty,
                queryBuffer,
                (uint)querySize,
                outputBuffer,
                DescriptorBufferSize,
                out _,
                overlapped: 0);

            if (!success)
            {
                return StorageDescriptorResult.Empty;
            }

            StorageDeviceDescriptor descriptor =
                Marshal.PtrToStructure<StorageDeviceDescriptor>(outputBuffer);

            return new StorageDescriptorResult
            {
                Vendor = ReadAnsiString(outputBuffer, descriptor.VendorIdOffset),
                Product = ReadAnsiString(outputBuffer, descriptor.ProductIdOffset),
                Revision = ReadAnsiString(
                    outputBuffer,
                    descriptor.ProductRevisionOffset),
                SerialNumber = ReadAnsiString(
                    outputBuffer,
                    descriptor.SerialNumberOffset),
                BusType = descriptor.BusType.ToString(),
                IsRemovableMedia = descriptor.RemovableMedia
            };
        }
        finally
        {
            Marshal.FreeHGlobal(outputBuffer);
            Marshal.FreeHGlobal(queryBuffer);
        }
    }

    private static long ReadDiskLength(SafeFileHandle handle)
    {
        int outputSize = Marshal.SizeOf<GetLengthInformation>();
        nint outputBuffer = Marshal.AllocHGlobal(outputSize);

        try
        {
            bool success = Kernel32.DeviceIoControl(
                handle,
                Kernel32.IoctlDiskGetLengthInfo,
                inputBuffer: 0,
                inputBufferSize: 0,
                outputBuffer,
                (uint)outputSize,
                out _,
                overlapped: 0);

            if (!success)
            {
                return 0;
            }

            GetLengthInformation information =
                Marshal.PtrToStructure<GetLengthInformation>(outputBuffer);

            return information.Length;
        }
        finally
        {
            Marshal.FreeHGlobal(outputBuffer);
        }
    }

    private static string? ReadAnsiString(nint buffer, uint offset)
    {
        if (offset == 0 || offset >= DescriptorBufferSize)
        {
            return null;
        }

        List<byte> bytes = [];

        for (int index = (int)offset;
             index < DescriptorBufferSize;
             index++)
        {
            byte value = Marshal.ReadByte(buffer, index);

            if (value == 0)
            {
                break;
            }

            bytes.Add(value);
        }

        if (bytes.Count == 0)
        {
            return null;
        }

        return Encoding.ASCII
            .GetString(bytes.ToArray())
            .Trim();
    }

    private sealed record StorageDescriptorResult
    {
        public static StorageDescriptorResult Empty { get; } = new();

        public string? Vendor { get; init; }

        public string? Product { get; init; }

        public string? Revision { get; init; }

        public string? SerialNumber { get; init; }

        public string BusType { get; init; } = "Unknown";

        public bool IsRemovableMedia { get; init; }
    }
}