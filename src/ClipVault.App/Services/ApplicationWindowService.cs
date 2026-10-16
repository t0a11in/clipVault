using System.Windows;

namespace ClipVault.App.Services;

public sealed class ApplicationWindowService : IApplicationWindowService
{
    public void ShowMainWindow()
    {
        var app = (App)Application.Current;
        var window = app.MainWindow;

        if (!window.IsVisible)
        {
            window.Show();
        }

        window.WindowState = WindowState.Normal;
        window.Activate();
        window.Focus();
    }

    public void ExitApplication()
    {
        ((App)Application.Current).RequestExit();
    }
}
