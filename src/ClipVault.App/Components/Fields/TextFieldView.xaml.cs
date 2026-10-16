using System.Windows;
using System.Windows.Controls;

namespace ClipVault.App.Components.Fields;

/// <summary>Campo de texto (una o varias líneas) en modo lectura. Solo declara entradas; sin lógica.</summary>
public partial class TextFieldView : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(TextFieldView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(TextFieldView), new PropertyMetadata(string.Empty));

    public TextFieldView()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}
