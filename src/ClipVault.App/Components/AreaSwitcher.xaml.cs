using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace ClipVault.App.Components;

/// <summary>
/// Selector segmentado reutilizable. Entradas:
/// <see cref="ItemsSource"/> (opciones), <see cref="SelectedItem"/> (enlace bidireccional)
/// y <see cref="Orientation"/>.
/// El code-behind solo declara propiedades: no contiene lógica de negocio.
/// </summary>
public partial class AreaSwitcher : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource), typeof(IEnumerable), typeof(AreaSwitcher), new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem), typeof(object), typeof(AreaSwitcher),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation), typeof(Orientation), typeof(AreaSwitcher),
            new PropertyMetadata(Orientation.Horizontal));

    public AreaSwitcher()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Horizontal (segmentos) o Vertical (riel lateral).</summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }
}
