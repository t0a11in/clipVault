using ClipVault.Core.Models;
using ClipVault.Infrastructure.Repositories;

namespace ClipVault.Tests.Integration.Repositories;

/// <summary>
/// Pruebas de integración de <c>ClipRepository</c> con SQLite real.
/// IAsyncLifetime: xUnit llama a InitializeAsync antes de cada prueba (crea el esquema)
/// y a DisposeAsync después (borra el archivo temporal).
///
/// Pendiente: la regla del CLAUDE.md "la limpieza del historial temporal nunca elimina
/// permanentes" no se puede probar todavía porque el repositorio no tiene un método de
/// limpieza por retención. Cuando exista, agregar aquí su prueba.
/// </summary>
public sealed class ClipRepositoryTests : IAsyncLifetime
{
    private readonly TemporarySqliteDatabase _database = new();
    private ClipRepository _sut = null!;

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();
        _sut = new ClipRepository(_database);
    }

    public Task DisposeAsync()
    {
        _database.Dispose();
        return Task.CompletedTask;
    }

    private static ClipItem TextClip(string text, DateTime? createdAt = null) => new()
    {
        Type = ClipType.Text,
        Content = text,
        CreatedAt = createdAt ?? DateTime.UtcNow
    };

    [Fact]
    public async Task AddTemporary_AlwaysStoresAsTemporary_EvenIfMarkedPermanent()
    {
        // Arrange: un clip que llega "mal" marcado como permanente y con categoría.
        var clip = TextClip("hola");
        clip.IsPermanent = true;
        clip.Title = "título";
        clip.Category = "TRABAJO";

        // Act
        await _sut.AddTemporaryAsync(clip);
        var stored = Assert.Single(await _sut.GetClipsAsync(null));

        // Assert: la captura siempre entra al historial temporal, sin título ni categoría.
        Assert.False(stored.IsPermanent);
        Assert.Null(stored.Title);
        Assert.Null(stored.Category);
        Assert.Equal("hola", stored.Content);
    }

    [Fact]
    public async Task Image_IsStoredAndReadBackAsSameBlob()
    {
        // Las imágenes se guardan como BLOB PNG dentro de SQLite (regla del CLAUDE.md).
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
        await _sut.AddTemporaryAsync(new ClipItem { Type = ClipType.Image, ImageData = png, CreatedAt = DateTime.UtcNow });

        var stored = Assert.Single(await _sut.GetClipsAsync(null));

        Assert.Equal(ClipType.Image, stored.Type);
        Assert.Equal(png, stored.ImageData);
    }

    [Fact]
    public async Task SavePermanently_SetsTitleNormalizedCategory_AndCreatesCategory()
    {
        var clip = await _sut.AddTemporaryAsync(TextClip("dato"));

        await _sut.SavePermanentlyAsync(clip.Id, "  Mi título  ", "compras   del hogar");
        var stored = Assert.Single(await _sut.GetClipsAsync(null));

        Assert.True(stored.IsPermanent);
        Assert.Equal("Mi título", stored.Title);
        Assert.Equal("COMPRAS DEL HOGAR", stored.Category);
        Assert.Contains("COMPRAS DEL HOGAR", await _sut.GetCategoriesAsync());
    }

    [Fact]
    public async Task GetClips_ReturnsNewestFirst_AndFiltersByCategory()
    {
        var older = await _sut.AddTemporaryAsync(TextClip("viejo", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        var newer = await _sut.AddTemporaryAsync(TextClip("nuevo", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)));
        await _sut.SavePermanentlyAsync(older.Id, "Viejo", "Trabajo");

        var all = await _sut.GetClipsAsync(null);
        var filtered = await _sut.GetClipsAsync("trabajo"); // el filtro se normaliza a mayúsculas

        Assert.Equal(new[] { newer.Id, older.Id }, all.Select(c => c.Id));
        Assert.Equal(older.Id, Assert.Single(filtered).Id);
    }

    [Fact]
    public async Task Delete_RemovesOnlyThatClip_AndIgnoresUnknownIds()
    {
        var keep = await _sut.AddTemporaryAsync(TextClip("se queda"));
        var remove = await _sut.AddTemporaryAsync(TextClip("se va"));

        await _sut.DeleteAsync(remove.Id);
        await _sut.DeleteAsync(999_999); // no existe: no debe lanzar excepción

        Assert.Equal(keep.Id, Assert.Single(await _sut.GetClipsAsync(null)).Id);
    }

    [Fact]
    public async Task UpdateText_ChangesContent()
    {
        var clip = await _sut.AddTemporaryAsync(TextClip("antes"));

        await _sut.UpdateTextAsync(clip.Id, "después");

        Assert.Equal("después", Assert.Single(await _sut.GetClipsAsync(null)).Content);
    }
}
