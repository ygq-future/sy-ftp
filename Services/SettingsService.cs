using System.Text.Json;
using sy_ftp.Helpers;
using sy_ftp.Models;

namespace sy_ftp.Services;

public static class SettingsService
{
    public static string BaseDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SY-FTP");

    public static string SettingsFile => Path.Combine(BaseDir, "settings.json");

    private static AppSettings? _current;

    public static AppSettings Current
    {
        get
        {
            if (_current is null) Load();
            return _current!;
        }
    }

    public static event EventHandler? SettingsChanged;

    /// <summary>System default for DefaultDownloadPath when the user hasn't set one.</summary>
    public static string SystemDefaultDownloadPath => FtpPathHelper.DefaultDownloadDir;

    /// <summary>System default for DefaultDataPath when the user hasn't set one.</summary>
    public static string SystemDefaultDataPath => BaseDir;

    public static void Load()
    {
        AppSettings? loaded = null;
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                loaded = JsonSerializer.Deserialize<AppSettings>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
        catch { }

        if (loaded is null)
        {
            // No settings.json — try migrating from legacy theme.json + accent.json
            loaded = new AppSettings();
            try
            {
                var themeFile = Path.Combine(BaseDir, "theme.json");
                if (File.Exists(themeFile))
                {
                    var t = JsonSerializer.Deserialize<string>(File.ReadAllText(themeFile));
                    if (!string.IsNullOrWhiteSpace(t)) loaded.Theme = t!;
                }
            }
            catch { }
            try
            {
                var accentFile = Path.Combine(BaseDir, "accent.json");
                if (File.Exists(accentFile))
                {
                    var a = JsonSerializer.Deserialize<string>(File.ReadAllText(accentFile));
                    if (!string.IsNullOrWhiteSpace(a)) loaded.AccentColor = a!;
                }
            }
            catch { }
        }

        // Fill in concrete defaults so the Settings UI never shows blank paths.
        if (string.IsNullOrWhiteSpace(loaded.DefaultDownloadPath))
            loaded.DefaultDownloadPath = SystemDefaultDownloadPath;
        if (string.IsNullOrWhiteSpace(loaded.DefaultDataPath))
            loaded.DefaultDataPath = SystemDefaultDataPath;

        _current = loaded;
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(BaseDir);
            File.WriteAllText(SettingsFile, JsonSerializer.Serialize(Current,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
        SettingsChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Directory where config.json (hosts) lives. Honors DefaultDataPath when set.</summary>
    public static string ConfigDir
    {
        get
        {
            var custom = Current.DefaultDataPath;
            if (!string.IsNullOrWhiteSpace(custom))
            {
                try
                {
                    Directory.CreateDirectory(custom);
                    return custom;
                }
                catch { }
            }
            return BaseDir;
        }
    }
}
