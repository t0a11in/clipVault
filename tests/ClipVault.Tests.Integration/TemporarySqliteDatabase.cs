using ClipVault.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ClipVault.Tests.Integration;

/// <summary>
/// Base SQLite temporal para una prueba. Implementa IDbContextFactory igual que la app,
/// así el repositorio se prueba exactamente como se usa en producción.
/// Se usa un archivo (y no ":memory:") porque el repositorio abre y cierra un
/// DbContext por operación; una base en memoria desaparecería al cerrarse.
/// </summary>
public sealed class TemporarySqliteDatabase : IDbContextFactory<ClipVaultDbContext>, IDisposable
{
    private readonly DbContextOptions<ClipVaultDbContext> _options;

    public TemporarySqliteDatabase()
    {
        FilePath = Path.Combine(Path.GetTempPath(), $"clipvault-test-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<ClipVaultDbContext>()
            .UseSqlite($"Data Source={FilePath}")
            .Options;
    }

    public string FilePath { get; }

    public ClipVaultDbContext CreateDbContext() => new(_options);

    /// <summary>Crea el esquema con el inicializador real de la app.</summary>
    public Task InitializeAsync() => new DatabaseInitializer(this).InitializeAsync();

    public void Dispose()
    {
        // SQLite mantiene conexiones en un pool: hay que liberarlas para poder borrar el archivo.
        SqliteConnection.ClearAllPools();
        foreach (var file in new[] { FilePath, FilePath + "-wal", FilePath + "-shm" })
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
