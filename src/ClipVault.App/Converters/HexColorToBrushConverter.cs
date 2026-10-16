using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ClipVault.App.Converters;

/// <summary>
/// Convierte "#RRGGBB" en un pincel congelado. Si el valor es nulo o inválido
/// devuelve <see cref="DependencyProperty.UnsetValue"/> para que se use el
/// valor por defecto (FallbackValue o el del estilo).
/// </summary>
public sealed class HexColorToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string hex || string.IsNullOrWhiteSpace(hex))
        {
            return DependencyProperty.UnsetValue;
        }

        try
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }
        catch (FormatException)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
