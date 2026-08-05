namespace BootForge.Core.Models;

public sealed record StorageDevice
{
    public required string Name { get; init; }

    public required string RootDirectory { get; init; }

    public string? VolumeLabel { get; init; }

    public string? FileSystem { get; init; }

    public long TotalSize { get; init; }

    public long AvailableFreeSpace { get; init; }

    public bool IsReady { get; init; }

    public bool IsRemovable { get; init; }

    public string DisplayName
    {
        get
        {
            string label = string.IsNullOrWhiteSpace(VolumeLabel)
                ? "Removable drive"
                : VolumeLabel;

            return $"{label} ({RootDirectory}) - {FormattedTotalSize}";
        }
    }

    public string FormattedTotalSize => FormatBytes(TotalSize);

    public string FormattedAvailableSpace => FormatBytes(AvailableFreeSpace);

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];

        double size = bytes;
        int unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }
}