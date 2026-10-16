using CommunityToolkit.Mvvm.ComponentModel;

namespace ClipVault.App.Features.Library;

/// <summary>Datos comunes de un ítem del panel de navegación (filtro o sección).</summary>
public abstract partial class NavItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string label = string.Empty;

    [ObservableProperty]
    private string glyph = string.Empty;

    /// <summary>Color del icono en #RRGGBB; null usa el color de texto atenuado.</summary>
    [ObservableProperty]
    private string? colorHex;

    /// <summary>Cantidad de páginas. Se llenará en la etapa 4.</summary>
    [ObservableProperty]
    private int count;
}

public enum LibraryFilterKind
{
    All,
    Favorites
}

/// <summary>Filtro global: "Todos los elementos" o "Favoritos".</summary>
public sealed partial class FilterNavItemViewModel : NavItemViewModel
{
    public FilterNavItemViewModel(LibraryFilterKind kind, string label, string glyph)
    {
        Kind = kind;
        Label = label;
        Glyph = glyph;
    }

    public LibraryFilterKind Kind { get; }
}
