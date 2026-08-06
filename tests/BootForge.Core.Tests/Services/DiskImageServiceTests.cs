using BootForge.Core.Models;
using BootForge.Infrastructure.Services;

namespace BootForge.Core.Tests.Services;

public sealed class DiskImageServiceTests
{
    private readonly DiskImageService _service = new();

    [Theory]
    [InlineData(".iso", "ISO")]
    [InlineData(".IMG", "IMG")]
    public void Load_SupportedImage_ReturnsMetadata(
        string extension,
        string expectedFormat)
    {
        string filePath = CreateTemporaryImage(
            extension,
            length: 2048);

        try
        {
            DiskImage image = _service.Load(filePath);

            Assert.Equal(
                Path.GetFullPath(filePath),
                image.FilePath);

            Assert.Equal(
                Path.GetFileName(filePath),
                image.FileName);

            Assert.Equal(expectedFormat, image.Format);
            Assert.Equal(2048, image.SizeInBytes);
            Assert.Equal("2 KB", image.FormattedSize);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_UnsupportedExtension_Throws()
    {
        NotSupportedException exception =
            Assert.Throws<NotSupportedException>(
                () => _service.Load("image.zip"));

        Assert.Contains(
            "ISO and IMG",
            exception.Message);
    }

    [Fact]
    public void Load_MissingImage_Throws()
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.iso");

        Assert.Throws<FileNotFoundException>(
            () => _service.Load(filePath));
    }

    [Fact]
    public void Load_EmptyImage_Throws()
    {
        string filePath = CreateTemporaryImage(
            ".img",
            length: 0);

        try
        {
            Assert.Throws<InvalidDataException>(
                () => _service.Load(filePath));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateTemporaryImage(
        string extension,
        int length)
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}{extension}");

        File.WriteAllBytes(
            filePath,
            new byte[length]);

        return filePath;
    }
}
