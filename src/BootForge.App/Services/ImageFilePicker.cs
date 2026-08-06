using BootForge.Core.Interfaces;
using Microsoft.Win32;

namespace BootForge.App.Services;

public sealed class ImageFilePicker : IImageFilePicker
{
    public string? PickImageFile()
    {
        OpenFileDialog dialog = new()
        {
            Title = "Select a bootable disk image",
            Filter =
                "Disk images (*.iso;*.img)|*.iso;*.img|" +
                "ISO images (*.iso)|*.iso|" +
                "Raw disk images (*.img)|*.img",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }
}
