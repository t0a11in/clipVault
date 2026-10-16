using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using ClipVault.App.Configuration;
using ClipVault.App.Services;
using ClipVault.App.ViewModels;
using ClipVault.Core.Interfaces;

namespace ClipVault.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IClipboardMonitor _clipboardMonitor;
    private readonly IHotkeyService _hotkeyService;
    private readonly IApplicationWindowService _windowService;
    private readonly ClipVaultOptions _options;

    public MainWindow(
        MainViewModel viewModel,
        IClipboardMonitor clipboardMonitor,
        IHotkeyService hotkeyService,
        IApplicationWindowService windowService,
        ClipVaultOptions options)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _clipboardMonitor = clipboardMonitor;
        _hotkeyService = hotkeyService;
        _windowService = windowService;
        _options = options;

        DataContext = _viewModel;

        SourceInitialized += OnSourceInitialized;
        ContentRendered += OnContentRendered;
        Closed += OnClosed;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;

        _clipboardMonitor.ClipboardChanged += OnClipboardChanged;
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;

        try
        {
            _clipboardMonitor.Start(handle);
        }
        catch (Exception exception)
        {
            _viewModel.Status =
                $"No se pudo iniciar el monitor: {exception.Message}";
        }

        if (!_hotkeyService.Register(handle, _options.Hotkey))
        {
            _viewModel.Status =
                "El atajo global ya está en uso por otra aplicación.";
        }
    }

    private async void OnContentRendered(object? sender, EventArgs e)
    {
        ContentRendered -= OnContentRendered;

        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception exception)
        {
            _viewModel.Status =
                $"No se pudo cargar el historial: {exception.Message}";
        }
    }

    private async void OnClipboardChanged(
        object? sender,
        ClipboardContentEventArgs e)
    {
        // Bloquear no altera el portapapeles de Windows: simplemente evita que
        // ClipVault guarde la copia o la imagen recibida mientras está activo.
        if (_viewModel.IsCaptureBlocked)
        {
            return;
        }

        try
        {
            await _viewModel.CaptureAsync(e.Content);
        }
        catch (Exception exception)
        {
            _viewModel.Status =
                $"No se pudo guardar el clip: {exception.Message}";
        }
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        _windowService.ShowMainWindow();
    }

    private void OnClipTableDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid { SelectedItem: ClipListItemViewModel clip } ||
            FindVisualParent<DataGridCell>(e.OriginalSource as DependencyObject) is null)
        {
            return;
        }

        _viewModel.OpenPreview(clip);
        e.Handled = true;
    }

    private void OnPreviewOverlayMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Solo un clic sobre el fondo semitransparente cierra el detalle.
        // Los clics dentro del Border central, incluido el TextBox, permanecen activos.
        if (!ReferenceEquals(sender, e.OriginalSource))
        {
            return;
        }

        _viewModel.ClosePreviewCommand.Execute(null);
        e.Handled = true;
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || !_viewModel.IsPreviewOpen)
        {
            return;
        }

        _viewModel.ClosePreviewCommand.Execute(null);
        e.Handled = true;
    }

    private static T? FindVisualParent<T>(DependencyObject? source)
        where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T target)
            {
                return target;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (Application.Current is App { IsExitRequested: false })
        {
            e.Cancel = true;
            // El cierre por la X minimiza a la barra de tareas. El monitor y el icono
            // de bandeja siguen activos; "Salir" en la bandeja finaliza la aplicación.
            WindowState = WindowState.Minimized;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _clipboardMonitor.ClipboardChanged -= OnClipboardChanged;
        _hotkeyService.HotkeyPressed -= OnHotkeyPressed;
        _clipboardMonitor.Stop();
        _hotkeyService.Unregister();
    }
}
