using BootForge.Core.Interfaces;
using BootForge.Core.Models;
using BootForge.DeviceManagement.Native;

namespace BootForge.DeviceManagement.Services;

public sealed class PhysicalDiskService : IPhysicalDiskService
{
    private const int MaximumDiskProbeCount = 64;

    private readonly PhysicalDiskReader _reader = new();
    private readonly ISystemDiskResolver _systemDiskResolver;
    private readonly IDiskSafetyClassifier _safetyClassifier;

    public PhysicalDiskService(
        ISystemDiskResolver systemDiskResolver,
        IDiskSafetyClassifier safetyClassifier)
    {
        _systemDiskResolver = systemDiskResolver;
        _safetyClassifier = safetyClassifier;
    }

    public IReadOnlyList<PhysicalDisk> GetPhysicalDisks()
    {
        List<PhysicalDisk> disks = [];

        int? systemDiskNumber =
            _systemDiskResolver.GetSystemDiskNumber();

        int? bootDiskNumber =
            _systemDiskResolver.GetBootDiskNumber();

        for (int diskNumber = 0;
             diskNumber < MaximumDiskProbeCount;
             diskNumber++)
        {
            PhysicalDisk? detectedDisk =
                _reader.TryRead(diskNumber);

            if (detectedDisk is null)
            {
                continue;
            }

            DiskSafetyAssessment assessment =
                _safetyClassifier.Classify(
                    detectedDisk,
                    systemDiskNumber,
                    bootDiskNumber);

            PhysicalDisk classifiedDisk =
                detectedDisk with
                {
                    Safety = assessment
                };

            disks.Add(classifiedDisk);
        }

        return disks;
    }
}