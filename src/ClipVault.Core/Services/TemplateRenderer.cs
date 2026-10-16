using System.Text.RegularExpressions;

namespace ClipVault.Core.Services;

/// <summary>
/// Renderiza plantillas de comando con variables <c>$nombre</c>.
/// Lógica pura (sin WPF ni E/S): se puede probar con xUnit sin dependencias.
/// </summary>
/// <remarks>
/// Reglas:
/// <list type="number">
/// <item>Solo se reemplazan las variables presentes en <c>values</c>; cualquier otro
/// <c>$x</c> (por ejemplo <c>$PWD</c> o <c>$backup</c> de PowerShell) queda intacto.</item>
/// <item>Coincidencia exacta del nombre: <c>$archivo</c> no altera <c>$archivo2</c>.</item>
/// <item>Una sola pasada: si un valor contiene <c>$algo</c>, no se vuelve a expandir.</item>
/// <item>No ejecuta nada: solo produce texto.</item>
/// </list>
/// </remarks>
public static partial class TemplateRenderer
{
    /// <summary>Variables previstas por la especificación.</summary>
    public static readonly IReadOnlyList<string> PlannedVariables = ["archivo", "texto", "ruta", "nombre", "fecha"];

    /// <summary>Variables implementadas: todas las previstas.</summary>
    public static readonly IReadOnlyList<string> SupportedVariables = PlannedVariables;

    [GeneratedRegex(@"\$([A-Za-z_][A-Za-z0-9_]*)")]
    private static partial Regex VariablePattern();

    /// <summary>Nombres de variables usados en la plantilla, sin repetir y en orden de aparición.</summary>
    public static IReadOnlyList<string> ExtractVariables(string? template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return [];
        }

        return VariablePattern()
            .Matches(template)
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>¿La plantilla usa alguna variable implementada?</summary>
    public static bool UsesSupportedVariables(string? template) =>
        ExtractVariables(template).Any(name => SupportedVariables.Contains(name, StringComparer.Ordinal));

    /// <summary>Reemplaza las variables conocidas por su valor.</summary>
    public static string Render(string? template, IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        return VariablePattern().Replace(template, match =>
            values.TryGetValue(match.Groups[1].Value, out var value) ? value : match.Value);
    }

    /// <summary>
    /// Deduce el valor de una variable comparando la plantilla con un texto ya renderizado
    /// (por ejemplo, el campo "Ejemplo"): <c>repopack "$archivo"</c> + <c>repopack "D:\x\"</c>
    /// → <c>archivo = D:\x\</c>. Devuelve false si el texto no corresponde a la plantilla.
    /// </summary>
    public static bool TryExtractValue(string? template, string? rendered, string variableName, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrEmpty(template) || string.IsNullOrEmpty(rendered))
        {
            return false;
        }

        var pattern = new System.Text.StringBuilder("^");
        var last = 0;
        var captured = false;
        foreach (Match match in VariablePattern().Matches(template))
        {
            pattern.Append(Regex.Escape(template[last..match.Index]));
            var name = match.Groups[1].Value;
            if (name == variableName)
            {
                // Primera aparición: se captura; las siguientes deben repetir el mismo valor.
                pattern.Append(captured ? @"\k<value>" : "(?<value>.+?)");
                captured = true;
            }
            else
            {
                pattern.Append(".*?");
            }

            last = match.Index + match.Length;
        }

        pattern.Append(Regex.Escape(template[last..])).Append('$');
        if (!captured)
        {
            return false;
        }

        var result = Regex.Match(rendered.Trim(), pattern.ToString(), RegexOptions.Singleline, TimeSpan.FromMilliseconds(200));
        if (!result.Success)
        {
            return false;
        }

        value = result.Groups["value"].Value;
        return value.Length > 0;
    }
}
