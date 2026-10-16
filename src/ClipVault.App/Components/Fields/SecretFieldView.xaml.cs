using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClipVault.App.Components.Fields;

/// <summary>Campo sensible: muestra la máscara o el valor revelado temporalmente. Solo declara entradas.</summary>
public partial class SecretFieldView : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(SecretFieldView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(SecretFieldView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty RevealTextProperty =
        DependencyProperty.Register(nameof(RevealText), typeof(string), typeof(SecretFieldView), new PropertyMetadata("Mostrar temporalmente"));

    public static readonly DependencyProperty RevealCommandProperty =
        DependencyProperty.Register(nameof(RevealCommand), typeof(ICommand), typeof(SecretFieldView), new PropertyMetadata(null));

    public static readonly DependencyProperty CopyCommandProperty =
        DependencyProperty.Register(nameof(CopyCommand), typeof(ICommand), typeof(SecretFieldView), new PropertyMetadata(null));

    public SecretFieldView()
    {
        InitializeComponent();
    }

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public string RevealText { get => (string)GetValue(RevealTextProperty); set => SetValue(RevealTextProperty, value); }

    public ICommand? RevealCommand { get => (ICommand?)GetValue(RevealCommandProperty); set => SetValue(RevealCommandProperty, value); }

    public ICommand? CopyCommand { get => (ICommand?)GetValue(CopyCommandProperty); set => SetValue(CopyCommandProperty, value); }
}
