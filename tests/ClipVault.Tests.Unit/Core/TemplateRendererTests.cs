using ClipVault.Core.Services;

namespace ClipVault.Tests.Unit.Core;

/// <summary>
/// Pruebas de <c>TemplateRenderer</c> (Core, lógica pura): renderizar plantillas con
/// <c>$archivo</c>, listar variables y deducir el valor desde el campo "Ejemplo".
/// Al ser una función pura, cada prueba es solo entrada → salida, sin dobles ni estado.
/// </summary>
public sealed class TemplateRendererTests
{
    private static readonly Dictionary<string, string> File = new() { ["archivo"] = @"D:\2026\proyectos\SnapGasto\" };

    [Fact]
    public void Render_KnownVariable_IsReplaced()
    {
        var result = TemplateRenderer.Render("repopack \"$archivo\"", File);

        Assert.Equal(@"repopack ""D:\2026\proyectos\SnapGasto\""", result);
    }

    [Fact]
    public void Render_UnknownVariables_AreLeftIntact()
    {
        // Variables propias de PowerShell (o del shell) no deben tocarse.
        const string script = "$backup = Join-Path $PWD \"x\"";

        Assert.Equal(script, TemplateRenderer.Render(script, File));
    }

    [Fact]
    public void Render_ExactNameMatch_DoesNotTouchLongerNames()
    {
        // $archivo2 es otra variable: no se reemplaza su prefijo.
        var result = TemplateRenderer.Render("$archivo $archivo2", File);

        Assert.Equal(@"D:\2026\proyectos\SnapGasto\ $archivo2", result);
    }

    [Fact]
    public void Render_ValueContainingVariable_IsNotExpandedAgain()
    {
        // Una sola pasada: si el valor contiene "$archivo", queda literal (sin recursión).
        var values = new Dictionary<string, string> { ["archivo"] = "$archivo" };

        Assert.Equal("cat $archivo", TemplateRenderer.Render("cat $archivo", values));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Render_EmptyTemplate_ReturnsEmpty(string? template)
    {
        Assert.Equal(string.Empty, TemplateRenderer.Render(template, File));
    }

    [Fact]
    public void ExtractVariables_ReturnsDistinctNamesInOrder()
    {
        var names = TemplateRenderer.ExtractVariables("$b $archivo $b $9no");

        Assert.Equal(new[] { "b", "archivo" }, names);
    }

    [Theory]
    [InlineData("repopack \"$archivo\"", true)]
    [InlineData("docker build -t api .", false)]
    [InlineData("set API_KEY=$API_KEY", false)]
    public void UsesSupportedVariables_DetectsArchivo(string template, bool expected)
    {
        Assert.Equal(expected, TemplateRenderer.UsesSupportedVariables(template));
    }

    [Fact]
    public void TryExtractValue_ExampleMatchesTemplate_ReturnsValue()
    {
        var ok = TemplateRenderer.TryExtractValue(
            "repopack \"$archivo\"",
            @"repopack ""D:\2026\proyectos\SnapGasto\""",
            "archivo",
            out var value);

        Assert.True(ok);
        Assert.Equal(@"D:\2026\proyectos\SnapGasto\", value);
    }

    [Fact]
    public void TryExtractValue_ExampleDoesNotMatch_ReturnsFalse()
    {
        var ok = TemplateRenderer.TryExtractValue("repopack \"$archivo\"", "docker build .", "archivo", out var value);

        Assert.False(ok);
        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public void TryExtractValue_RepeatedVariable_MustHaveSameValue()
    {
        const string template = "copy $archivo $archivo.bak";

        Assert.True(TemplateRenderer.TryExtractValue(template, "copy a.txt a.txt.bak", "archivo", out var same));
        Assert.Equal("a.txt", same);
        Assert.False(TemplateRenderer.TryExtractValue(template, "copy a.txt b.txt.bak", "archivo", out _));
    }

    [Fact]
    public void Render_SeveralSupportedVariables_AreAllReplaced()
    {
        var values = new Dictionary<string, string>
        {
            ["nombre"] = "feature/login",
            ["texto"] = "Inicio",
            ["fecha"] = "2026-10-15"
        };

        var result = TemplateRenderer.Render("git checkout -b \"$nombre\" && git commit -m \"$texto ($fecha)\"", values);

        Assert.Equal("git checkout -b \"feature/login\" && git commit -m \"Inicio (2026-10-15)\"", result);
    }

    [Theory]
    [InlineData("nombre", "feature/login")]
    [InlineData("texto", "Inicio del login")]
    [InlineData("fecha", "2026-10-15")]
    public void TryExtractValue_SeveralVariables_ExtractsEachOne(string variable, string expected)
    {
        const string template = "git checkout -b \"$nombre\" && git commit -m \"$texto ($fecha)\"";
        const string example = "git checkout -b \"feature/login\" && git commit -m \"Inicio del login (2026-10-15)\"";

        Assert.True(TemplateRenderer.TryExtractValue(template, example, variable, out var value));
        Assert.Equal(expected, value);
    }
}
