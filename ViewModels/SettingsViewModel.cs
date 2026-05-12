using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sy_ftp.Helpers;
using sy_ftp.Models;
using sy_ftp.Services;

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
    private int _selectedSectionIndex;

    public bool IsGeneralSelected => SelectedSectionIndex == 0;
    public bool IsAppearanceSelected => SelectedSectionIndex == 1;
    public bool IsPathsSelected => SelectedSectionIndex == 2;

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
    private string _accentColor = "#4050B5";

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

    private static async Task<string?> PickFolderAsync()
    {
        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var owner = lifetime?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? lifetime?.MainWindow;
        if (owner is null) return null;

        var folders = await owner.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { AllowMultiple = false });
        return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
    }
}

public record LanguageOption(string Code, string Display);

