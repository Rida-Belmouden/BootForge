using BootForge.Core.Models;
using BootForge.Infrastructure.Services;

namespace BootForge.Core.Tests.Services;

public sealed class ImageWriterTests
{
    [Fact]
    public async Task WriteAsync_CopiesAllBytesAndReportsCompletion()
    {
        byte[] content = Enumerable
            .Range(0, 8192)
            .Select(index => (byte)(index % 251))
            .ToArray();

        await using MemoryStream source = new(content);
        await using MemoryStream destination = new();
        List<ImageWriteProgress> updates = [];

        ImageWriter writer = new(bufferSize: 1024);

        await writer.WriteAsync(
            source,
            destination,
            new InlineProgress<ImageWriteProgress>(
                updates.Add));

        Assert.Equal(content, destination.ToArray());
        Assert.NotEmpty(updates);
        Assert.Equal(0, updates[0].BytesWritten);
        Assert.Equal(content.Length, updates[^1].BytesWritten);
        Assert.Equal(100, updates[^1].Percentage);
    }

    [Fact]
    public async Task WriteAsync_CancellationStopsBeforeCompletion()
    {
        byte[] content = new byte[8192];

        await using MemoryStream source = new(content);
        await using MemoryStream destination = new();
        using CancellationTokenSource cancellation = new();

        ImageWriter writer = new(bufferSize: 1024);

        InlineProgress<ImageWriteProgress> progress =
            new(update =>
            {
                if (update.BytesWritten >= 1024)
                {
                    cancellation.Cancel();
                }
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => writer.WriteAsync(
                source,
                destination,
                progress,
                cancellation.Token));

        Assert.True(destination.Length < content.Length);
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
