namespace ClipVault.App.Services;

/// <summary>
/// Acceso al portapapeles para la Biblioteca. Interfaz para poder simularlo en
/// pruebas (Moq) y para no llamar a <c>System.Windows.Clipboard</c> desde los ViewModels.
/// </summary>
public interface IClipboardService
{
    /// <summary>Copia texto. Devuelve false si el portapapeles sigue ocupado tras reintentar.</summary>
    bool TrySetText(string text);

    /// <summary>
    /// Copia un dato sensible marcándolo para que el historial de Windows (Win+V), la
    /// nube y los monitores que respetan la convención no lo guarden.
    /// </summary>
    bool TrySetSensitiveText(string text);

    /// <summary>Lee texto (o null si no hay texto o está ocupado). Solo ante una acción explícita del usuario.</summary>
    string? TryGetText();
}
