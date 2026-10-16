using ClipVault.Core.Models;

namespace ClipVault.Core.Interfaces;

public interface IClipRepository
{
    Task<ClipItem> AddTemporaryAsync(
        ClipItem clip,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClipItem>> GetClipsAsync(
        string? filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryItem>> GetCategoryItemsAsync(
        CancellationToken cancellationToken = default);

    Task<string?> AddCategoryAsync(
        string? category,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateCategoryAsync(
        string currentCategory,
        string? newCategory,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteUserCategoryAsync(
        string category,
        CancellationToken cancellationToken = default);

    Task<ClipItem?> GetLatestAsync(
        CancellationToken cancellationToken = default);

    Task SavePermanentlyAsync(
        long id,
        string title,
        string? category,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task UpdateTextAsync(
        long id,
        string content,
        CancellationToken cancellationToken = default);
}
