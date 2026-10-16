namespace ClipVault.Core.Models.Library;

/// <summary>
/// Definición de un campo de página (Descripción, Comando, Ejemplo…).
/// Modelo provisional: aún no se persiste. Permite agregar campos sin
/// cambiar la vista (el editor los dibuja según <see cref="Type"/>).
/// </summary>
public sealed class FieldDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Clave estable para el código (por ejemplo "command").</summary>
    public required string Key { get; init; }

    public required string Label { get; set; }

    public FieldType Type { get; set; } = FieldType.Text;

    public bool IsRequired { get; set; }

    /// <summary>Su contenido se oculta y nunca se copia ni se registra sin acción explícita.</summary>
    public bool IsSensitive { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>Se incluye al copiar la página.</summary>
    public bool IsCopySource { get; set; }

    /// <summary>Contiene una plantilla con variables ($archivo…).</summary>
    public bool IsCommandTemplate { get; set; }
}
