using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClipVault.App.Components.Behaviors;

/// <summary>
/// Comportamiento visual para campos de edición en línea (renombrar sección/página):
/// <list type="bullet">
/// <item><c>FocusWhenVisible</c>: al aparecer, el control toma el foco (en un TextBox, además selecciona su texto).</item>
/// <item><c>LostFocusCommand</c>: comando a ejecutar al perder el foco (confirmar o cancelar).</item>
/// </list>
/// No contiene lógica de negocio: solo enlaza eventos de foco con comandos del ViewModel.
/// </summary>
public static class InlineEdit
{
    public static readonly DependencyProperty FocusWhenVisibleProperty =
        DependencyProperty.RegisterAttached(
            "FocusWhenVisible", typeof(bool), typeof(InlineEdit),
            new PropertyMetadata(false, OnFocusWhenVisibleChanged));

    public static readonly DependencyProperty LostFocusCommandProperty =
        DependencyProperty.RegisterAttached(
            "LostFocusCommand", typeof(ICommand), typeof(InlineEdit),
            new PropertyMetadata(null, OnLostFocusCommandChanged));

    public static bool GetFocusWhenVisible(DependencyObject element) => (bool)element.GetValue(FocusWhenVisibleProperty);

    public static void SetFocusWhenVisible(DependencyObject element, bool value) => element.SetValue(FocusWhenVisibleProperty, value);

    public static ICommand? GetLostFocusCommand(DependencyObject element) => (ICommand?)element.GetValue(LostFocusCommandProperty);

    public static void SetLostFocusCommand(DependencyObject element, ICommand? value) => element.SetValue(LostFocusCommandProperty, value);

    private static void OnFocusWhenVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        element.IsVisibleChanged -= OnElementVisibleChanged;
        if ((bool)e.NewValue)
        {
            element.IsVisibleChanged += OnElementVisibleChanged;
        }
    }

    // Sirve para cualquier control (TextBox, botón, panel de configuración…);
    // si es un TextBox, además selecciona su texto.
    private static void OnElementVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is UIElement element && (bool)e.NewValue)
        {
            // Se difiere hasta que el layout termine; si no, el foco puede perderse.
            element.Dispatcher.InvokeAsync(() =>
            {
                element.Focus();
                Keyboard.Focus(element);
                (element as TextBox)?.SelectAll();
            }, System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private static void OnLostFocusCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        element.LostKeyboardFocus -= OnLostKeyboardFocus;
        if (e.NewValue is not null)
        {
            element.LostKeyboardFocus += OnLostKeyboardFocus;
        }
    }

    private static void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not DependencyObject element || sender is not UIElement { IsVisible: true })
        {
            return;
        }

        var command = GetLostFocusCommand(element);
        if (command?.CanExecute(null) == true)
        {
            command.Execute(null);
        }
    }
}
