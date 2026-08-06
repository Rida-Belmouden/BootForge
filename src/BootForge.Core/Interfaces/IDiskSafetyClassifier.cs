using BootForge.Core.Models;

namespace BootForge.Core.Interfaces;

public interface IDiskSafetyClassifier
{
    DiskSafetyAssessment Classify(
        PhysicalDisk disk,
        int? systemDiskNumber,
        int? bootDiskNumber);
}