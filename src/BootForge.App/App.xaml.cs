using System.Windows;
using BootForge.App.Services;
using BootForge.App.ViewModels;
using BootForge.Core.Interfaces;
using BootForge.DeviceManagement.Services;
using BootForge.Infrastructure.Services;

namespace BootForge.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ISystemDiskResolver systemDiskResolver =
            new SystemDiskResolver();

        IDiskSafetyClassifier safetyClassifier =
            new DiskSafetyClassifier();

        IPhysicalDiskService physicalDiskService =
            new PhysicalDiskService(
                systemDiskResolver,
                safetyClassifier);

        IDiskImageService diskImageService =
            new DiskImageService();

        IImageFilePicker imageFilePicker =
            new ImageFilePicker();

        MainViewModel viewModel =
            new(
                physicalDiskService,
                diskImageService,
                imageFilePicker);

        MainWindow mainWindow = new()
        {
            DataContext = viewModel
        };

        mainWindow.Show();
    }
}
