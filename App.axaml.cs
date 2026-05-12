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
using sy_ftp.Models;
using sy_ftp.Services;
using sy_ftp.ViewModels;
using sy_ftp.Views;

namespace sy_ftp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Load unified settings first so everything else can read from it
        SettingsService.Load();
        LocalizationService.Instance.Language = SettingsService.Current.Language;
        RequestedThemeVariant = ParseTheme(SettingsService.Current.Theme);
        ApplyAccentColor(SettingsService.Current.AccentColor);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ThemeVariant ParseTheme(string value) => value switch
    {
        "Dark" => ThemeVariant.Dark,
        "Light" => ThemeVariant.Light,
        _ => ThemeVariant.Default
    };

    public static void SaveTheme(ThemeVariant theme)
    {
        SettingsService.Current.Theme = theme switch
        {
            var t when t == ThemeVariant.Dark => "Dark",
            var t when t == ThemeVariant.Light => "Light",
            _ => "Default"
        };
        SettingsService.Save();
    }

    private static string ConfigFile => Path.Combine(SettingsService.ConfigDir, "config.json");

    public static AppConfig LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigFile))
            {
                var json = File.ReadAllText(ConfigFile);
                var config = JsonSerializer.Deserialize<AppConfig>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (config is not null) return config;
            }
        }
        catch { }
        return new AppConfig();
    }

    public static void SaveConfig(AppConfig config)
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigFile);
            if (dir is not null) Directory.CreateDirectory(dir);
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(config,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public static string LoadAccentColor() => SettingsService.Current.AccentColor;

    public static void SaveAccentColor(string hex)
    {
        SettingsService.Current.AccentColor = hex;
        SettingsService.Save();
    }

    public static void ApplyAccentColor(string hex)
    {
        var app = Current;
        if (app is null) return;

        var isDark = app.RequestedThemeVariant == ThemeVariant.Dark;
        Color color;
        try { color = Color.Parse(hex); }
        catch { color = Color.Parse("#4050B5"); }
        var hsl = color.ToHsl();

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
