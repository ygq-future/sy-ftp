using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace sy_ftp.Converters;

public class WindowStateToBoolConverter : IValueConverter
{
    public static readonly WindowStateToBoolConverter IsNormal = new(WindowState.Normal);
    public static readonly WindowStateToBoolConverter IsMaximized = new(WindowState.Maximized);

    private readonly WindowState _match;
    private WindowStateToBoolConverter(WindowState match) => _match = match;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not WindowState s) return false;
        return _match == WindowState.Normal
            ? s != WindowState.Maximized
            : s == WindowState.Maximized;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
