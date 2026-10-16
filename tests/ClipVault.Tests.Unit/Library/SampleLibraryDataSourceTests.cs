using System.Text.RegularExpressions;
using ClipVault.App.Features.Library.Data;

namespace ClipVault.Tests.Unit.Library;

/// <summary>
/// Protege los datos de ejemplo de la maqueta: no deben contener secretos reales
/// y cada página debe pertenecer a una sección existente.
/// </summary>
public sealed class SampleLibraryDataSourceTests
{
    private readonly SampleLibraryDataSource _source = new();

    [Fact]
    public void SensitiveValues_AreOnlyPlaceholders()
    {
        // Un valor sensible de ejemplo solo puede ser un marcador "${SECRET:NOMBRE}".
        var placeholder = new Regex(@"^\$\{SECRET:[A-Z0-9_]+\}$");
        var sensitiveIds = _source.GetFieldDefinitions().Where(d => d.IsSensitive).Select(d => d.Id).ToHashSet();

        var sensitiveValues = _source.GetPages()
            .SelectMany(p => p.Fields)
            .Where(f => sensitiveIds.Contains(f.FieldDefinitionId))
            .Select(f => f.Value)
            .ToList();

        Assert.NotEmpty(sensitiveValues); // la maqueta debe mostrar al menos un caso sensible
        Assert.All(sensitiveValues, value => Assert.Matches(placeholder, value));
    }

    [Fact]
    public void EveryPage_BelongsToAnExistingSection()
    {
        var sectionIds = _source.GetSections().Select(s => s.Id).ToHashSet();

        Assert.All(_source.GetPages(), page => Assert.Contains(page.SectionId, sectionIds));
    }
}
