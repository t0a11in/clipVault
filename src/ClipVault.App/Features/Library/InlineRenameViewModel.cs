using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClipVault.App.Features.Library;

/// <summary>
/// Estado reutilizable de "renombrar en línea" (secciones y páginas).
/// Enter → <see cref="CommitCommand"/>, Escape → <see cref="CancelCommand"/>,
/// perder el foco → <see cref="FinishCommand"/> (confirma si es válido, si no cancela).
/// </summary>
public sealed partial class InlineRenameViewModel : ObservableObject
{
    private readonly Func<string> _currentName;
    private readonly Action<string> _apply;
    private readonly Action _onCancelled;

    /// <param name="currentName">Nombre actual (para iniciar la edición).</param>
    /// <param name="apply">Aplica el nombre confirmado (ya sin espacios sobrantes).</param>
    /// <param name="onCancelled">Se llama al cancelar (por ejemplo, para descartar un elemento nuevo).</param>
    public InlineRenameViewModel(Func<string> currentName, Action<string> apply, Action onCancelled)
    {
        _currentName = currentName;
        _apply = apply;
        _onCancelled = onCancelled;
    }

    [ObservableProperty]
    private bool isEditing;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CommitCommand))]
    private string editName = string.Empty;

    public void Begin()
    {
        EditName = _currentName();
        IsEditing = true;
    }

    private bool CanCommit() => !string.IsNullOrWhiteSpace(EditName);

    [RelayCommand(CanExecute = nameof(CanCommit))]
    private void Commit()
    {
        if (!IsEditing)
        {
            return;
        }

        IsEditing = false;
        _apply(EditName.Trim());
    }

    [RelayCommand]
    private void Cancel()
    {
        if (!IsEditing)
        {
            return;
        }

        IsEditing = false;
        _onCancelled();
    }

    [RelayCommand]
    private void Finish()
    {
        if (CanCommit())
        {
            Commit();
        }
        else
        {
            Cancel();
        }
    }
}
