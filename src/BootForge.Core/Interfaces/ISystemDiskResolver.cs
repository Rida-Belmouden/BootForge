namespace BootForge.Core.Interfaces;

public interface ISystemDiskResolver
{
    int? GetSystemDiskNumber();

    int? GetBootDiskNumber();
}