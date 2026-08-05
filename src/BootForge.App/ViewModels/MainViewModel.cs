using System.Collections.ObjectModel;
using BootForge.Core.Interfaces;
using BootForge.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BootForge.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IPhysicalDiskService _physicalDiskService;
    private readonly IDiskImageService _diskImageService;
    private readonly IImageFilePicker _imageFilePicker;
    private readonly IWritePlanService _writePlanService;
    private readonly IWriteConfirmationService
        _writeConfirmationService;

    public MainViewModel(
        IPhysicalDiskService physicalDiskService,
        IDiskImageService diskImageService,
        IImageFilePicker imageFilePicker,
        IWritePlanService writePlanService,
        IWriteConfirmationService writeConfirmationService)
    {
        _physicalDiskService = physicalDiskService;
        _diskImageService = diskImageService;
        _imageFilePicker = imageFilePicker;
        _writePlanService = writePlanService;
        _writeConfirmationService =
            writeConfirmationService;

        RefreshDisks();
    }

    public ObservableCollection<PhysicalDisk> Disks { get; } = [];

    [ObservableProperty]
    private PhysicalDisk? selectedDisk;

    [ObservableProperty]
    private DiskImage? selectedImage;

    [ObservableProperty]
    private string statusMessage = "Ready";

    public bool CanStart =>
        SelectedDisk?.IsSelectable == true &&
        SelectedImage is not null &&
        SelectedImage.FitsOn(SelectedDisk);

    public string StartHint
    {
        get
        {
            if (SelectedImage is null)
            {
                return "Select an ISO or IMG image.";
            }

            if (SelectedDisk is null)
            {
                return "Select a target disk.";
            }

            if (!SelectedDisk.IsSelectable)
            {
                return "The selected disk is blocked for safety.";
            }

            if (!SelectedImage.FitsOn(SelectedDisk))
            {
                return "The image is larger than the selected disk.";
            }

            return "Ready to write. All data on the target will be erased.";
        }
    }

    partial void OnSelectedDiskChanged(PhysicalDisk? value)
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(StartHint));
    }

    partial void OnSelectedImageChanged(DiskImage? value)
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(StartHint));
    }

    [RelayCommand]
    private void Start()
    {
        if (!CanStart ||
            SelectedImage is null ||
            SelectedDisk is null)
        {
            return;
        }

        try
        {
            WritePlan plan = _writePlanService.Create(
                SelectedImage,
                SelectedDisk);

            if (!_writeConfirmationService.Confirm(plan))
            {
                StatusMessage = "Write operation cancelled.";
                return;
            }

            StatusMessage =
                "Safety checks passed. Raw disk writing remains disabled until volume locking is available.";
        }
        catch (Exception exception)
        {
            StatusMessage =
                $"Unable to start: {exception.Message}";
        }
    }

    [RelayCommand]
    private void SelectImage()
    {
        string? filePath = _imageFilePicker.PickImageFile();

        if (filePath is null)
        {
            return;
        }

        try
        {
            SelectedImage = _diskImageService.Load(filePath);
            StatusMessage =
                $"Selected {SelectedImage.FileName}.";
        }
        catch (Exception exception)
        {
            SelectedImage = null;
            StatusMessage =
                $"Image selection failed: {exception.Message}";
        }
    }

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
                .FirstOrDefault(disk => disk.IsSelectable)
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
