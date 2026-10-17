using ClipVault.Core.Models;
using ClipVault.Infrastructure.Repositories;

namespace ClipVault.Tests.Integration.Repositories;

/// <summary>
/// El inicializador se ejecuta en cada arranque de la app: debe poder correr varias
/// veces sin perder datos (idempotente).
/// </summary>
public sealed class DatabaseInitializerTests : IDisposable
{
    private readonly TemporarySqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Initialize_Twice_KeepsExistingClips()
    {
        // Arrange: primer arranque y un clip guardado.
        await _database.InitializeAsync();
        var repository = new ClipRepository(_database);
        await repository.AddTemporaryAsync(new ClipItem { Type = ClipType.Text, Content = "persistente", CreatedAt = DateTime.UtcNow });

        // Act: segundo arranque.
        await _database.InitializeAsync();

        // Assert
        Assert.Equal("persistente", Assert.Single(await repository.GetClipsAsync(null)).Content);
    }
}
