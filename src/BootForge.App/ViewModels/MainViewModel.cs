using System.Collections.ObjectModel;
using BootForge.Core.Interfaces;
using BootForge.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BootForge.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IStorageDeviceService _storageDeviceService;

    public MainViewModel(IStorageDeviceService storageDeviceService)
    {
        _storageDeviceService = storageDeviceService;

        RefreshDevices();
    }

    public ObservableCollection<StorageDevice> Devices { get; } = [];

    [ObservableProperty]
    private StorageDevice? selectedDevice;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [RelayCommand]
    private void RefreshDevices()
    {
        Devices.Clear();

        try
        {
            IReadOnlyList<StorageDevice> detectedDevices =
                _storageDeviceService.GetRemovableDevices();

            foreach (StorageDevice device in detectedDevices)
            {
                Devices.Add(device);
            }

            StatusMessage = Devices.Count switch
            {
                0 => "No removable device detected.",
                1 => "1 removable device detected.",
                _ => $"{Devices.Count} removable devices detected."
            };

            SelectedDevice = Devices.FirstOrDefault();
        }
        catch (Exception exception)
        {
            StatusMessage = $"Device detection failed: {exception.Message}";
        }
    }
}