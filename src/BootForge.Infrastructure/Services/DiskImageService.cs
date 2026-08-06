using BootForge.Core.Interfaces;
using BootForge.Core.Models;

namespace BootForge.Infrastructure.Services;

public sealed class DiskImageService : IDiskImageService
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".iso",
            ".img"
        };

    private readonly IDiskImageAnalyzer _imageAnalyzer;

    public DiskImageService()
        : this(new DiskImageAnalyzer())
    {
    }

    public DiskImageService(
        IDiskImageAnalyzer imageAnalyzer)
    {
        _imageAnalyzer = imageAnalyzer;
    }

    public DiskImage Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string fullPath = Path.GetFullPath(filePath);
        string extension = Path.GetExtension(fullPath);

        if (!SupportedExtensions.Contains(extension))
        {
            throw new NotSupportedException(
                "Only ISO and IMG disk images are supported.");
        }

        FileInfo file = new(fullPath);

        if (!file.Exists)
        {
            throw new FileNotFoundException(
                "The selected disk image no longer exists.",
                fullPath);
        }

        if (file.Length == 0)
        {
            throw new InvalidDataException(
                "The selected disk image is empty.");
        }

        return new DiskImage
        {
            FilePath = file.FullName,
            FileName = file.Name,
            Format = extension.TrimStart('.').ToUpperInvariant(),
            SizeInBytes = file.Length,
            LastModifiedUtc = file.LastWriteTimeUtc,
            Analysis = _imageAnalyzer.Analyze(
                file.FullName,
                extension.TrimStart('.'))
        };
    }
}
