using System.Runtime.InteropServices;
using System.Windows;

namespace ClipVault.App.Services;

/// <summary>
/// Implementación con el portapapeles de WPF (hilo STA de la UI).
/// Otro programa puede tener el portapapeles abierto un instante: se reintenta
/// unas pocas veces antes de informar el fallo. Nunca registra el contenido.
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    private const int Attempts = 4;
    private const int DelayMilliseconds = 40;

    // Formatos documentados por Windows para excluir contenido del historial,
    // de la sincronización en la nube y de los monitores de portapapeles.
    private const string ExcludeFromMonitors = "ExcludeClipboardContentFromMonitorProcessing";
    private const string CanIncludeInHistory = "CanIncludeInClipboardHistory";
    private const string CanUploadToCloud = "CanUploadToCloudClipboard";

    public bool TrySetText(string text) => TrySet(() => text);

    public bool TrySetSensitiveText(string text) => TrySet(() =>
    {
        var data = new DataObject();
        data.SetText(text);
        data.SetData(ExcludeFromMonitors, new System.IO.MemoryStream([0, 0, 0, 0]));
        data.SetData(CanIncludeInHistory, new System.IO.MemoryStream([0, 0, 0, 0]));
        data.SetData(CanUploadToCloud, new System.IO.MemoryStream([0, 0, 0, 0]));
        return data;
    });

    private static bool TrySet(Func<object> createData)
    {
        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            try
            {
                Clipboard.SetDataObject(createData(), copy: true);
                return true;
            }
            catch (COMException) when (attempt < Attempts)
            {
                Thread.Sleep(DelayMilliseconds);
            }
            catch (COMException)
            {
                return false;
            }
        }

        return false;
    }

    public string? TryGetText()
    {
        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            try
            {
                return Clipboard.ContainsText() ? Clipboard.GetText() : null;
            }
            catch (COMException) when (attempt < Attempts)
            {
                Thread.Sleep(DelayMilliseconds);
            }
            catch (COMException)
            {
                return null;
            }
        }

        return null;
    }
}
