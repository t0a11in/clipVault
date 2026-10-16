namespace ClipVault.App.Features.Library;

/// <summary>
/// Traduce claves de icono neutrales (del modelo) a glifos de
/// Segoe Fluent Icons / Segoe MDL2 Assets. Un único lugar para cambiar iconos.
/// </summary>
public static class LibraryIcons
{
    public const string All = "";
    public const string Favorites = "";
    public const string Add = "";
    public const string Folder = "";

    private static readonly Dictionary<string, string> Glyphs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chat"] = "",
        ["terminal"] = "",
        ["document"] = "",
        ["code"] = "",
        ["folder"] = Folder
    };

    public static string FromKey(string? key) =>
        key is not null && Glyphs.TryGetValue(key, out var glyph) ? glyph : Folder;
}
