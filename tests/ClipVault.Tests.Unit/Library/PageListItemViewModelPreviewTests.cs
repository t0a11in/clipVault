using ClipVault.Tests.Unit.TestData;

namespace ClipVault.Tests.Unit.Library;

/// <summary>
/// Pruebas de <c>PageListItemViewModel.Preview</c>: la línea de vista previa que se
/// muestra bajo el título en la lista de páginas.
///
/// Reglas que se verifican:
/// 1. Prefiere el comando; si no hay comando, usa la descripción.
/// 2. Muestra solo la primera línea (sin espacios sobrantes).
/// 3. NUNCA muestra el valor de un campo sensible.
/// 4. Si no hay nada que mostrar, devuelve "Sin contenido".
/// </summary>
public sealed class PageListItemViewModelPreviewTests
{
    private const string NoContent = "Sin contenido";

    [Fact]
    public void Preview_WithCommandAndDescription_PrefersCommand()
    {
        // Arrange
        var page = LibraryTestData.Page(
            "IA Clonar",
            description: "Llevar árbol del proyecto de una IA a otra.",
            command: "repopack \"$archivo\"");

        // Act
        var preview = page.Preview;

        // Assert: el comando es lo más útil de ver en la lista.
        Assert.Equal("repopack \"$archivo\"", preview);
    }

    [Fact]
    public void Preview_WithoutCommand_UsesDescription()
    {
        var page = LibraryTestData.Page("Saludo a cliente", description: "Hola, gracias por escribirnos.");

        Assert.Equal("Hola, gracias por escribirnos.", page.Preview);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Preview_BlankCommand_FallsBackToDescription(string blankCommand)
    {
        // Un comando vacío o solo con espacios no cuenta como contenido.
        var page = LibraryTestData.Page("Página", description: "Texto de respaldo", command: blankCommand);

        Assert.Equal("Texto de respaldo", page.Preview);
    }

    [Fact]
    public void Preview_MultilineCommand_ShowsOnlyFirstLineTrimmed()
    {
        // Arrange: el script de PowerShell de los datos de ejemplo tiene varias líneas.
        var command =
            "  $backup = Join-Path $PWD \"angular21-backup\"  \n" +
            "New-Item -ItemType Directory -Path $backup";
        var page = LibraryTestData.Page("Backup previo", command: command);

        // Act
        var preview = page.Preview;

        // Assert: una sola línea, sin espacios al inicio ni al final.
        Assert.Equal("$backup = Join-Path $PWD \"angular21-backup\"", preview);
        Assert.DoesNotContain("\n", preview);
    }

    [Fact]
    public void Preview_OnlySensitiveContent_NeverShowsSecret()
    {
        // Arrange: la página solo tiene un campo sensible.
        const string secretValue = "valor-secreto-123";
        var page = LibraryTestData.Page("CMD", secret: secretValue);

        // Act
        var preview = page.Preview;

        // Assert: se muestra el texto genérico y el secreto no aparece en ninguna parte.
        Assert.Equal(NoContent, preview);
        Assert.DoesNotContain(secretValue, preview);
    }

    [Fact]
    public void Preview_SensitiveAndDescription_ShowsDescriptionNotSecret()
    {
        const string secretValue = "valor-secreto-123";
        var page = LibraryTestData.Page("CMD", description: "Definir la clave de la API.", secret: secretValue);

        Assert.Equal("Definir la clave de la API.", page.Preview);
        Assert.DoesNotContain(secretValue, page.Preview);
    }

    [Fact]
    public void Preview_NoFields_ReturnsNoContentText()
    {
        var page = LibraryTestData.Page("Nueva página");

        Assert.Equal(NoContent, page.Preview);
    }
}
