using System.Globalization;
using System.Windows.Data;

namespace ClipVault.App.Converters;

/// <summary>Invierte un bool (por ejemplo, IsEnabled = !IsNavigationLocked).</summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;
}
