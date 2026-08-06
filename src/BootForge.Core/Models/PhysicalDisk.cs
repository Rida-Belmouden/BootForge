namespace BootForge.Core.Models;

public sealed record PhysicalDisk
{
    public required int DiskNumber { get; init; }

    public required string DevicePath { get; init; }

    public string? Vendor { get; init; }

    public string? Product { get; init; }

    public string? Revision { get; init; }

    public string? SerialNumber { get; init; }

    public string BusType { get; init; } = "Unknown";

    public long SizeInBytes { get; init; }

    public bool IsRemovableMedia { get; init; }

    public string DisplayName
    {
        get
        {
            string name = string.Join(
                " ",
                new[] { Vendor, Product }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));

            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"Physical Disk {DiskNumber}";
            }

            string state = Safety is null || IsSelectable
                ? string.Empty
                : " — Blocked";

            return $"{name} — Disk {DiskNumber} — {FormattedSize}{state}";
        }
    }

    public string FormattedSize => FormatBytes(SizeInBytes);

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "Unknown size";
        }

        string[] units = ["B", "KB", "MB", "GB", "TB", "PB"];

        double value = bytes;
        int unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }
    public DiskSafetyAssessment? Safety { get; init; }

    public bool IsSelectable => Safety?.IsSelectable == true;
}
