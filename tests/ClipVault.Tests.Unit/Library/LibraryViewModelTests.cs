using ClipVault.App.Features.Library;
using ClipVault.App.Services;
using ClipVault.Core.Interfaces;
using ClipVault.Core.Models.Library;
using Moq;

namespace ClipVault.Tests.Unit.Library;

/// <summary>
/// Pruebas de <c>LibraryViewModel</c> (y de su editor) usando <b>Moq</b>.
///
/// ¿Por qué Moq aquí? LibraryViewModel depende de cuatro interfaces:
/// - ILibraryDataSource   → se simula para entregar datos controlados (sin SQLite).
/// - IClipboardService    → se simula para no tocar el portapapeles real y para VERIFICAR qué se copió.
/// - IFileDialogService   → se simula para no abrir diálogos.
/// - IConfirmationService → se simula para responder "Sí" o "No" sin ventanas.
///
/// Vocabulario de Moq usado:
/// - <c>Setup(...).Returns(...)</c>: qué devuelve el doble cuando lo llaman.
/// - <c>Verify(..., Times.Once)</c>: comprueba que el código llamó al doble (y cuántas veces).
/// - <c>It.IsAny&lt;T&gt;()</c> / <c>It.Is&lt;T&gt;(condición)</c>: cualquier argumento / uno que cumpla la condición.
/// </summary>
public sealed class LibraryViewModelTests
{
    // ---------- Datos controlados para estas pruebas ----------

    private readonly Section _commands = new() { Name = "Comandos", Order = 0 };
    private readonly Section _messages = new() { Name = "Mensajes", Order = 1 };

    private readonly FieldDefinition _description = new() { Key = "description", Label = "Descripción", Type = FieldType.MultilineText, DisplayOrder = 0 };
    private readonly FieldDefinition _command = new() { Key = "command", Label = "Comando", Type = FieldType.Code, DisplayOrder = 1, IsCommandTemplate = true };
    private readonly FieldDefinition _example = new() { Key = "example", Label = "Ejemplo", Type = FieldType.Text, DisplayOrder = 2 };
    private readonly FieldDefinition _secret = new() { Key = "secret", Label = "Token", Type = FieldType.Secret, DisplayOrder = 3, IsSensitive = true };

    // ---------- Dobles de prueba (Moq) ----------

    private readonly Mock<ILibraryDataSource> _dataSource = new();
    private readonly Mock<IClipboardService> _clipboard = new();
    private readonly Mock<IFileDialogService> _fileDialogs = new();
    private readonly Mock<IConfirmationService> _confirmation = new();

