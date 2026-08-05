using BootForge.Core.Interfaces;
using BootForge.Core.Models;

namespace BootForge.DeviceManagement.Services;

public sealed class StorageDeviceService : IStorageDeviceService
{
    public IReadOnlyList<StorageDevice> GetRemovableDevices()
    {
        List<StorageDevice> devices = [];

        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Removable)
            {
                continue;
            }

            devices.Add(CreateStorageDevice(drive));
        }

        return devices
            .OrderBy(device => device.RootDirectory)
            .ToList();
    }

    private static StorageDevice CreateStorageDevice(DriveInfo drive)
    {
        if (!drive.IsReady)
        {
            return new StorageDevice
            {
                Name = drive.Name,
                RootDirectory = drive.RootDirectory.FullName,
                IsReady = false,
                IsRemovable = true
            };
        }

        try
        {
            return new StorageDevice
            {
                Name = drive.Name,
                RootDirectory = drive.RootDirectory.FullName,
                VolumeLabel = drive.VolumeLabel,
                FileSystem = drive.DriveFormat,
                TotalSize = drive.TotalSize,
                AvailableFreeSpace = drive.AvailableFreeSpace,
                IsReady = true,
                IsRemovable = true
            };
        }
        catch (IOException)
        {
            return CreateUnavailableDevice(drive);
        }
        catch (UnauthorizedAccessException)
        {
            return CreateUnavailableDevice(drive);
        }
    }

    private static StorageDevice CreateUnavailableDevice(DriveInfo drive)
    {
        return new StorageDevice
        {
            Name = drive.Name,
            RootDirectory = drive.RootDirectory.FullName,
            IsReady = false,
            IsRemovable = true
        };
    }
}