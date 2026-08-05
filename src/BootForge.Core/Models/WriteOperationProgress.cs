using BootForge.Core.Enums;

namespace BootForge.Core.Models;

public sealed record WriteOperationProgress
{
    public required WriteOperationPhase Phase { get; init; }

    public required ImageWriteProgress Progress { get; init; }
}
