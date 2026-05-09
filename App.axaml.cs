using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Text.Json;
using Avalonia.Markup.Xaml;
using sy_ftp.ViewModels;
using sy_ftp.Views;

namespace sy_ftp;

public partial class App : Application
{
    private static readonly string ThemeFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SY-FTP", "theme.json");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        RequestedThemeVariant = LoadTheme();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static ThemeVariant LoadTheme()
    {
        try
        {
            if (File.Exists(ThemeFile))
            {
                var json = File.ReadAllText(ThemeFile);
                var theme = JsonSerializer.Deserialize<string>(json);
                return theme switch
                {
                    "Dark" => ThemeVariant.Dark,
                    "Light" => ThemeVariant.Light,
                    _ => ThemeVariant.Default
                };
            }
        }
        catch { }
        return ThemeVariant.Default;
    }

    public static void SaveTheme(ThemeVariant theme)
    {
        try
        {
            var dir = Path.GetDirectoryName(ThemeFile);
            if (dir is not null) Directory.CreateDirectory(dir);
            var value = theme switch
            {
                var t when t == ThemeVariant.Dark => "Dark",
                var t when t == ThemeVariant.Light => "Light",
                _ => "Default"
            };
            File.WriteAllText(ThemeFile, JsonSerializer.Serialize(value));
        }
        catch { }
    }

    private static readonly string AccentFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SY-FTP", "accent.json");

    public static string LoadAccentColor()
    {
        try
        {
            if (File.Exists(AccentFile))
            {
                var hex = JsonSerializer.Deserialize<string>(File.ReadAllText(AccentFile));
                if (!string.IsNullOrWhiteSpace(hex))
                    return hex;
            }
        }
        catch { }
        return "#4050B5";
    }

    public static void SaveAccentColor(string hex)
    {
        try
        {
            var dir = Path.GetDirectoryName(AccentFile);
            if (dir is not null) Directory.CreateDirectory(dir);
            File.WriteAllText(AccentFile, JsonSerializer.Serialize(hex));
        }
        catch { }
    }

    public static void ApplyAccentColor(string hex)
    {
        var app = Current;
        if (app is null) return;

        var isDark = app.RequestedThemeVariant == ThemeVariant.Dark;
        var color = Color.Parse(hex);
        var hsl = color.ToHsl();

        // Resolve the correct target dictionary:
        // ThemeDictionaries (keyed by ThemeVariant) override app.Resources, so we must
        // write into the active ThemeDictionary to beat Semi.Avalonia's own entries.
        var themeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
        IResourceDictionary? themeDict = null;
        foreach (var kv in app.Resources.ThemeDictionaries)
        {
            if (kv.Key is ThemeVariant tv && tv == themeVariant
                && kv.Value is IResourceDictionary rd)
            {
                themeDict = rd;
                break;
            }
        }

        void Set(string key, double l, double s = double.NaN)
        {
            var sat = double.IsNaN(s) ? hsl.S : s;
            var c = new HslColor(hsl.A, hsl.H, sat, ClampL(l)).ToRgb();
            var brush = new SolidColorBrush(c);
            // Write to the active theme dictionary so it wins over Semi's own entries,
            // and also to flat resources as a fallback for controls that skip theme lookup.
            if (themeDict is not null)
                themeDict[key] = brush;
            app.Resources[key] = brush;
        }

        Set("SemiColorPrimary", hsl.L);
        Set("SemiColorPrimaryDisabled", hsl.L + 0.12, hsl.S * 0.3);

        if (isDark)
        {
            Set("SemiColorPrimaryPointerover", hsl.L + 0.08);
            Set("SemiColorPrimaryActive", hsl.L - 0.08);
            Set("SemiColorPrimaryLight", 0.14, Math.Min(hsl.S * 1.1, 1.0));
            Set("SemiColorPrimaryLightPointerover", 0.19, Math.Min(hsl.S * 1.1, 1.0));
            Set("SemiColorPrimaryLightActive", 0.10, Math.Min(hsl.S * 1.1, 1.0));
        }
        else
        {
            Set("SemiColorPrimaryPointerover", hsl.L - 0.06);
            Set("SemiColorPrimaryActive", hsl.L - 0.12);
            Set("SemiColorPrimaryLight", 0.92, hsl.S * 0.35);
            Set("SemiColorPrimaryLightPointerover", 0.87, hsl.S * 0.35);
            Set("SemiColorPrimaryLightActive", 0.95, hsl.S * 0.35);
        }
    }

    private static double ClampL(double l) => Math.Clamp(l, 0.0, 1.0);

}
