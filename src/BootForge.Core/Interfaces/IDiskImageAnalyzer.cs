using BootForge.Core.Models;

namespace BootForge.Core.Interfaces;

public interface IDiskImageAnalyzer
{
    DiskImageAnalysis Analyze(
        string filePath,
        string format);
}
