using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClipVault.App.Components;

/// <summary>
/// Caja de búsqueda reutilizable. El code-behind solo maneja comportamiento
/// visual del propio control (marcador, botón limpiar, foco); no hay lógica de negocio.
/// </summary>
public partial class SearchBox : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(SearchBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(SearchBox), new PropertyMetadata("Buscar"));

    public SearchBox()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>Enfoca el campo y selecciona su texto (usado por Ctrl+F).</summary>
    public void FocusInput()
    {
        Input.Focus();
        Keyboard.Focus(Input);
        Input.SelectAll();
    }

    private void OnInputTextChanged(object sender, TextChangedEventArgs e)
    {
        var isEmpty = string.IsNullOrEmpty(Input.Text);
        PlaceholderText.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        ClearButton.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnInputFocusChanged(object sender, KeyboardFocusChangedEventArgs e)
    {
        Frame.BorderBrush = (System.Windows.Media.Brush)FindResource(
            Input.IsKeyboardFocused ? "Cv.Brush.Focus" : "Cv.Brush.InputBorder");
        Frame.BorderThickness = (Thickness)FindResource(
            Input.IsKeyboardFocused ? "Cv.Focus.Thickness" : "Cv.Border.Thickness");
    }

    private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && !string.IsNullOrEmpty(Input.Text))
        {
            Input.Clear();
            e.Handled = true;
        }
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        Input.Clear();
        FocusInput();
    }
}
