namespace ClipVault.Core.Models.Library;

/// <summary>Valor de un campo dentro de una página.</summary>
public sealed class FieldValue
{
    public required Guid FieldDefinitionId { get; init; }

    public string Value { get; set; } = string.Empty;
}
