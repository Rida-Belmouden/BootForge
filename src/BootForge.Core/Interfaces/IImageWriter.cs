using BootForge.Core.Models;

namespace BootForge.Core.Interfaces;

public interface IImageWriter
{
    Task WriteAsync(
        Stream source,
        Stream destination,
        IProgress<ImageWriteProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
