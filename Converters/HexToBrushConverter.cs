using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace sy_ftp.Converters;

public class HexToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string hex ? SolidColorBrush.Parse(hex) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
