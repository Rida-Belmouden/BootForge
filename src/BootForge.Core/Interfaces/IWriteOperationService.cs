using BootForge.Core.Models;

namespace BootForge.Core.Interfaces;

public interface IWriteOperationService
{
    Task WriteAsync(
        WritePlan plan,
        IProgress<WriteOperationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
