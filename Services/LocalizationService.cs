using System.Collections.Generic;
using System.ComponentModel;
using sy_ftp.Resources;

namespace sy_ftp.Services;

/// <summary>
/// Singleton string provider. XAML binds via Tr markup extension, which
/// creates a Binding to [key] on this instance. When Language changes we
/// raise PropertyChanged("Item[]") so every bound target re-evaluates.
/// </summary>
public class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();

    private string _language = "en";
    private IReadOnlyDictionary<string, string> _table = Strings.En;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Language
    {
        get => _language;
        set
        {
            if (_language == value) return;
            _language = value;
            _table = value == "zh" ? Strings.Zh : Strings.En;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? LanguageChanged;

    public string this[string key] => Tr(key);

    public string Tr(string key)
    {
        if (_table.TryGetValue(key, out var v)) return v;
        // fall back to english
        return Strings.En.TryGetValue(key, out var en) ? en : key;
    }

    public string Tr(string key, params object[] args)
    {
        var fmt = Tr(key);
        try { return string.Format(fmt, args); }
        catch { return fmt; }
    }
}
