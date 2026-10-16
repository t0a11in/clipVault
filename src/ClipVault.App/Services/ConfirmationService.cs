using System.Windows;

namespace ClipVault.App.Services;

/// <summary>
/// Confirmaciones con el cuadro de mensaje nativo. Esc o cerrar equivale a la opción
/// más segura ("No" o "Seguir editando"). MessageBox no permite personalizar los botones,
/// por eso el mensaje explica qué hace cada uno; un diálogo propio puede reemplazarlo
/// sin cambiar los ViewModels.
/// </summary>
public sealed class ConfirmationService : IConfirmationService
{
    public bool Confirm(string title, string message, string confirmText)
    {
        var text = $"{message}\n\nPulsa «Sí» para {confirmText.ToLowerInvariant()}.";
        return Show(text, title, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
    }

    public UnsavedChangesDecision AskUnsavedChanges(string pageTitle)
    {
        var text = $"«{pageTitle}» tiene cambios sin guardar.\n\n" +
                   "Sí: guardar los cambios\nNo: descartarlos\nCancelar: seguir editando";
        return Show(text, "Cambios sin guardar", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) switch
        {
            MessageBoxResult.Yes => UnsavedChangesDecision.Save,
            MessageBoxResult.No => UnsavedChangesDecision.Discard,
            _ => UnsavedChangesDecision.ContinueEditing
        };
    }

    private static MessageBoxResult Show(
        string text, string title, MessageBoxButton buttons, MessageBoxImage image, MessageBoxResult defaultResult)
    {
        var owner = Application.Current?.MainWindow;
        return owner is null
            ? MessageBox.Show(text, title, buttons, image, defaultResult)
            : MessageBox.Show(owner, text, title, buttons, image, defaultResult);
    }
}
