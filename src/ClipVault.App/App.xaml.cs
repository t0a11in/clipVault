using System.IO;
using System.Windows;
using ClipVault.App.Configuration;
using ClipVault.App.Services;
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
            services.AddSingleton<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();

            var initializer =
                _serviceProvider.GetRequiredService<DatabaseInitializer>();

            await initializer.InitializeAsync();

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
