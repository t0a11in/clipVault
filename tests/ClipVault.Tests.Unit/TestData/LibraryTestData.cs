using ClipVault.App.Features.Library;
using ClipVault.Core.Models.Library;

namespace ClipVault.Tests.Unit.TestData;

/// <summary>
/// Datos de prueba de la Biblioteca, armados a mano para que cada prueba sea
/// independiente de los datos de ejemplo de la app (SampleLibraryDataSource):
/// si alguien cambia los ejemplos, las pruebas no se rompen por motivos ajenos.
/// </summary>
internal static class LibraryTestData
{
    // Definiciones de campo con la misma forma que las reales (descripción, comando y secreto).
    public static readonly FieldDefinition Description = new()
    {
        Key = "description",
        Label = "Descripción",
        Type = FieldType.MultilineText,
        DisplayOrder = 0
    };

    public static readonly FieldDefinition Command = new()
    {
        Key = "command",
        Label = "Comando",
        Type = FieldType.Code,
        DisplayOrder = 1,
        IsCommandTemplate = true
    };

    public static readonly FieldDefinition Secret = new()
    {
        Key = "secret",
        Label = "Token de API",
        Type = FieldType.Secret,
        DisplayOrder = 2,
        IsSensitive = true
    };

    public static IReadOnlyList<FieldDefinition> AllFields { get; } = [Description, Command, Secret];

    /// <summary>
    /// Crea el ViewModel de una página con los valores indicados.
    /// Los valores nulos se omiten (el campo no existe en la página).
    /// </summary>
    public static PageListItemViewModel Page(
        string title,
        string? description = null,
        string? command = null,
        string? secret = null)
    {
        var fields = new List<FieldValue>();
        Add(fields, Description, description);
        Add(fields, Command, command);
        Add(fields, Secret, secret);

        var model = new LibraryPage
        {
            SectionId = Guid.NewGuid(),
            Title = title,
            IsSensitive = secret is not null,
            Fields = fields
        };

        // El tercer parámetro es la acción para descartar una página nueva; en estas pruebas no se usa.
        return new PageListItemViewModel(model, AllFields, onDiscardNew: _ => { });
    }

    private static void Add(List<FieldValue> fields, FieldDefinition definition, string? value)
    {
        if (value is not null)
        {
            fields.Add(new FieldValue { FieldDefinitionId = definition.Id, Value = value });
        }
    }
}
