using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using sy_ftp.Converters;
using sy_ftp.Services;

namespace sy_ftp.Markup;

/// <summary>
/// XAML usage: {l:Tr key=sidebar.hosts} or {l:Tr sidebar.hosts}.
/// Produces a Binding to LocalizationService.Language + a converter that
/// returns Tr(key). Binding re-evaluates whenever Language property changes,
/// which gives us immediate, reliable language switching everywhere.
/// </summary>
public class TrExtension : MarkupExtension
{
    public string Key { get; set; } = "";

    public TrExtension() { }
    public TrExtension(string key) { Key = key; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding(nameof(LocalizationService.Language))
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay,
            Converter = LocConverter.Instance,
            ConverterParameter = Key,
        };
    }
}

