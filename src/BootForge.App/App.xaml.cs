using System.Windows;
using BootForge.App.ViewModels;
using BootForge.Core.Interfaces;
using BootForge.DeviceManagement.Services;

namespace BootForge.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IPhysicalDiskService physicalDiskService =
            new PhysicalDiskService();

        MainViewModel viewModel =
            new(physicalDiskService);

        MainWindow mainWindow = new()
        {
            DataContext = viewModel
        };

        mainWindow.Show();
    }
}