using System.Buffers;
using BootForge.Core.Interfaces;
using BootForge.Core.Models;

namespace BootForge.Infrastructure.Services;

public sealed class ImageWriter : IImageWriter
{
    private const int DefaultBufferSize = 1024 * 1024;

    private readonly int _bufferSize;

    public ImageWriter(int bufferSize = DefaultBufferSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            bufferSize);

        _bufferSize = bufferSize;
    }

    public async Task WriteAsync(
        Stream source,
        Stream destination,
        IProgress<ImageWriteProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        if (!source.CanRead)
        {
            throw new ArgumentException(
                "The source stream must be readable.",
                nameof(source));
        }

        if (!destination.CanWrite)
        {
            throw new ArgumentException(
                "The destination stream must be writable.",
                nameof(destination));
        }

        long? totalBytes = source.CanSeek
            ? source.Length - source.Position
            : null;

        long bytesWritten = 0;
        ReportProgress(progress, bytesWritten, totalBytes);

        byte[] buffer =
            ArrayPool<byte>.Shared.Rent(_bufferSize);

        try
        {
            while (true)
            {
                int bytesRead = await source.ReadAsync(
                    buffer.AsMemory(0, _bufferSize),
                    cancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                await destination.WriteAsync(
                    buffer.AsMemory(0, bytesRead),
                    cancellationToken);

                bytesWritten += bytesRead;
                ReportProgress(
                    progress,
                    bytesWritten,
                    totalBytes);
            }

            await destination.FlushAsync(cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void ReportProgress(
        IProgress<ImageWriteProgress>? progress,
        long bytesWritten,
        long? totalBytes)
    {
        progress?.Report(
            new ImageWriteProgress
            {
                BytesWritten = bytesWritten,
                TotalBytes = totalBytes
            });
    }
}
