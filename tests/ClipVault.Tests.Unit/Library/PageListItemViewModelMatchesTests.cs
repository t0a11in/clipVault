using ClipVault.Tests.Unit.TestData;

namespace ClipVault.Tests.Unit.Library;

/// <summary>
/// Pruebas de <c>PageListItemViewModel.Matches</c>: la búsqueda de páginas.
///
/// Qué se garantiza (prioridad alta, por privacidad):
/// 1. Busca en el título y en el contenido NO sensible.
/// 2. No distingue mayúsculas ni tildes ("revision" encuentra "Revisión").
/// 3. NUNCA encuentra una página por el texto de un campo sensible.
///
/// Patrón usado en cada prueba: Arrange (preparar) → Act (ejecutar) → Assert (verificar).
/// Nombres: Método_Escenario_ResultadoEsperado, para que el reporte se lea solo.
/// </summary>
public sealed class PageListItemViewModelMatchesTests
{
    // [Theory] + [InlineData]: la misma prueba se ejecuta una vez por cada fila de datos.
    // Sirve para cubrir varios casos equivalentes sin copiar código.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Matches_EmptyQuery_ReturnsTrue(string? query)
    {
        // Arrange: una búsqueda vacía no debe ocultar páginas.
        var page = LibraryTestData.Page("IA Clonar");

        // Act
        var result = page.Matches(query!);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("IA Clonar")]   // texto exacto
    [InlineData("ia clonar")]   // minúsculas
    [InlineData("CLONAR")]      // mayúsculas y solo una parte
    [InlineData("  clonar  ")]  // espacios alrededor: se recortan
    public void Matches_TitleIgnoringCaseAndSpaces_ReturnsTrue(string query)
    {
        var page = LibraryTestData.Page("IA Clonar");

        Assert.True(page.Matches(query));
    }

    [Theory]
    [InlineData("Revisión de código", "revision")]  // la consulta no tiene tildes
    [InlineData("Revision de codigo", "revisión")]  // el título no tiene tildes
    [InlineData("Configuración", "CONFIGURACION")]  // tildes + mayúsculas
    public void Matches_IgnoresAccents_ReturnsTrue(string title, string query)
    {
        var page = LibraryTestData.Page(title);

        Assert.True(page.Matches(query));
    }

    [Fact]
    public void Matches_TextInDescription_ReturnsTrue()
    {
        var page = LibraryTestData.Page("IA Clonar", description: "Llevar árbol del proyecto de una IA a otra.");

        Assert.True(page.Matches("arbol del proyecto"));
    }

    [Fact]
    public void Matches_TextInCommand_ReturnsTrue()
    {
        var page = LibraryTestData.Page("IA Clonar", command: "repopack \"$archivo\"");

        Assert.True(page.Matches("repopack"));
    }

    [Fact]
    public void Matches_TextOnlyInSensitiveField_ReturnsFalse()
    {
        // Arrange: el texto buscado existe, pero SOLO dentro del campo sensible.
        // Si la búsqueda lo encontrara, revelaría parte del secreto
        // (bastaría probar letras hasta que la página aparezca).
        var page = LibraryTestData.Page(
            "CMD",
            description: "Definir la clave de la API.",
            secret: "valor-secreto-123");

        // Act
        var result = page.Matches("secreto-123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Matches_TextNotPresentAnywhere_ReturnsFalse()
    {
        var page = LibraryTestData.Page("Dockerfile", description: "Construir la imagen.", command: "docker build -t api .");

        Assert.False(page.Matches("kubernetes"));
    }
}
