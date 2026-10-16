using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace ClipVault.App.Components;

/// <summary>Lista seleccionable reutilizable con desplazamiento propio. Solo declara entradas.</summary>
public partial class SelectableList : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(SelectableList), new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem), typeof(object), typeof(SelectableList),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty ListNameProperty =
        DependencyProperty.Register(nameof(ListName), typeof(string), typeof(SelectableList), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ItemStyleProperty =
        DependencyProperty.Register(nameof(ItemStyle), typeof(Style), typeof(SelectableList), new PropertyMetadata(null));

    public SelectableList()
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

    /// <summary>Estilo del ListBoxItem. Si es null se usa Cv.ListBoxItem.Page.</summary>
    public Style? ItemStyle
    {
        get => (Style?)GetValue(ItemStyleProperty);
        set => SetValue(ItemStyleProperty, value);
    }
}
