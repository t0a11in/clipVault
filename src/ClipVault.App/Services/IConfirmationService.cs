namespace ClipVault.App.Services;

/// <summary>Respuesta al intentar salir de una edición con cambios.</summary>
public enum UnsavedChangesDecision
{
    Save,
    Discard,
    ContinueEditing
}

/// <summary>Confirmaciones al usuario (eliminar, copiar datos sensibles, descartar cambios).</summary>
public interface IConfirmationService
{
    bool Confirm(string title, string message, string confirmText);

    /// <summary>Guardar, descartar o seguir editando.</summary>
    UnsavedChangesDecision AskUnsavedChanges(string pageTitle);
}
