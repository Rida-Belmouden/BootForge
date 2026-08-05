using System.Collections.ObjectModel;
using BootForge.Core.Interfaces;
using BootForge.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BootForge.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IPhysicalDiskService _physicalDiskService;

    public MainViewModel(IPhysicalDiskService physicalDiskService)
    {
        _physicalDiskService = physicalDiskService;

        RefreshDisks();
    }

    public ObservableCollection<PhysicalDisk> Disks { get; } = [];

    [ObservableProperty]
    private PhysicalDisk? selectedDisk;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [RelayCommand]
    private void RefreshDisks()
    {
        Disks.Clear();

        try
        {
            IReadOnlyList<PhysicalDisk> detectedDisks =
                _physicalDiskService.GetPhysicalDisks();

            foreach (PhysicalDisk disk in detectedDisks)
            {
                Disks.Add(disk);
            }

            SelectedDisk = Disks
                .FirstOrDefault(disk =>
                    disk.BusType.Equals(
                        "Usb",
                        StringComparison.OrdinalIgnoreCase))
                ?? Disks.FirstOrDefault();

            StatusMessage = Disks.Count switch
            {
                0 => "No physical disks detected.",
                1 => "1 physical disk detected.",
                _ => $"{Disks.Count} physical disks detected."
            };
        }
        catch (Exception exception)
        {
            StatusMessage =
                $"Physical disk detection failed: {exception.Message}";
        }
    }
}