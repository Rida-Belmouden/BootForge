using BootForge.Core.Enums;

namespace BootForge.Core.Models;

public sealed record DiskImageAnalysis
{
    public static DiskImageAnalysis Unknown { get; } =
        new()
        {
            IsRecognized = false,
            IsBootable = false,
            FirmwareSupport = BootFirmwareSupport.None,
            Description = "Boot structure not recognized"
        };

    public required bool IsRecognized { get; init; }

    public required bool IsBootable { get; init; }

    public required BootFirmwareSupport FirmwareSupport
    {
        get;
        init;
    }

    public required string Description { get; init; }
}
