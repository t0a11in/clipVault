namespace ClipVault.App.Services;

public interface ICategoryDialogService
{
    Task ShowAsync(CancellationToken cancellationToken = default);
}
