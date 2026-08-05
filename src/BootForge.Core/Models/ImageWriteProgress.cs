namespace BootForge.Core.Models;

public sealed record ImageWriteProgress
{
    public required long BytesWritten { get; init; }

    public long? TotalBytes { get; init; }

    public double? Percentage =>
        TotalBytes > 0
            ? Math.Clamp(
                BytesWritten * 100d / TotalBytes.Value,
                0,
                100)
            : null;
}
