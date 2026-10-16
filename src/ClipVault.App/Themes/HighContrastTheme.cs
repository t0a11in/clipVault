using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace ClipVault.App.Themes;

/// <summary>
/// Soporte del modo de alto contraste de Windows para la Biblioteca (etapa 10).
/// Cuando está activo, los pinceles Cv.Brush.* se reemplazan por los colores del
/// sistema; como las vistas los usan con DynamicResource, el cambio es inmediato.
/// Al desactivarse, se quitan los reemplazos y vuelven los tokens del JSON.
/// No afecta al Historial: solo toca las claves Cv.Brush.*.
/// </summary>
public static class HighContrastTheme
{
    private static ResourceDictionary? _target;

    public static void Attach(ResourceDictionary applicationResources)
    {
        _target = applicationResources;
        Apply();
        SystemParameters.StaticPropertyChanged += OnSystemParameterChanged;
    }

    public static void Detach() => SystemParameters.StaticPropertyChanged -= OnSystemParameterChanged;

    private static void OnSystemParameterChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SystemParameters.HighContrast))
        {
            Application.Current?.Dispatcher.Invoke(Apply);
        }
    }

    private static void Apply()
    {
        if (_target is null)
        {
            return;
        }

        foreach (var (key, brush) in Overrides())
        {
            if (SystemParameters.HighContrast)
            {
                _target[key] = brush;
            }
            else
            {
                _target.Remove(key);
            }
        }
    }

    // Fondo = Window, texto = WindowText, selección/acento = Highlight.
    private static IEnumerable<(string Key, Brush Brush)> Overrides()
    {
        Brush window = SystemColors.WindowBrush;
        Brush text = SystemColors.WindowTextBrush;
        Brush highlight = SystemColors.HighlightBrush;
        Brush highlightText = SystemColors.HighlightTextBrush;
        Brush gray = SystemColors.GrayTextBrush;

        string[] backgrounds =
        [
            "Cv.Brush.Surface", "Cv.Brush.SurfaceSubtle", "Cv.Brush.TopBar", "Cv.Brush.Hover",
            "Cv.Brush.SuccessBackground", "Cv.Brush.WarningBackground", "Cv.Brush.DangerBackground",
            "Cv.Brush.SensitiveBackground", "Cv.Brush.DisabledBackground", "Cv.Brush.Overlay"
        ];
        string[] foregrounds =
        [
            "Cv.Brush.Text", "Cv.Brush.MutedText", "Cv.Brush.PlaceholderText", "Cv.Brush.Border",
            "Cv.Brush.InputBorder", "Cv.Brush.Success", "Cv.Brush.Warning", "Cv.Brush.Danger",
            "Cv.Brush.DangerHover", "Cv.Brush.Sensitive"
        ];
        string[] accents =
        [
            "Cv.Brush.Primary", "Cv.Brush.PrimaryHover", "Cv.Brush.PrimaryPressed",
            "Cv.Brush.Selected", "Cv.Brush.SelectedIndicator", "Cv.Brush.Focus"
        ];

        foreach (var key in backgrounds)
        {
            yield return (key, window);
        }

        foreach (var key in foregrounds)
        {
            yield return (key, text);
        }

        foreach (var key in accents)
        {
            yield return (key, highlight);
        }

        yield return ("Cv.Brush.OnPrimary", highlightText);
        yield return ("Cv.Brush.SelectedText", highlightText);
        yield return ("Cv.Brush.DisabledText", gray);
    }
}
