using BootForge.Core.Models;

namespace BootForge.Core.Interfaces;

public interface IImageVerifier
{
    Task VerifyAsync(
        Stream expected,
        Stream actual,
        IProgress<ImageWriteProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
