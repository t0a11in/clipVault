using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClipVault.App.Components;

/// <summary>Vista previa de plantilla reutilizable. Solo declara entradas; sin lógica.</summary>
public partial class TemplatePreview : UserControl
{
    public static readonly DependencyProperty TemplateTextProperty = Register<string>(nameof(TemplateText), string.Empty);
    public static readonly DependencyProperty RenderedTextProperty = Register<string>(nameof(RenderedText), string.Empty);
    public static readonly DependencyProperty VariablesProperty = Register<IEnumerable?>(nameof(Variables), null);
    public static readonly DependencyProperty PickFileCommandProperty = Register<ICommand?>(nameof(PickFileCommand), null);
    public static readonly DependencyProperty PickFolderCommandProperty = Register<ICommand?>(nameof(PickFolderCommand), null);
    public static readonly DependencyProperty PasteCommandProperty = Register<ICommand?>(nameof(PasteCommand), null);
    public static readonly DependencyProperty ResetCommandProperty = Register<ICommand?>(nameof(ResetCommand), null);

    public TemplatePreview()
    {
        InitializeComponent();
    }

    public string TemplateText { get => (string)GetValue(TemplateTextProperty); set => SetValue(TemplateTextProperty, value); }

    public string RenderedText { get => (string)GetValue(RenderedTextProperty); set => SetValue(RenderedTextProperty, value); }

    /// <summary>Variables de la plantilla (cada una con Token, Value, Hint e IsPath).</summary>
    public IEnumerable? Variables { get => (IEnumerable?)GetValue(VariablesProperty); set => SetValue(VariablesProperty, value); }

    public ICommand? PickFileCommand { get => (ICommand?)GetValue(PickFileCommandProperty); set => SetValue(PickFileCommandProperty, value); }

    public ICommand? PickFolderCommand { get => (ICommand?)GetValue(PickFolderCommandProperty); set => SetValue(PickFolderCommandProperty, value); }

    public ICommand? PasteCommand { get => (ICommand?)GetValue(PasteCommandProperty); set => SetValue(PasteCommandProperty, value); }

    public ICommand? ResetCommand { get => (ICommand?)GetValue(ResetCommandProperty); set => SetValue(ResetCommandProperty, value); }

    private static DependencyProperty Register<T>(string name, T defaultValue) =>
        DependencyProperty.Register(name, typeof(T), typeof(TemplatePreview), new PropertyMetadata(defaultValue));
}
