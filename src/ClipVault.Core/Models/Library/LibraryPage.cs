namespace ClipVault.Core.Models.Library;

/// <summary>
/// Página de la Biblioteca (mensaje, comando, prompt o fragmento).
/// Modelo provisional: aún no se persiste.
/// </summary>
public sealed class LibraryPage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Guid SectionId { get; set; }

    public required string Title { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>La página contiene al menos un dato sensible.</summary>
    public bool IsSensitive { get; set; }

    public bool IsFavorite { get; set; }

    public List<FieldValue> Fields { get; init; } = [];

    public string GetValue(FieldDefinition definition) =>
        Fields.FirstOrDefault(field => field.FieldDefinitionId == definition.Id)?.Value ?? string.Empty;
}
