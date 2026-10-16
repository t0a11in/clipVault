using ClipVault.Core.Models.Library;

namespace ClipVault.Core.Interfaces;

/// <summary>
/// Origen de datos de la Biblioteca. En la maqueta lo implementa una fuente en
/// memoria; en la etapa de persistencia se implementará en Infrastructure
/// sin cambiar los ViewModels.
/// </summary>
public interface ILibraryDataSource
{
    IReadOnlyList<Section> GetSections();

    /// <summary>Campos disponibles para las páginas, ordenados por DisplayOrder.</summary>
    IReadOnlyList<FieldDefinition> GetFieldDefinitions();

    IReadOnlyList<LibraryPage> GetPages();
}
