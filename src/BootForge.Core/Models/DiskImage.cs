namespace BootForge.Core.Models;

public sealed record DiskImage
{
    public required string FilePath { get; init; }

    public required string FileName { get; init; }

    public required string Format { get; init; }

    public required long SizeInBytes { get; init; }

    public DateTime LastModifiedUtc { get; init; }

    public DiskImageAnalysis Analysis { get; init; } =
        DiskImageAnalysis.Unknown;

    public string FormattedSize => FormatBytes(SizeInBytes);

    public bool FitsOn(PhysicalDisk disk)
    {
        ArgumentNullException.ThrowIfNull(disk);

        return SizeInBytes <= disk.SizeInBytes;
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];

        double value = bytes;
        int unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }
}
