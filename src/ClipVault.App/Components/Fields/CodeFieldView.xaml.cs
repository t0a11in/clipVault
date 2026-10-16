using System.Windows;
using System.Windows.Controls;

namespace ClipVault.App.Components.Fields;

/// <summary>Campo de código o comando en modo lectura (monoespaciado). Solo declara entradas; sin lógica.</summary>
public partial class CodeFieldView : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(CodeFieldView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(CodeFieldView), new PropertyMetadata(string.Empty));

    public CodeFieldView()
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
