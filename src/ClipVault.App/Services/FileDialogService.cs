using Microsoft.Win32;

namespace ClipVault.App.Services;

/// <summary>Diálogos nativos de Windows (Microsoft.Win32, incluido en WPF).</summary>
public sealed class FileDialogService : IFileDialogService
{
    public string? PickFile(string title)
    {
        var dialog = new OpenFileDialog { Title = title, CheckFileExists = true, Multiselect = false };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickFolder(string title)
    {
        var dialog = new OpenFolderDialog { Title = title, Multiselect = false };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
