using BootForge.Infrastructure.Services;

namespace BootForge.Core.Tests.Services;

public sealed class ImageVerifierTests
{
    [Fact]
    public async Task VerifyAsync_IdenticalStreams_Completes()
    {
        byte[] content = Enumerable
            .Range(0, 4096)
            .Select(index => (byte)(index % 251))
            .ToArray();

        await using MemoryStream expected = new(content);
        await using MemoryStream actual = new(content);

        ImageVerifier verifier = new(bufferSize: 257);

        await verifier.VerifyAsync(expected, actual);
    }

    [Fact]
    public async Task VerifyAsync_DifferentByte_Throws()
    {
        byte[] expectedContent = new byte[2048];
        byte[] actualContent = new byte[2048];
        actualContent[1025] = 1;

        await using MemoryStream expected =
            new(expectedContent);

        await using MemoryStream actual =
            new(actualContent);

        ImageVerifier verifier = new(bufferSize: 512);

        InvalidDataException exception =
            await Assert.ThrowsAsync<InvalidDataException>(
                () => verifier.VerifyAsync(
                    expected,
                    actual));

        Assert.Contains(
            "byte offset",
            exception.Message);
    }

    [Fact]
    public async Task VerifyAsync_TruncatedTarget_Throws()
    {
        await using MemoryStream expected =
            new(new byte[2048]);

        await using MemoryStream actual =
            new(new byte[1024]);

        ImageVerifier verifier = new(bufferSize: 512);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => verifier.VerifyAsync(expected, actual));
    }
}
