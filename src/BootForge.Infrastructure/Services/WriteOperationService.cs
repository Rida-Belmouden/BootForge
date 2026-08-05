using BootForge.Core.Interfaces;
using BootForge.Core.Models;

namespace BootForge.Infrastructure.Services;

public sealed class WriteOperationService
    : IWriteOperationService
{
    private const int SourceBufferSize = 1024 * 1024;

    private readonly IVolumeLockService _volumeLockService;
    private readonly IWritePlanService _writePlanService;
    private readonly IRawDiskStreamFactory
        _rawDiskStreamFactory;
    private readonly IImageWriter _imageWriter;

    public WriteOperationService(
        IVolumeLockService volumeLockService,
        IWritePlanService writePlanService,
        IRawDiskStreamFactory rawDiskStreamFactory,
        IImageWriter imageWriter)
    {
        _volumeLockService = volumeLockService;
        _writePlanService = writePlanService;
        _rawDiskStreamFactory = rawDiskStreamFactory;
        _imageWriter = imageWriter;
    }

    public async Task WriteAsync(
        WritePlan plan,
        IProgress<ImageWriteProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        cancellationToken.ThrowIfCancellationRequested();

        using IVolumeLock volumeLock =
            _volumeLockService.AcquireForDisk(
                plan.TargetDisk.DiskNumber);

        // Revalidate while the target volumes are locked so a disk
        // swapped after confirmation can never receive image data.
        WritePlan verifiedPlan = _writePlanService.Create(
            plan.Image,
            plan.TargetDisk);

        await using FileStream source = new(
            verifiedPlan.Image.FilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            SourceBufferSize,
            FileOptions.Asynchronous |
            FileOptions.SequentialScan);

        await using Stream destination =
            _rawDiskStreamFactory.OpenWrite(
                verifiedPlan.TargetDisk);

        await _imageWriter.WriteAsync(
            source,
            destination,
            progress,
            cancellationToken);
    }
}
