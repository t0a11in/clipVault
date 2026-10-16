using System.IO;
using System.Windows;
using ClipVault.App.Configuration;
using ClipVault.App.Features.Library;
using ClipVault.App.Features.Library.Data;
using ClipVault.App.Services;
using ClipVault.App.Shell;
using ClipVault.App.Themes;
using ClipVault.App.ViewModels;
using ClipVault.App.Views;
using ClipVault.Core.Interfaces;
using ClipVault.Infrastructure.Data;
using ClipVault.Infrastructure.DependencyInjection;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;

namespace ClipVault.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public bool IsExitRequested { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var services = new ServiceCollection();

            services.AddClipVaultInfrastructure(GetDatabasePath());
            services.AddSingleton(ClipVaultOptions.FromArguments(e.Args));

            services.AddSingleton<IApplicationWindowService, ApplicationWindowService>();
            services.AddSingleton<ISaveClipDialogService, SaveClipDialogService>();
            services.AddSingleton<ICategoryDialogService, CategoryDialogService>();
            services.AddSingleton<MainViewModel>();
            services.AddTransient<SaveClipDialogViewModel>();
            services.AddTransient<CategoryDialogViewModel>();
            // Biblioteca y cáscara: singletons para conservar el estado al cambiar de área.
            // Servicios de la Biblioteca (interfaces para poder simularlos en pruebas).
            services.AddSingleton<IClipboardService, ClipboardService>();
            services.AddSingleton<IFileDialogService, FileDialogService>();
            services.AddSingleton<IConfirmationService, ConfirmationService>();
            // Maqueta: datos de ejemplo en memoria (se reemplazará por persistencia real).
            services.AddSingleton<ILibraryDataSource, SampleLibraryDataSource>();
            services.AddSingleton<LibraryViewModel>();
            services.AddSingleton<ShellViewModel>();
            services.AddSingleton<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();

            var initializer =
                _serviceProvider.GetRequiredService<DatabaseInitializer>();

            await initializer.InitializeAsync();

            // Biblioteca: respeta el alto contraste de Windows (no afecta al Historial).
            HighContrastTheme.Attach(Resources);

            var window = _serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = window;

            ConfigureTrayIcon(
                _serviceProvider.GetRequiredService<MainViewModel>());

            window.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "ClipVault",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }

    public void RequestExit()
    {
        IsExitRequested = true;
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            (_serviceProvider?.GetService(typeof(IClipboardMonitor))
                as IClipboardMonitor)?.Stop();

            (_serviceProvider?.GetService(typeof(IHotkeyService))
                as IHotkeyService)?.Unregister();

            HighContrastTheme.Detach();

            if (Resources["TrayIcon"] is TaskbarIcon trayIcon)
            {
                trayIcon.Dispose();
            }

            _serviceProvider?.Dispose();
        }
        finally
        {
            base.OnExit(e);
        }
    }

    private void ConfigureTrayIcon(MainViewModel viewModel)
    {
        var trayIcon = (TaskbarIcon)Resources["TrayIcon"];

        trayIcon.DataContext = viewModel;

        if (trayIcon.ContextMenu is not null)
        {
            trayIcon.ContextMenu.DataContext = viewModel;
        }
    }

    private static string GetDatabasePath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "ClipVault");

        Directory.CreateDirectory(folder);

        return Path.Combine(folder, "clipvault.db");
    }
}
