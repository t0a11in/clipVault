using System.Windows;
using ClipVault.App.ViewModels;
using ClipVault.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ClipVault.App.Services;

public sealed class CategoryDialogService(IServiceProvider serviceProvider)
    : ICategoryDialogService
{
    public async Task ShowAsync(CancellationToken cancellationToken = default)
    {
        var viewModel = serviceProvider.GetRequiredService<CategoryDialogViewModel>();
        await viewModel.InitializeAsync(cancellationToken);

        var owner = Application.Current.MainWindow;

        // También permite abrir Categorías desde el menú de bandeja cuando la ventana
        // principal está minimizada a la barra de tareas.
        if (owner.WindowState == WindowState.Minimized)
        {
            owner.WindowState = WindowState.Normal;
            owner.Activate();
        }

        var dialog = new CategoryDialog(viewModel)
        {
            Owner = owner
        };

        dialog.ShowDialog();
    }
}
