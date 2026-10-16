using System.Text.RegularExpressions;

namespace ClipVault.Core.Models;

public static partial class CategoryName
{
    private static readonly HashSet<string> LowercaseWords = new(StringComparer.Ordinal)
    {
        "a", "al", "con", "de", "del", "el", "en", "la", "las", "los", "o", "para", "por", "sin", "un", "una", "y"
    };

    /// <summary>
    /// Quita espacios repetidos y convierte el nombre a la forma que se persiste.
    /// Ejemplo: "ComPRAS del    hogar" => "COMPRAS DEL HOGAR".
    /// </summary>
    public static string? NormalizeForStorage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return RepeatedWhitespace().Replace(value.Trim(), " ").ToUpperInvariant();
    }

    /// <summary>
    /// Convierte la forma interna en mayúsculas a una etiqueta legible en la UI.
    /// Ejemplo: "COMPRAS DEL HOGAR" => "Compras del Hogar".
    /// </summary>
    public static string ToDisplay(string? value)
    {
        var normalized = NormalizeForStorage(value);

        if (normalized is null)
        {
            return string.Empty;
        }

        var words = normalized.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (var index = 0; index < words.Length; index++)
        {
            if (index > 0 && LowercaseWords.Contains(words[index]))
            {
                continue;
            }

            words[index] = char.ToUpperInvariant(words[index][0]) + words[index][1..];
        }

        return string.Join(' ', words);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex RepeatedWhitespace();
}
