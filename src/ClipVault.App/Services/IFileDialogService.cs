namespace ClipVault.App.Services;

/// <summary>Selectores de archivo y carpeta, detrás de una interfaz para los ViewModels.</summary>
public interface IFileDialogService
{
    /// <summary>Ruta elegida o null si se canceló.</summary>
    string? PickFile(string title);

    /// <summary>Ruta elegida o null si se canceló.</summary>
    string? PickFolder(string title);
}
