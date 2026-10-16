using System.Windows;
using System.Windows.Controls;

namespace ClipVault.App.Components;

/// <summary>Encabezado de columna reutilizable. Solo declara entradas; sin lógica.</summary>
public partial class PanelHeader : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(PanelHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ActionsProperty =
        DependencyProperty.Register(nameof(Actions), typeof(object), typeof(PanelHeader), new PropertyMetadata(null));

    public PanelHeader()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }
}
