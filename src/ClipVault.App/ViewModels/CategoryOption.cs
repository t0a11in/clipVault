using ClipVault.Core.Models;

namespace ClipVault.App.ViewModels;

/// <summary>
/// Separa el valor canónico persistido de la etiqueta amigable mostrada al usuario.
/// </summary>
public sealed record CategoryOption(string StorageValue, bool IsUserCreated = false)
{
    public string DisplayName => CategoryName.ToDisplay(StorageValue);
}
