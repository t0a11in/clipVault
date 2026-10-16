using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClipVault.App.Components;

/// <summary>Aviso breve reutilizable. Solo declara entradas; sin lógica.</summary>
public partial class NoticeBar : UserControl
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(NoticeBar), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(NoticeBar), new PropertyMetadata(false));

    /// <summary>"Success", "Warning" o "Error".</summary>
    public static readonly DependencyProperty ToneProperty =
        DependencyProperty.Register(nameof(Tone), typeof(string), typeof(NoticeBar), new PropertyMetadata("Success"));

    public static readonly DependencyProperty DismissCommandProperty =
        DependencyProperty.Register(nameof(DismissCommand), typeof(ICommand), typeof(NoticeBar), new PropertyMetadata(null));

    public NoticeBar()
    {
        InitializeComponent();
    }

    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }

    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    public string Tone { get => (string)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    public ICommand? DismissCommand { get => (ICommand?)GetValue(DismissCommandProperty); set => SetValue(DismissCommandProperty, value); }
}
