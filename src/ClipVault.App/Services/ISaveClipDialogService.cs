using ClipVault.Core.Models;

namespace ClipVault.App.Services;

public interface ISaveClipDialogService
{
    Task<SaveClipRequest?> ShowAsync(
        ClipItem clip,
        CancellationToken cancellationToken = default);
}

public sealed record SaveClipRequest(string Title, string? Category);
