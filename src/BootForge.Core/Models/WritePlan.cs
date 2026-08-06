namespace BootForge.Core.Models;

public sealed record WritePlan
{
    public required DiskImage Image { get; init; }

    public required PhysicalDisk TargetDisk { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
