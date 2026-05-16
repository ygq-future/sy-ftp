using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sy_ftp.Helpers;
using sy_ftp.Models;
using sy_ftp.Resources;
using sy_ftp.Services;
using sy_ftp.Views;

namespace sy_ftp.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _main;

    public LocalizationService Loc => LocalizationService.Instance;
    public IReadOnlyList<AccentColorOption> Palette => AccentPalette.Options;

    public IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new("en", "English"),
        new("zh", "中文 (Chinese)"),
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGeneralSelected))]
    [NotifyPropertyChangedFor(nameof(IsAppearanceSelected))]
    [NotifyPropertyChangedFor(nameof(IsPathsSelected))]
    [NotifyPropertyChangedFor(nameof(IsAboutSelected))]
    private int _selectedSectionIndex;

    public bool IsGeneralSelected => SelectedSectionIndex == 0;
    public bool IsAppearanceSelected => SelectedSectionIndex == 1;
    public bool IsPathsSelected => SelectedSectionIndex == 2;
    public bool IsAboutSelected => SelectedSectionIndex == 3;

    public string AppName => "SY-FTP";
    public string AppVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null && version.Major > 0)
            {
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
            return "1.0.3";
        }
    }
    public string Developer => "ygq-future";
    public string License => "Apache-2.0";
    public string GitHubUrl => "https://github.com/ygq-future/sy-ftp";

    [ObservableProperty]
    private LanguageOption _selectedLanguage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLightMode))]
    private bool _isDarkMode;

    public bool IsLightMode
    {
        get => !IsDarkMode;
        set
        {
            if (value) IsDarkMode = false;
        }
    }

    [ObservableProperty]
    private string _accentColor = "#2296F5";

    [ObservableProperty]
    private string? _backgroundImagePath;

    [ObservableProperty]
    private double _backgroundOpacity = 0.3;

    public double BackgroundOpacityPercent
    {
        get => BackgroundOpacity * 100;
        set
        {
            var newOpacity = value / 100.0;
            if (Math.Abs(BackgroundOpacity - newOpacity) > 0.001)
            {
                BackgroundOpacity = newOpacity;
            }
        }
    }

    [ObservableProperty]
    private string _defaultDownloadPath = "";

    [ObservableProperty]
    private string _defaultDataPath = "";

    public SettingsViewModel(MainWindowViewModel main)
    {
        _main = main;

        var s = SettingsService.Current;
        _selectedLanguage = Languages.FirstOrDefault(l => l.Code == s.Language) ?? Languages[0];
        _isDarkMode = main.IsDarkMode;
        _accentColor = main.AccentColor;
        _backgroundImagePath = s.BackgroundImagePath;
        _backgroundOpacity = s.BackgroundOpacity;
        _defaultDownloadPath = s.DefaultDownloadPath ?? "";
        _defaultDataPath = s.DefaultDataPath ?? "";
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (value is null) return;
        SettingsService.Current.Language = value.Code;
        SettingsService.Save();
        Loc.Language = value.Code;
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        _main.IsDarkMode = value;
    }

    [RelayCommand]
    private void PickAccent(AccentColorOption? option)
    {
        if (option is null) return;
        AccentColor = option.Hex;
        _main.ApplyAccentHex(option.Hex);
    }

    partial void OnBackgroundOpacityChanged(double value)
    {
        OnPropertyChanged(nameof(BackgroundOpacityPercent));
        _main.ApplyBackgroundImage(BackgroundImagePath, value);
    }

    [RelayCommand]
    private async Task BrowseBackgroundAsync()
    {
        var picked = await PickImageFileAsync();
        if (picked is null) return;
        BackgroundImagePath = picked;
        _main.ApplyBackgroundImage(picked, BackgroundOpacity);
    }

    [RelayCommand]
    private void ClearBackground()
    {
        BackgroundImagePath = null;
        _main.ApplyBackgroundImage(null, BackgroundOpacity);
    }

    [RelayCommand]
    private async Task BrowseDownloadAsync()
    {
        var picked = await PickFolderAsync();
        if (picked is null) return;
        DefaultDownloadPath = picked;
        SettingsService.Current.DefaultDownloadPath = picked;
        SettingsService.Save();
    }

    [RelayCommand]
    private void ResetDownload()
    {
        var def = SettingsService.SystemDefaultDownloadPath;
        DefaultDownloadPath = def;
        SettingsService.Current.DefaultDownloadPath = def;
        SettingsService.Save();
    }

    [RelayCommand]
    private async Task BrowseDataAsync()
    {
        var picked = await PickFolderAsync();
        if (picked is null) return;
        DefaultDataPath = picked;
        SettingsService.Current.DefaultDataPath = picked;
        SettingsService.Save();
    }

    [RelayCommand]
    private void ResetData()
    {
        var def = SettingsService.SystemDefaultDataPath;
        DefaultDataPath = def;
        SettingsService.Current.DefaultDataPath = def;
        SettingsService.Save();
    }

    [RelayCommand]
    private void OpenGitHub()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(GitHubUrl)
            {
                UseShellExecute = true
            });
        }
        catch { /* Ignore if browser fails to open */ }
    }

    [RelayCommand]
    private async Task ExportBackupAsync()
    {
        try
        {
            // Get password from user
            var password = await PromptPasswordAsync(
                Loc.Tr("settings.backup.password.title"),
                Loc.Tr("settings.backup.password.export.label")
            );

            if (string.IsNullOrEmpty(password))
                return;

            // Pick save location
            var filePath = await PickSaveFileAsync();
            if (string.IsNullOrEmpty(filePath))
                return;

            // Export backup
            ConfigBackupService.ExportConfig(password, filePath);
            await ShowMessageAsync(Loc.Tr("settings.backup.export.success"));
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(string.Format(Loc.Tr("settings.backup.export.error"), ex.Message));
        }
    }

    [RelayCommand]
    private async Task ImportBackupAsync()
    {
        try
        {
            // Pick backup file
            var filePath = await PickOpenFileAsync();
            if (string.IsNullOrEmpty(filePath))
                return;

            // Get password from user
            var password = await PromptPasswordAsync(
                Loc.Tr("settings.backup.password.title"),
                Loc.Tr("settings.backup.password.import.label")
            );

            if (string.IsNullOrEmpty(password))
                return;

            // Import backup
            ConfigBackupService.ImportConfig(password, filePath);

            // Apply imported settings to running application
            var settings = SettingsService.Current;

            // Apply theme
            var theme = settings.Theme switch
            {
                "Dark" => ThemeVariant.Dark,
                "Light" => ThemeVariant.Light,
                _ => ThemeVariant.Default
            };
            if (Application.Current is not null)
            {
                Application.Current.RequestedThemeVariant = theme;
                _main.IsDarkMode = Application.Current.ActualThemeVariant == ThemeVariant.Dark;
            }

            // Apply accent color
            App.ApplyAccentColor(settings.AccentColor);
            _main.AccentColor = settings.AccentColor;
            AccentColor = settings.AccentColor;

            // Apply language
            Loc.Language = settings.Language;

            // Apply window topmost
            _main.IsTopmost = settings.WindowTopmost;

            // Apply background image
            _main.ApplyBackgroundImage(settings.BackgroundImagePath, settings.BackgroundOpacity);
            BackgroundImagePath = settings.BackgroundImagePath;
            BackgroundOpacity = settings.BackgroundOpacity;

            await ShowMessageAsync(Loc.Tr("settings.backup.import.success"));

            // Reload hosts in main window
            var config = App.LoadConfig();
            _main.HostManager.Hosts.Clear();
            foreach (var host in config.Hosts)
                _main.HostManager.Hosts.Add(host);
            if (_main.HostManager.Hosts.Count > 0)
                _main.HostManager.SelectedHost = _main.HostManager.Hosts[0];
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(string.Format(Loc.Tr("settings.backup.import.error"), ex.Message));
        }
    }

    private static async Task<string?> PromptPasswordAsync(string title, string label)
    {
        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var owner = lifetime?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? lifetime?.MainWindow;
        if (owner is null) return null;

        var dialog = new PasswordInputDialog
        {
            HeaderTitle = title,
            Message = label,
            Placeholder = LocalizationService.Instance.Tr("settings.backup.password.placeholder")
        };

        return await dialog.ShowDialog<string?>(owner);
    }

    private static async Task ShowMessageAsync(string message)
    {
        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var owner = lifetime?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? lifetime?.MainWindow;
        if (owner is null) return;

        var dialog = new MessageDialog
        {
            HeaderTitle = LocalizationService.Instance.Tr("settings.backup"),
            Message = message
        };

        await dialog.ShowDialog(owner);
    }

    private static async Task<string?> PickSaveFileAsync()
    {
        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var owner = lifetime?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? lifetime?.MainWindow;
        if (owner is null) return null;

        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = LocalizationService.Instance.Tr("settings.backup.export"),
            DefaultExtension = "sftp-backup",
            SuggestedFileName = $"sftp-config-{DateTime.Now:yyyyMMdd-HHmmss}.sftp-backup",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("SFTP Backup")
                {
                    Patterns = new[] { "*.sftp-backup" }
                }
            }
        });

        return file?.TryGetLocalPath();
    }

    private static async Task<string?> PickOpenFileAsync()
    {
        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var owner = lifetime?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? lifetime?.MainWindow;
        if (owner is null) return null;

        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = LocalizationService.Instance.Tr("settings.backup.import"),
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("SFTP Backup")
                {
                    Patterns = new[] { "*.sftp-backup" }
                }
            }
        });

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    private static async Task<string?> PickFolderAsync()
    {
        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var owner = lifetime?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? lifetime?.MainWindow;
        if (owner is null) return null;

        var folders = await owner.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { AllowMultiple = false });
        return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
    }

    private static async Task<string?> PickImageFileAsync()
    {
        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var owner = lifetime?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? lifetime?.MainWindow;
        if (owner is null) return null;

        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = LocalizationService.Instance.Tr("settings.background.browse"),
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Image Files")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp" }
                }
            }
        });

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }
}

public record LanguageOption(string Code, string Display);

