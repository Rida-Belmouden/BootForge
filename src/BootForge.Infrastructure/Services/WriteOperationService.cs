using BootForge.Core.Interfaces;
using BootForge.Core.Models;
using BootForge.Core.Enums;

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
    private readonly IImageVerifier _imageVerifier;
    private readonly IDiskPropertyUpdater _diskPropertyUpdater;

    public WriteOperationService(
        IVolumeLockService volumeLockService,
        IWritePlanService writePlanService,
        IRawDiskStreamFactory rawDiskStreamFactory,
        IImageWriter imageWriter,
        IImageVerifier imageVerifier,
        IDiskPropertyUpdater diskPropertyUpdater)
    {
        _volumeLockService = volumeLockService;
        _writePlanService = writePlanService;
        _rawDiskStreamFactory = rawDiskStreamFactory;
        _imageWriter = imageWriter;
        _imageVerifier = imageVerifier;
        _diskPropertyUpdater = diskPropertyUpdater;
    }

    public async Task WriteAsync(
        WritePlan plan,
        IProgress<WriteOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        cancellationToken.ThrowIfCancellationRequested();

        PhysicalDisk completedDisk;

        using (IVolumeLock volumeLock =
               _volumeLockService.AcquireForDisk(
                   plan.TargetDisk.DiskNumber))
        {
            // Revalidate while the target volumes are locked so a
            // disk swapped after confirmation receives no image data.
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
                CreatePhaseProgress(
                    progress,
                    WriteOperationPhase.Writing),
                cancellationToken);

            if (!source.CanSeek || !destination.CanSeek)
            {
                throw new NotSupportedException(
                    "The target disk cannot be reopened for verification.");
            }

            source.Seek(0, SeekOrigin.Begin);
            destination.Seek(0, SeekOrigin.Begin);

            await _imageVerifier.VerifyAsync(
                source,
                destination,
                CreatePhaseProgress(
                    progress,
                    WriteOperationPhase.Verifying),
                cancellationToken);

            completedDisk = verifiedPlan.TargetDisk;
        }

        _diskPropertyUpdater.Update(completedDisk);
    }

    private static IProgress<ImageWriteProgress>?
        CreatePhaseProgress(
            IProgress<WriteOperationProgress>? progress,
            WriteOperationPhase phase)
    {
        return progress is null
            ? null
            : new InlineProgress<ImageWriteProgress>(
                update => progress.Report(
                    new WriteOperationProgress
                    {
                        Phase = phase,
                        Progress = update
                    }));
    }

    private sealed class InlineProgress<T>(
        Action<T> callback) : IProgress<T>
    {
        public void Report(T value)
        {
            callback(value);
        }
    }
}
