namespace ClipVault.Core.Models.Library;

/// <summary>
/// Sección de la Biblioteca (pestaña vertical estilo OneNote).
/// Modelo provisional: aún no se persiste. Sin dependencias de WPF; el color
/// se guarda como texto hexadecimal y el icono como una clave neutral.
/// </summary>
public sealed class Section
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    /// <summary>Color identificador en formato #RRGGBB.</summary>
    public string ColorHex { get; set; } = "#4F46E5";

    /// <summary>Clave de icono (por ejemplo "chat", "terminal"); la UI la traduce a un glifo.</summary>
    public string IconKey { get; set; } = "folder";

    /// <summary>Atajo rápido opcional (se usará en una etapa posterior).</summary>
    public string? Shortcut { get; set; }

    public int Order { get; set; }
}
