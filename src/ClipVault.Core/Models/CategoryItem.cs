namespace ClipVault.Core.Models;

/// <summary>
/// Categoría persistida independientemente de los clips. Name se guarda en la
/// forma canónica en mayúsculas para impedir duplicados por formato.
/// </summary>
public sealed class CategoryItem
{
    public string Name { get; set; } = string.Empty;

    // Solo las categorías creadas explícitamente desde ClipVault pueden borrarse.
    // Las importadas de una base existente quedan protegidas, aunque editables.
    public bool IsUserCreated { get; set; }
}
