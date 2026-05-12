using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using sy_ftp.Services;
using sy_ftp.ViewModels;

namespace sy_ftp.Converters;

/// <summary>
/// Translates a localization key to the active-language string.
///
/// Two usage patterns:
/// 1. Bind a key, pass it as value: Text="{Binding TitleKey, Converter={x:Static conv:LocConverter.Instance}}"
/// 2. Bind Language (as a trigger) with the key in ConverterParameter — this is what {l:Tr} uses
///    so that changing Language re-evaluates every instance.
/// </summary>
public class LocConverter : IValueConverter
{
    public static readonly LocConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Parameter takes precedence (that's the Tr markup-extension pattern).
        if (parameter is string pKey && !string.IsNullOrEmpty(pKey))
            return LocalizationService.Instance.Tr(pKey);
        if (value is string vKey && !string.IsNullOrEmpty(vKey))
            return LocalizationService.Instance.Tr(vKey);
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Returns true when the converted value equals the ConverterParameter (as strings).</summary>
public class EqualsConverter : IValueConverter
{
    public static readonly EqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var a = value?.ToString();
        var b = parameter?.ToString();
        return string.Equals(a, b, StringComparison.Ordinal);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Displays the tag sentinel ("All tags") as its localized label, passes real tags
/// through. Accepts two bindings via MultiBinding: [tag_value, Language]. The
/// Language binding exists purely to re-trigger this converter when language changes.
/// </summary>
public class TagLabelConverter : IMultiValueConverter
{
    public static readonly TagLabelConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = values.Count > 0 ? values[0] as string ?? "" : "";
        return s == HostManagerViewModel.AllTagsSentinel
            ? LocalizationService.Instance.Tr("sidebar.tag.all")
            : s;
    }
}



