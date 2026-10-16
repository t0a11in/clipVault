using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace ClipVault.App.Components;

/// <summary>Lista de navegación reutilizable. Solo declara entradas; sin lógica.</summary>
public partial class NavList : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(NavList), new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem), typeof(object), typeof(NavList),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty ListNameProperty =
        DependencyProperty.Register(nameof(ListName), typeof(string), typeof(NavList), new PropertyMetadata(string.Empty));

    public NavList()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string ListName
    {
        get => (string)GetValue(ListNameProperty);
        set => SetValue(ListNameProperty, value);
    }
}
