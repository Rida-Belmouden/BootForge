using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using BootForge.Core.Enums;
using BootForge.Core.Interfaces;
using BootForge.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BootForge.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    public string VersionText { get; } = GetVersionText();

    private readonly IPhysicalDiskService _physicalDiskService;
    private readonly IDiskImageService _diskImageService;
    private readonly IImageFilePicker _imageFilePicker;
    private readonly IWritePlanService _writePlanService;
    private readonly IWriteConfirmationService
        _writeConfirmationService;
    private readonly IWriteOperationService
        _writeOperationService;
    private readonly IDeviceEjectService _deviceEjectService;

    private CancellationTokenSource? _writeCancellation;
    private readonly Stopwatch _writeStopwatch = new();

    public MainViewModel(
        IPhysicalDiskService physicalDiskService,
        IDiskImageService diskImageService,
        IImageFilePicker imageFilePicker,
        IWritePlanService writePlanService,
        IWriteConfirmationService writeConfirmationService,
        IWriteOperationService writeOperationService,
        IDeviceEjectService deviceEjectService)
    {
        _physicalDiskService = physicalDiskService;
        _diskImageService = diskImageService;
        _imageFilePicker = imageFilePicker;
        _writePlanService = writePlanService;
        _writeConfirmationService =
            writeConfirmationService;
        _writeOperationService = writeOperationService;
        _deviceEjectService = deviceEjectService;

        RefreshDisks();
    }

    public ObservableCollection<PhysicalDisk> Disks { get; } = [];

    private static string GetVersionText()
    {
        string? informationalVersion = typeof(MainViewModel).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        string version = informationalVersion?
            .Split('+', StringSplitOptions.RemoveEmptyEntries)[0]
            ?? "0.0.0-dev";

        return $"v{version}";
    }

    [ObservableProperty]
    private PhysicalDisk? selectedDisk;

    [ObservableProperty]
    private DiskImage? selectedImage;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private bool isWriting;

    [ObservableProperty]
    private double writeProgress;

    [ObservableProperty]
    private string writeProgressText = string.Empty;

    [ObservableProperty]
    private bool hasCompletedWrite;

    [ObservableProperty]
    private bool hasEjectedTarget;

    public bool CanStart =>
        !IsWriting &&
        !HasCompletedWrite &&
        SelectedDisk?.IsSelectable == true &&
        SelectedImage is not null &&
        SelectedImage.Analysis.IsBootable &&
        SelectedImage.FitsOn(SelectedDisk);

    public bool CanModifySelection => !IsWriting;

    public bool CanEject =>
        HasCompletedWrite &&
        !HasEjectedTarget &&
        SelectedDisk is not null;

    public string StartHint
    {
        get
        {
            if (IsWriting)
            {
                return "Writing in progress. Do not remove the target disk.";
            }

            if (HasCompletedWrite)
            {
                return HasEjectedTarget
                    ? "The target was ejected and can be unplugged safely."
                    : "Verification complete. Eject the target before unplugging it.";
            }

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

            if (!SelectedImage.Analysis.IsBootable)
            {
                return "The selected file does not contain a recognized boot structure.";
            }

            return "Ready to write. All data on the target will be erased.";
        }
    }

    public string SelectedImageName =>
        SelectedImage?.FileName ??
        "No ISO or IMG image selected";

    public string SelectedImageDetails =>
        SelectedImage is null
            ? "Choose a bootable image to continue."
            : $"{SelectedImage.Format} · {SelectedImage.FormattedSize} · {SelectedImage.Analysis.Description}";

    public bool HasSelectedImage => SelectedImage is not null;

    public string SelectedImageFirmware =>
        SelectedImage?.Analysis.FirmwareSupport switch
        {
            BootFirmwareSupport.Bios => "BIOS",
            BootFirmwareSupport.Uefi => "UEFI",
            BootFirmwareSupport.Bios |
                BootFirmwareSupport.Uefi => "BIOS + UEFI",
            _ => "Not bootable"
        };

    public string SelectedImageLayout =>
        SelectedImage?.Analysis switch
        {
            { IsHybridImage: true } => "Hybrid ISO",
            { ImageKind: DiskImageKind.Iso9660 } => "ISO 9660",
            { ImageKind: DiskImageKind.RawDisk } => "Raw disk image",
            _ => "Unknown"
        };

    public string SelectedImagePartitionScheme =>
        SelectedImage?.Analysis.PartitionScheme switch
        {
            DiskPartitionScheme.Mbr => "MBR",
            DiskPartitionScheme.Gpt => "GPT",
            _ => "Image-defined"
        };

    public string SelectedImageFileSystem =>
        SelectedImage?.Analysis.ImageKind switch
        {
            DiskImageKind.Iso9660 => "ISO 9660 (preserved)",
            DiskImageKind.RawDisk => "Preserved from image",
            _ => "Unknown"
        };

    public string SelectedImageWriteMode =>
        SelectedImage is null
            ? "—"
            : "Raw image";

    partial void OnSelectedDiskChanged(PhysicalDisk? value)
    {
        HasCompletedWrite = false;
        HasEjectedTarget = false;
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(StartHint));
    }

    partial void OnSelectedImageChanged(DiskImage? value)
    {
        HasCompletedWrite = false;
        HasEjectedTarget = false;
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(StartHint));
        OnPropertyChanged(nameof(SelectedImageName));
        OnPropertyChanged(nameof(SelectedImageDetails));
        OnPropertyChanged(nameof(HasSelectedImage));
        OnPropertyChanged(nameof(SelectedImageFirmware));
        OnPropertyChanged(nameof(SelectedImageLayout));
        OnPropertyChanged(
            nameof(SelectedImagePartitionScheme));
        OnPropertyChanged(nameof(SelectedImageFileSystem));
        OnPropertyChanged(nameof(SelectedImageWriteMode));
    }

    partial void OnIsWritingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanModifySelection));
        OnPropertyChanged(nameof(StartHint));
    }

    partial void OnHasCompletedWriteChanged(bool value)
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanEject));
        OnPropertyChanged(nameof(StartHint));
    }

    partial void OnHasEjectedTargetChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEject));
        OnPropertyChanged(nameof(StartHint));
    }

    [RelayCommand]
    private async Task StartAsync()
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

            using CancellationTokenSource cancellation = new();
            _writeCancellation = cancellation;
            IsWriting = true;
            WriteProgress = 0;
            WriteProgressText = "Preparing target disk…";
            StatusMessage =
                "Locking volumes and preparing raw disk access.";

            _writeStopwatch.Restart();

            Progress<WriteOperationProgress> progress =
                new(UpdateWriteProgress);

            await _writeOperationService.WriteAsync(
                plan,
                progress,
                cancellation.Token);

            _writeStopwatch.Stop();
            WriteProgress = 100;
            HasCompletedWrite = true;
            StatusMessage =
                "Image written and verified successfully.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage =
                "Write cancelled. The target disk may be incomplete and should not be used.";
        }
        catch (Exception exception)
        {
            StatusMessage =
                $"Unable to start: {exception.Message}";
        }
        finally
        {
            _writeStopwatch.Stop();
            _writeCancellation = null;
            IsWriting = false;
        }
    }

    [RelayCommand]
    private void CancelWrite()
    {
        _writeCancellation?.Cancel();
    }

    [RelayCommand]
    private void EjectTarget()
    {
        if (!CanEject || SelectedDisk is null)
        {
            return;
        }

        try
        {
            _deviceEjectService.Eject(SelectedDisk);
            HasEjectedTarget = true;
            StatusMessage =
                "The target was ejected successfully.";
        }
        catch (Exception exception)
        {
            StatusMessage =
                $"Unable to eject target: {exception.Message}";
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

    private void UpdateWriteProgress(
        WriteOperationProgress update)
    {
        ImageWriteProgress progress = update.Progress;
        WriteProgress = progress.Percentage ?? 0;

        StatusMessage = update.Phase switch
        {
            WriteOperationPhase.Writing =>
                "Writing image to the target disk.",
            WriteOperationPhase.Verifying =>
                "Verifying written data byte by byte.",
            _ => StatusMessage
        };

        string written = FormatBytes(progress.BytesWritten);
        string total = progress.TotalBytes.HasValue
            ? FormatBytes(progress.TotalBytes.Value)
            : "unknown";

        double elapsedSeconds =
            _writeStopwatch.Elapsed.TotalSeconds;

        double bytesPerSecond = elapsedSeconds > 0
            ? progress.BytesWritten / elapsedSeconds
            : 0;

        string speed = bytesPerSecond > 0
            ? $"{FormatBytes((long)bytesPerSecond)}/s"
            : "calculating speed";

        WriteProgressText =
            $"{WriteProgress:0.0}% — {written} of {total} — {speed}";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        int unitIndex = 0;

        while (value >= 1024 &&
               unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }
}
