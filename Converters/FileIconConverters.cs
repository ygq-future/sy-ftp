using System.Globalization;
using Avalonia.Data.Converters;
using sy_ftp.Helpers;

namespace sy_ftp.Converters;

public class FileIconGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string name ? FileIconHelper.GetFileIcon(name) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class FileIconBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string name ? FileIconHelper.GetFileBrush(name) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
