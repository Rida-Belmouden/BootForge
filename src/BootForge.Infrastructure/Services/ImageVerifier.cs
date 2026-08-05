using System.Buffers;
using BootForge.Core.Interfaces;
using BootForge.Core.Models;

namespace BootForge.Infrastructure.Services;

public sealed class ImageVerifier : IImageVerifier
{
    private const int DefaultBufferSize = 1024 * 1024;

    private readonly int _bufferSize;

    public ImageVerifier(int bufferSize = DefaultBufferSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            bufferSize);

        _bufferSize = bufferSize;
    }

    public async Task VerifyAsync(
        Stream expected,
        Stream actual,
        IProgress<ImageWriteProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        if (!expected.CanRead || !actual.CanRead)
        {
            throw new ArgumentException(
                "Both verification streams must be readable.");
        }

        long? totalBytes = expected.CanSeek
            ? expected.Length - expected.Position
            : null;

        byte[] expectedBuffer =
            ArrayPool<byte>.Shared.Rent(_bufferSize);

        byte[] actualBuffer =
            ArrayPool<byte>.Shared.Rent(_bufferSize);

        long verifiedBytes = 0;

        try
        {
            while (true)
            {
                int expectedCount = await expected.ReadAsync(
                    expectedBuffer.AsMemory(0, _bufferSize),
                    cancellationToken);

                if (expectedCount == 0)
                {
                    break;
                }

                int actualCount = await ReadExactlyAsync(
                    actual,
                    actualBuffer,
                    expectedCount,
                    cancellationToken);

                if (actualCount != expectedCount ||
                    !expectedBuffer
                        .AsSpan(0, expectedCount)
                        .SequenceEqual(
                            actualBuffer.AsSpan(
                                0,
                                actualCount)))
                {
                    throw new InvalidDataException(
                        $"Verification failed near byte offset {verifiedBytes}.");
                }

                verifiedBytes += expectedCount;
                progress?.Report(
                    new ImageWriteProgress
                    {
                        BytesWritten = verifiedBytes,
                        TotalBytes = totalBytes
                    });
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(expectedBuffer);
            ArrayPool<byte>.Shared.Return(actualBuffer);
        }
    }

    private static async Task<int> ReadExactlyAsync(
        Stream stream,
        byte[] buffer,
        int count,
        CancellationToken cancellationToken)
    {
        int totalRead = 0;

        while (totalRead < count)
        {
            int bytesRead = await stream.ReadAsync(
                buffer.AsMemory(
                    totalRead,
                    count - totalRead),
                cancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            totalRead += bytesRead;
        }

        return totalRead;
    }
}
