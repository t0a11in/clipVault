using ClipVault.App.Features.Library;

namespace ClipVault.Tests.Unit.Library;

/// <summary>
/// Pruebas de <c>InlineRenameViewModel</c>, el estado compartido de "renombrar en línea"
/// que usan secciones y páginas. Reglas: Enter confirma (sin espacios sobrantes),
/// Escape cancela, un nombre vacío no se puede confirmar y perder el foco con el
/// nombre vacío equivale a cancelar.
///
/// No hace falta Moq: las dependencias son tres delegados, y se reemplazan por
/// lambdas que registran lo que ocurrió (un "doble de prueba" hecho a mano).
/// </summary>
public sealed class InlineRenameViewModelTests
{
    // Estado observado por las lambdas: qué nombre se aplicó y si se canceló.
    private string _currentName = "Comandos";
    private string? _appliedName;
    private int _cancelCount;

    private InlineRenameViewModel CreateSut() =>
        new(currentName: () => _currentName,
            apply: name => _appliedName = name,
            onCancelled: () => _cancelCount++);

    [Fact]
    public void Begin_CopiesCurrentNameAndStartsEditing()
    {
        var sut = CreateSut();

        sut.Begin();

        Assert.True(sut.IsEditing);
        Assert.Equal("Comandos", sut.EditName);
    }

    [Fact]
    public void Commit_ValidName_AppliesTrimmedNameAndStopsEditing()
    {
        var sut = CreateSut();
        sut.Begin();
        sut.EditName = "   Docker   ";

        sut.CommitCommand.Execute(null);

        Assert.Equal("Docker", _appliedName);
        Assert.False(sut.IsEditing);
        Assert.Equal(0, _cancelCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Commit_BlankName_CannotExecute(string blank)
    {
        var sut = CreateSut();
        sut.Begin();

        sut.EditName = blank;

        // CanExecute = false: el botón/Enter quedan deshabilitados y nada se aplica.
        Assert.False(sut.CommitCommand.CanExecute(null));
    }

    [Fact]
    public void Cancel_WhileEditing_CallsOnCancelledAndDoesNotApply()
    {
        var sut = CreateSut();
        sut.Begin();
        sut.EditName = "Otro nombre";

        sut.CancelCommand.Execute(null);

        Assert.Equal(1, _cancelCount);
        Assert.Null(_appliedName);
        Assert.False(sut.IsEditing);
    }

    [Fact]
    public void Finish_BlankName_Cancels()
    {
        // Perder el foco con el campo vacío: se cancela (y una sección nueva se descartaría).
        var sut = CreateSut();
        sut.Begin();
        sut.EditName = "";

        sut.FinishCommand.Execute(null);

        Assert.Equal(1, _cancelCount);
        Assert.Null(_appliedName);
    }

    [Fact]
    public void Finish_ValidName_Commits()
    {
        var sut = CreateSut();
        sut.Begin();
        sut.EditName = "Prompts";

        sut.FinishCommand.Execute(null);

        Assert.Equal("Prompts", _appliedName);
        Assert.Equal(0, _cancelCount);
    }

    [Fact]
    public void CommitAndCancel_WhenNotEditing_DoNothing()
    {
        // Protege contra dobles disparos (por ejemplo Enter seguido de pérdida de foco).
        var sut = CreateSut();

        sut.EditName = "Algo";
        sut.CommitCommand.Execute(null);
        sut.CancelCommand.Execute(null);

        Assert.Null(_appliedName);
        Assert.Equal(0, _cancelCount);
    }
}
