using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClipVault.App.Converters;

/// <summary>
/// Enlaza RadioButton con un enum: IsChecked = (valor == ConverterParameter).
/// Al marcar el botón devuelve el valor del parámetro; al desmarcar no cambia nada.
/// </summary>
public sealed class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null && parameter is string name && string.Equals(value.ToString(), name, StringComparison.Ordinal);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true && parameter is string name ? Enum.Parse(targetType, name) : Binding.DoNothing;
}
