using System.Windows;
using BootForge.Core.Interfaces;
using BootForge.Core.Models;

namespace BootForge.App.Services;

public sealed class WriteConfirmationService
    : IWriteConfirmationService
{
    public bool Confirm(WritePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        string diskName =
            string.IsNullOrWhiteSpace(plan.TargetDisk.Product)
                ? $"Disk {plan.TargetDisk.DiskNumber}"
                : plan.TargetDisk.Product;

        string message =
            "All data on the selected target will be permanently erased." +
            Environment.NewLine +
            Environment.NewLine +
            $"Image: {plan.Image.FileName} ({plan.Image.FormattedSize})" +
            Environment.NewLine +
            $"Target: {diskName} — Disk {plan.TargetDisk.DiskNumber}" +
            Environment.NewLine +
            $"Capacity: {plan.TargetDisk.FormattedSize}" +
            Environment.NewLine +
            Environment.NewLine +
            "Do you want to continue?";

        MessageBoxResult result = MessageBox.Show(
            message,
            "BootForge — Confirm disk write",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        return result == MessageBoxResult.Yes;
    }
}
