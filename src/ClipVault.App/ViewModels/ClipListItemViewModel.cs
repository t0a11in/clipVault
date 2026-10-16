using ClipVault.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClipVault.App.ViewModels;

/// <summary>
/// Estado de presentación de un clip. La casilla de eliminación pertenece a la UI,
/// por eso no se persiste en la entidad ClipItem ni en SQLite.
/// </summary>
public sealed partial class ClipListItemViewModel(ClipItem clip) : ObservableObject
{
    public ClipItem Clip { get; } = clip;

    public long Id => Clip.Id;

    public ClipType Type => Clip.Type;

    public string? Title => Clip.Title;

    // En SQLite se mantiene "COMPRAS DEL HOGAR"; la tabla muestra "Compras del Hogar".
    public string Category => CategoryName.ToDisplay(Clip.Category);

    public bool IsPermanent => Clip.IsPermanent;

    public DateTime CreatedAt => Clip.CreatedAt;

    public byte[]? ImageData => Clip.ImageData;

    public string DisplayText => Clip.DisplayText;

    public void UpdateText(string content)
    {
        Clip.Content = content;
        OnPropertyChanged(nameof(DisplayText));
    }

    [ObservableProperty]
    private bool isMarkedForDeletion;
}
