using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace sy_ftp.Converters;

public class NotNullOrEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrEmpty(s);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
