using System.Windows;
using ClipVault.App.ViewModels;
using ClipVault.App.Views;
using ClipVault.Core.Interfaces;
using ClipVault.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ClipVault.App.Services;

public sealed class SaveClipDialogService(
    IServiceProvider serviceProvider,
    IClipRepository repository)
    : ISaveClipDialogService
{
    public async Task<SaveClipRequest?> ShowAsync(
        ClipItem clip,
        CancellationToken cancellationToken = default)
    {
        var viewModel =
            serviceProvider.GetRequiredService<SaveClipDialogViewModel>();

        // Los clips temporales no tienen título. Se propone un valor editable,
        // mientras que un clip ya guardado conserva el último título asignado.
        viewModel.Title = string.IsNullOrWhiteSpace(clip.Title)
            ? "SIN REGISTRO"
            : clip.Title;
        viewModel.Category = clip.Category ?? string.Empty;
        viewModel.SetCategories(await repository.GetCategoriesAsync(cancellationToken));

        var dialog = new SaveClipDialog(viewModel)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        // La categoría agregada desde el botón + se persiste antes de guardar el clip.
        var category = await repository.AddCategoryAsync(
            viewModel.Category,
            cancellationToken);

        return new SaveClipRequest(viewModel.Title.Trim(), category);
    }
}
