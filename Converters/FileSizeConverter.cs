using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace sy_ftp.Converters;

public class FileSizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long bytes) return null;

        return bytes switch
        {
            < 1024 => $"{bytes:N0} B",
            < 1024 * 1024 => $"{bytes / 1024.0:N1} KB",
            < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):N1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):N1} GB"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
