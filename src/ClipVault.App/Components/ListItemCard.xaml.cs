using System.Windows;
using System.Windows.Controls;

namespace ClipVault.App.Components;

/// <summary>Tarjeta de ítem de lista reutilizable. Solo declara entradas; sin lógica.</summary>
public partial class ListItemCard : UserControl
{
    public static readonly DependencyProperty TitleProperty = Register<string>(nameof(Title), string.Empty);
    public static readonly DependencyProperty PreviewProperty = Register<string>(nameof(Preview), string.Empty);
    public static readonly DependencyProperty MetaProperty = Register<string>(nameof(Meta), string.Empty);
    public static readonly DependencyProperty IsFavoriteProperty = Register<bool>(nameof(IsFavorite), false);
    public static readonly DependencyProperty IsSensitiveProperty = Register<bool>(nameof(IsSensitive), false);
    public static readonly DependencyProperty HasCodeProperty = Register<bool>(nameof(HasCode), false);
    public static readonly DependencyProperty UseMonospacePreviewProperty = Register<bool>(nameof(UseMonospacePreview), false);

    public ListItemCard()
    {
        InitializeComponent();
    }

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    public string Preview { get => (string)GetValue(PreviewProperty); set => SetValue(PreviewProperty, value); }

    public string Meta { get => (string)GetValue(MetaProperty); set => SetValue(MetaProperty, value); }

    public bool IsFavorite { get => (bool)GetValue(IsFavoriteProperty); set => SetValue(IsFavoriteProperty, value); }

    public bool IsSensitive { get => (bool)GetValue(IsSensitiveProperty); set => SetValue(IsSensitiveProperty, value); }

    public bool HasCode { get => (bool)GetValue(HasCodeProperty); set => SetValue(HasCodeProperty, value); }

    public bool UseMonospacePreview { get => (bool)GetValue(UseMonospacePreviewProperty); set => SetValue(UseMonospacePreviewProperty, value); }

    private static DependencyProperty Register<T>(string name, T defaultValue) =>
        DependencyProperty.Register(name, typeof(T), typeof(ListItemCard), new PropertyMetadata(defaultValue));
}
