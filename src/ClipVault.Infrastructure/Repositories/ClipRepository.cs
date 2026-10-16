using ClipVault.Core.Interfaces;
using ClipVault.Core.Models;
using ClipVault.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClipVault.Infrastructure.Repositories;

public sealed class ClipRepository(
    IDbContextFactory<ClipVaultDbContext> contextFactory) : IClipRepository
{
    public async Task<ClipItem> AddTemporaryAsync(
        ClipItem clip,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        clip.IsPermanent = false;
        clip.Title = null;
        clip.Category = null;

        await context.Clips.AddAsync(clip, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return clip;
    }

    public async Task<IReadOnlyList<ClipItem>> GetClipsAsync(
        string? filter,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<ClipItem> query = context.Clips.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var value = CategoryName.NormalizeForStorage(filter)!;

            query = query.Where(clip =>
                clip.Category != null && clip.Category.Contains(value));
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ClipItem?> GetLatestAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Clips
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Categories
            .AsNoTracking()
            .Select(category => category.Name)
            .Distinct()
            .OrderBy(category => category)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryItem>> GetCategoryItemsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> AddCategoryAsync(
        string? category,
        CancellationToken cancellationToken = default)
    {
        var normalized = CategoryName.NormalizeForStorage(category);

        if (normalized is null)
        {
            return null;
        }

        await using var context =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var exists = await context.Categories
            .AnyAsync(item => item.Name == normalized, cancellationToken);

        if (!exists)
        {
            await context.Categories.AddAsync(
                new CategoryItem
                {
                    Name = normalized,
                    IsUserCreated = true
                },
                cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        return normalized;
    }

    public async Task<bool> UpdateCategoryAsync(
        string currentCategory,
        string? newCategory,
        CancellationToken cancellationToken = default)
    {
        var current = CategoryName.NormalizeForStorage(currentCategory);
        var updated = CategoryName.NormalizeForStorage(newCategory);

        if (current is null || updated is null)
        {
            return false;
        }

        await using var context =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var exists = await context.Categories
            .AnyAsync(category => category.Name == current, cancellationToken);

        if (!exists)
        {
            return false;
        }

        if (current == updated)
        {
            return true;
        }

        var targetExists = await context.Categories
            .AnyAsync(category => category.Name == updated, cancellationToken);

        if (targetExists)
        {
            return false;
        }

        // Se actualiza el catálogo y los clips que referencian la categoría.
        // ExecuteSql evita modificar una columna que es clave primaria en EF.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Categories SET Name = {updated} WHERE Name = {current}",
            cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Clips SET Category = {updated} WHERE Category = {current}",
            cancellationToken);

        return true;
    }

    public async Task<bool> DeleteUserCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        var normalized = CategoryName.NormalizeForStorage(category);

        if (normalized is null)
        {
            return false;
        }

        await using var context =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var categoryItem = await context.Categories.FindAsync(
            new object[] { normalized },
            cancellationToken);

        if (categoryItem is null || !categoryItem.IsUserCreated)
        {
            return false;
        }

        // Los clips no se borran: simplemente vuelven a quedar sin categoría.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Clips SET Category = NULL WHERE Category = {normalized}",
            cancellationToken);
        context.Categories.Remove(categoryItem);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task SavePermanentlyAsync(
        long id,
        string title,
        string? category,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var clip = await context.Clips.FindAsync(new object[] { id }, cancellationToken);

        if (clip is null)
        {
            return;
        }

        clip.Title = title.Trim();
        clip.Category = CategoryName.NormalizeForStorage(category);
        clip.IsPermanent = true;

        if (clip.Category is not null && !await context.Categories
                .AnyAsync(item => item.Name == clip.Category, cancellationToken))
        {
            context.Categories.Add(new CategoryItem
            {
                Name = clip.Category,
                IsUserCreated = true
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var clip = await context.Clips.FindAsync(new object[] { id }, cancellationToken);

        if (clip is null)
        {
            return;
        }

        context.Clips.Remove(clip);
        await context.SaveChangesAsync(cancellationToken);
    }


    public async Task UpdateTextAsync(
        long id,
        string content,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var clip = await context.Clips.FindAsync(new object[] { id }, cancellationToken);

        // Una imagen no se modifica mediante el editor de texto del detalle.
        if (clip is null || clip.Type != ClipType.Text)
        {
            return;
        }

        clip.Content = content;
        await context.SaveChangesAsync(cancellationToken);
    }
}
