using BootForge.Core.Enums;

namespace BootForge.Core.Models;

public sealed record DiskSafetyAssessment
{
    public required DiskSafetyStatus Status { get; init; }

    public required bool IsSelectable { get; init; }

    public required string Reason { get; init; }

    public bool IsSystemDisk { get; init; }

    public bool IsBootDisk { get; init; }
}