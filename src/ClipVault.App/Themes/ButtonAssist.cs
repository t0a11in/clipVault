using System.Windows;
using System.Windows.Media;

namespace ClipVault.App.Themes;

/// <summary>
/// Propiedades adjuntas para que una sola plantilla de botón sirva a todas las
/// variantes (primaria, secundaria, icono, destructiva): cada estilo solo indica
/// los pinceles de hover y pressed, y la plantilla los aplica.
/// </summary>
public static class ButtonAssist
{
    public static readonly DependencyProperty HoverBackgroundProperty =
        DependencyProperty.RegisterAttached(
            "HoverBackground", typeof(Brush), typeof(ButtonAssist), new PropertyMetadata(null));

    public static readonly DependencyProperty PressedBackgroundProperty =
        DependencyProperty.RegisterAttached(
            "PressedBackground", typeof(Brush), typeof(ButtonAssist), new PropertyMetadata(null));

    public static readonly DependencyProperty HoverForegroundProperty =
        DependencyProperty.RegisterAttached(
            "HoverForeground", typeof(Brush), typeof(ButtonAssist), new PropertyMetadata(null));

    public static Brush? GetHoverBackground(DependencyObject element) =>
        (Brush?)element.GetValue(HoverBackgroundProperty);

    public static void SetHoverBackground(DependencyObject element, Brush? value) =>
        element.SetValue(HoverBackgroundProperty, value);

    public static Brush? GetPressedBackground(DependencyObject element) =>
        (Brush?)element.GetValue(PressedBackgroundProperty);

    public static void SetPressedBackground(DependencyObject element, Brush? value) =>
        element.SetValue(PressedBackgroundProperty, value);

    public static Brush? GetHoverForeground(DependencyObject element) =>
        (Brush?)element.GetValue(HoverForegroundProperty);

    public static void SetHoverForeground(DependencyObject element, Brush? value) =>
        element.SetValue(HoverForegroundProperty, value);
}