    public LibraryViewModelTests()
    {
        // xUnit crea una instancia nueva de esta clase por cada prueba:
        // el constructor es el "Arrange" común y no hay estado compartido entre pruebas.
        var pages = new List<LibraryPage>
        {
            Page(_commands, "IA Clonar", new DateTime(2026, 10, 16), isFavorite: true,
                (_description, "Llevar árbol del proyecto de una IA a otra."),
                (_command, "repopack \"$archivo\""),
                (_example, @"repopack ""D:\x\""")),
            Page(_commands, "CMD", new DateTime(2026, 10, 10), isFavorite: false,
                (_description, "Definir la clave."),
                (_secret, "valor-secreto-123")),
            Page(_messages, "Saludo", new DateTime(2026, 10, 9), isFavorite: false,
                (_description, "Hola, gracias por escribirnos."))
        };

        _dataSource.Setup(d => d.GetSections()).Returns(new List<Section> { _commands, _messages });
        _dataSource.Setup(d => d.GetFieldDefinitions()).Returns(new List<FieldDefinition> { _description, _command, _example, _secret });
        _dataSource.Setup(d => d.GetPages()).Returns(pages);

        // Por defecto el portapapeles acepta la copia.
        _clipboard.Setup(c => c.TrySetText(It.IsAny<string>())).Returns(true);
    }

    private LibraryViewModel CreateSut() =>
        new(_dataSource.Object, _clipboard.Object, _fileDialogs.Object, _confirmation.Object);

    private static LibraryPage Page(
        Section section, string title, DateTime updatedAt, bool isFavorite,
        params (FieldDefinition Field, string Value)[] values) => new()
    {
        SectionId = section.Id,
        Title = title,
        UpdatedAt = updatedAt,
        IsFavorite = isFavorite,
        IsSensitive = values.Any(v => v.Field.IsSensitive),
        Fields = values.Select(v => new FieldValue { FieldDefinitionId = v.Field.Id, Value = v.Value }).ToList()
    };

    private static PageListItemViewModel Find(LibraryViewModel sut, string title) =>
        sut.VisiblePages.Single(p => p.Title == title);

    // ===================== Navegación y lista =====================

    [Fact]
    public void Constructor_SelectsFirstSection_AndShowsItsPagesNewestFirst()
    {
        var sut = CreateSut();

        Assert.Equal("Comandos", sut.SelectedSection?.Label);
        Assert.Equal(new[] { "IA Clonar", "CMD" }, sut.VisiblePages.Select(p => p.Title));
    }

    [Fact]
    public void SelectingFilter_ClearsSection_AndSelectingSection_ClearsFilter()
    {
        var sut = CreateSut();

        sut.SelectedFilter = sut.Filters[0];   // "Todos los elementos"
        Assert.Null(sut.SelectedSection);
        Assert.Equal(3, sut.VisiblePages.Count);

        sut.SelectedSection = sut.Sections[1]; // "Mensajes"
        Assert.Null(sut.SelectedFilter);
        Assert.Equal("Saludo", Assert.Single(sut.VisiblePages).Title);
    }

    [Fact]
    public void Counts_ReflectPagesPerSection_TotalAndFavorites()
    {
        var sut = CreateSut();

        Assert.Equal(2, sut.Sections[0].Count);   // Comandos
        Assert.Equal(1, sut.Sections[1].Count);   // Mensajes
        Assert.Equal(3, sut.Filters[0].Count);    // Todos
        Assert.Equal(1, sut.Filters[1].Count);    // Favoritos
    }

    [Fact]
    public void Search_KeepsSelectedPage_WhenItIsStillVisible()
    {
        // Regresión: antes, cada tecla en la búsqueda perdía la página seleccionada.
        var sut = CreateSut();
        sut.SelectedPage = Find(sut, "IA Clonar");

        sut.SearchText = "repo";

        Assert.Equal("IA Clonar", sut.SelectedPage?.Title);
    }

    [Fact]
    public void Search_WithoutMatches_ShowsNoResultsState()
    {
        var sut = CreateSut();

        sut.SearchText = "kubernetes";

        Assert.Empty(sut.VisiblePages);
        Assert.True(sut.IsNoSearchResults);
        Assert.False(sut.IsContextEmpty);
    }

    [Fact]
    public void NewPage_CancelledName_IsDiscarded()
    {
        var sut = CreateSut();

        sut.NewPageCommand.Execute(null);
        Assert.Equal(3, sut.Sections[0].Count);           // se agregó
        sut.SelectedPage!.Rename.CancelCommand.Execute(null);

        Assert.Equal(2, sut.Sections[0].Count);           // se descartó
        Assert.DoesNotContain(sut.VisiblePages, p => p.Title.StartsWith("Nueva página", StringComparison.Ordinal));
    }

    // ===================== Editor: copiar =====================

    [Fact]
    public void Copy_TemplatePage_CopiesRenderedCommand_UsingValueFromExample()
    {
        // Arrange: la fuente por defecto de $archivo es el campo "Ejemplo".
        var sut = CreateSut();
        sut.SelectedPage = Find(sut, "IA Clonar");

        // Act
        sut.Editor.CopyCommand.Execute(null);

        // Assert: Moq verifica el texto exacto que se envió al portapapeles.
        _clipboard.Verify(c => c.TrySetText(@"repopack ""D:\x\"""), Times.Once);
    }

    [Fact]
    public void Copy_PageWithSecret_NeverSendsSecretToClipboard()
    {
        var sut = CreateSut();
        sut.SelectedPage = Find(sut, "CMD");

        sut.Editor.CopyCommand.Execute(null);

        // Se copió algo, pero ese algo no contiene el secreto.
        _clipboard.Verify(c => c.TrySetText(It.Is<string>(text => !text.Contains("valor-secreto-123"))), Times.Once);
        _clipboard.Verify(c => c.TrySetSensitiveText(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void CopySensitiveField_UserDeclines_NothingIsCopied()
    {
        // Arrange: el usuario responde "No" a la confirmación.
        _confirmation.Setup(c => c.Confirm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        var sut = CreateSut();
        sut.SelectedPage = Find(sut, "CMD");
        var secretField = sut.Editor.Fields.Single(f => f.IsSensitive);

        // Act
        secretField.CopySensitiveCommand.Execute(null);

        // Assert
        _clipboard.Verify(c => c.TrySetSensitiveText(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void CopySensitiveField_UserConfirms_UsesSensitiveClipboardCopy()
    {
        _confirmation.Setup(c => c.Confirm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _clipboard.Setup(c => c.TrySetSensitiveText(It.IsAny<string>())).Returns(true);
        var sut = CreateSut();
        sut.SelectedPage = Find(sut, "CMD");

        sut.Editor.Fields.Single(f => f.IsSensitive).CopySensitiveCommand.Execute(null);

        // Debe usar la copia "marcada" (excluida del historial de Windows), no la normal.
        _clipboard.Verify(c => c.TrySetSensitiveText("valor-secreto-123"), Times.Once);
        _clipboard.Verify(c => c.TrySetText("valor-secreto-123"), Times.Never);
        // Y el aviso al usuario nunca incluye el valor.
        Assert.DoesNotContain("valor-secreto-123", sut.Notice.Message);
    }

    // ===================== Editor: eliminar y editar =====================

    [Theory]
    [InlineData(false, 2)] // el usuario cancela: la página sigue
    [InlineData(true, 1)]  // el usuario confirma: se elimina
    public void Delete_RespectsUserConfirmation(bool userConfirms, int expectedPagesInSection)
    {
        _confirmation.Setup(c => c.Confirm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(userConfirms);
        var sut = CreateSut();
        sut.SelectedPage = Find(sut, "IA Clonar");

        sut.Editor.DeleteCommand.Execute(null);

        Assert.Equal(expectedPagesInSection, sut.Sections[0].Count);
    }

    [Fact]
    public void Edit_LocksNavigation_AndSaveUnlocksIt()
    {
        var sut = CreateSut();
        sut.SelectedPage = Find(sut, "IA Clonar");

        sut.Editor.EditCommand.Execute(null);
        Assert.True(sut.IsNavigationLocked);
        Assert.False(sut.NewPageCommand.CanExecute(null));   // no se puede crear otra página mientras se edita

        sut.Editor.EditTitle = "IA Clonar v2";
        sut.Editor.SaveCommand.Execute(null);

        Assert.False(sut.IsNavigationLocked);
        Assert.Equal("IA Clonar v2", sut.SelectedPage?.Title);
    }

    [Fact]
    public void Editor_TemplateWithSeveralVariables_CreatesOneVariablePerSupportedName()
    {
        // Arrange: página nueva con tres variables soportadas y una del shell ($HOME).
        var page = Page(_commands, "Git", new DateTime(2026, 10, 17), isFavorite: false,
            (_command, "git checkout -b \"$nombre\" && git commit -m \"$texto ($fecha)\" && cd $HOME"),
            (_example, "git checkout -b \"rama\" && git commit -m \"hola (2026-10-17)\" && cd $HOME"));
        _dataSource.Setup(d => d.GetPages()).Returns(new List<LibraryPage> { page });
        var sut = CreateSut();

        // Act
        sut.SelectedPage = Find(sut, "Git");

        // Assert: $HOME no es una variable de la plantilla; las otras se deducen del ejemplo.
        Assert.Equal(new[] { "nombre", "texto", "fecha" }, sut.Editor.Variables.Select(v => v.Name));
        Assert.Equal("git checkout -b \"rama\" && git commit -m \"hola (2026-10-17)\" && cd $HOME", sut.Editor.RenderedCommand);
    }
}
