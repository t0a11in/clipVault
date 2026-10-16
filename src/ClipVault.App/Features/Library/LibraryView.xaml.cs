using System.Windows.Controls;
using System.Windows.Input;

namespace ClipVault.App.Features.Library;

/// <summary>
/// Vista de la Biblioteca. Solo código visual: la lógica vive en LibraryViewModel.
/// </summary>
public partial class LibraryView : UserControl
{
    public LibraryView()
    {
        InitializeComponent();
    }

    // Ctrl+F o el botón Buscar: mover el foco a la búsqueda de páginas.
    private void OnFindExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        PageSearch.FocusInput();
        e.Handled = true;
    }
}
