using System.Data;
using ClipVault.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ClipVault.Infrastructure.Data;

public sealed class DatabaseInitializer(
    IDbContextFactory<ClipVaultDbContext> contextFactory)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var context =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        await context.Database.EnsureCreatedAsync(cancellationToken);

        // EnsureCreated no modifica una base SQLite ya existente. Este CREATE es una
        // migración compatible para instalaciones anteriores, sin tocar los clips.
        await context.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS Categories (Name TEXT NOT NULL PRIMARY KEY, IsUserCreated INTEGER NOT NULL DEFAULT 0)",
            cancellationToken);

        await EnsureCategoryOriginColumnAsync(context, cancellationToken);

        // Importa las categorías que ya existían dentro de Clips al nuevo catálogo.
        var existingNames = await context.Categories
            .AsNoTracking()
            .Select(category => category.Name)
            .ToHashSetAsync(cancellationToken);

        var legacyNames = await context.Clips
            .AsNoTracking()
            .Where(clip => clip.Category != null && clip.Category != string.Empty)
            .Select(clip => clip.Category!)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var legacyName in legacyNames)
        {
            var normalized = CategoryName.NormalizeForStorage(legacyName);

            if (normalized is not null && existingNames.Add(normalized))
            {
                context.Categories.Add(new CategoryItem
                {
                    Name = normalized,
                    IsUserCreated = false
                });
            }
        }

        // También corrige los valores ya almacenados en Clips para que la base no
        // conserve variantes como "ComPRAS del    hogar".
        var clipsWithCategory = await context.Clips
            .Where(clip => clip.Category != null && clip.Category != string.Empty)
            .ToListAsync(cancellationToken);

        foreach (var clip in clipsWithCategory)
        {
            clip.Category = CategoryName.NormalizeForStorage(clip.Category);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCategoryOriginColumnAsync(
        ClipVaultDbContext context,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var hasOriginColumn = false;

            await using (var columnCommand = connection.CreateCommand())
            {
                columnCommand.CommandText = "PRAGMA table_info(Categories)";
                await using var reader = await columnCommand.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    if (string.Equals(reader.GetString(1), "IsUserCreated", StringComparison.Ordinal))
                    {
                        hasOriginColumn = true;
                        break;
                    }
                }
            }

            if (!hasOriginColumn)
            {
                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE Categories ADD COLUMN IsUserCreated INTEGER NOT NULL DEFAULT 0",
                    cancellationToken);
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
