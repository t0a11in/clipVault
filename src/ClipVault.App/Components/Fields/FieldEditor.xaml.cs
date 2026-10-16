using System.Windows;
using System.Windows.Controls;

namespace ClipVault.App.Components.Fields;

/// <summary>Editor de campo reutilizable. Solo declara entradas; sin lógica.</summary>
public partial class FieldEditor : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(FieldEditor), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value), typeof(string), typeof(FieldEditor),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty IsMultilineProperty =
        DependencyProperty.Register(nameof(IsMultiline), typeof(bool), typeof(FieldEditor), new PropertyMetadata(false));

    public static readonly DependencyProperty IsCodeProperty =
        DependencyProperty.Register(nameof(IsCode), typeof(bool), typeof(FieldEditor), new PropertyMetadata(false));

    public static readonly DependencyProperty IsSensitiveProperty =
        DependencyProperty.Register(nameof(IsSensitive), typeof(bool), typeof(FieldEditor), new PropertyMetadata(false));

    public FieldEditor()
    {
        InitializeComponent();
    }

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public bool IsMultiline { get => (bool)GetValue(IsMultilineProperty); set => SetValue(IsMultilineProperty, value); }

    public bool IsCode { get => (bool)GetValue(IsCodeProperty); set => SetValue(IsCodeProperty, value); }

    public bool IsSensitive { get => (bool)GetValue(IsSensitiveProperty); set => SetValue(IsSensitiveProperty, value); }
}
