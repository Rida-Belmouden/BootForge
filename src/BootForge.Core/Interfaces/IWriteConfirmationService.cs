using BootForge.Core.Models;

namespace BootForge.Core.Interfaces;

public interface IWriteConfirmationService
{
    bool Confirm(WritePlan plan);
}
