using ClipVault.App.Features.Library;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClipVault.App.Shell;

/// <summary>
/// Cáscara de navegación: decide qué área se muestra (Historial o Biblioteca).
/// No conoce el Historial por dentro: el Historial sigue usando MainViewModel
/// sin cambios. Ambas áreas viven a la vez, por eso al volver al Historial se
/// conserva su estado (pestaña, selección, filtro).
/// </summary>
public partial class ShellViewModel : ObservableObject
{
    public ShellViewModel(LibraryViewModel library)
    {
        Library = library;
        Areas =
        [
            new AreaOption(AppArea.History, "Historial", "", "Historial del portapapeles"),
            new AreaOption(AppArea.Library, "Biblioteca", "", "Biblioteca de mensajes y comandos")
        ];
        selectedArea = Areas[0];
    }

    public IReadOnlyList<AreaOption> Areas { get; }

    public LibraryViewModel Library { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHistoryActive))]
    [NotifyPropertyChangedFor(nameof(IsLibraryActive))]
    private AreaOption? selectedArea;

    // Si el selector llegara a enviar null, se trata como Historial (área por defecto).
    public bool IsHistoryActive => SelectedArea?.Area != AppArea.Library;

    public bool IsLibraryActive => SelectedArea?.Area == AppArea.Library;

    [RelayCommand]
    private void ShowHistory() => SelectedArea = Areas[0];

    [RelayCommand]
    private void ShowLibrary() => SelectedArea = Areas[1];
}
