using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sy_ftp.Helpers;
using sy_ftp.Models;
using sy_ftp.Services;

namespace sy_ftp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IFileWatcherService _fileWatcher;

    /// <summary>Live sessions keyed by host id.</summary>
    private readonly Dictionary<Guid, HostSession> _sessions = new();

    public HostManagerViewModel HostManager { get; }
    public FileBrowserViewModel FileBrowser { get; }
    public LocalizationService Loc => LocalizationService.Instance;

    [ObservableProperty]
    private bool _isTopmost;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotConnected))]
    private bool _isConnected;

    public bool IsNotConnected => !IsConnected;

    [ObservableProperty]
    private string _statusText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStatusLoading))]
    private bool _isBusy;

    /// <summary>True when either the connection is in progress or the browser is loading a directory.</summary>
    public bool IsStatusLoading => IsBusy || FileBrowser.IsLoading;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private string _accentColor = "#2296F5";

    [ObservableProperty]
    private string? _backgroundImagePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PanelFillOpacity))]
    [NotifyPropertyChangedFor(nameof(HasBackgroundImage))]
    private Bitmap? _backgroundImage;

    [ObservableProperty]
    private double _backgroundOpacity = 0.3;

    /// <summary>True when a background image is currently loaded.</summary>
    public bool HasBackgroundImage => BackgroundImage is not null;

    /// <summary>
    /// Opacity for panel fill layers (sidebar, file browser, status bar).
    /// Full opacity when no background image is set, subtle tint otherwise so
    /// the background image shows through while panels still feel like cards.
    /// </summary>
    public double PanelFillOpacity => BackgroundImage is null ? 1.0 : 0.25;

    /// <summary>Extended palette used by the Settings window.</summary>
    public IReadOnlyList<AccentColorOption> AccentColors => AccentPalette.Options;

    public MainWindowViewModel() : this(new FileWatcherService()) { }

    public MainWindowViewModel(IFileWatcherService fileWatcher)
    {
        _fileWatcher = fileWatcher;
        HostManager = new HostManagerViewModel();
        FileBrowser = new FileBrowserViewModel(new FtpService(), fileWatcher);

        _statusText = Loc.Tr("status.disconnected");

        _isDarkMode = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

        var saved = App.LoadAccentColor();
        _accentColor = saved;
        App.ApplyAccentColor(saved);

        // Load background settings
        var settings = SettingsService.Current;
        _backgroundImagePath = settings.BackgroundImagePath;
        _backgroundOpacity = settings.BackgroundOpacity;
        LoadBackgroundImageFile(_backgroundImagePath);

        // Load persisted config
        var config = App.LoadConfig();
        _isTopmost = config.WindowTopmost;
        foreach (var host in config.Hosts)
            HostManager.Hosts.Add(host);
        if (HostManager.Hosts.Count > 0)
            HostManager.SelectedHost = HostManager.Hosts[0];

        // Auto-save on host changes
        HostManager.HostDataChanged += (_, _) => SaveConfig();

        // Bubble FileBrowser.IsLoading into IsStatusLoading for the status-bar spinner
        FileBrowser.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FileBrowserViewModel.IsLoading))
                OnPropertyChanged(nameof(IsStatusLoading));
        };

        // Swap file browser session on host selection change
        HostManager.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(HostManagerViewModel.SelectedHost))
                await OnSelectedHostChangedAsync();
        };

        // Keep translated status strings in sync when language changes
        Loc.LanguageChanged += (_, _) => RefreshTranslatedStatus();
    }

    private void RefreshTranslatedStatus()
    {
        if (IsConnected && HostManager.SelectedHost is { } host)
            StatusText = Loc.Tr("status.connected", host.Name);
        else if (!IsBusy)
            StatusText = Loc.Tr("status.disconnected");
    }

    private async Task OnSelectedHostChangedAsync()
    {
        var host = HostManager.SelectedHost;
        if (host is null)
        {
            await FileBrowser.ActivateSessionAsync(null, CancellationToken.None);
            IsConnected = false;
            StatusText = Loc.Tr("status.disconnected");
            return;
        }

        if (_sessions.TryGetValue(host.Id, out var session))
        {
            await FileBrowser.ActivateSessionAsync(session, CancellationToken.None);
            IsConnected = true;
            StatusText = Loc.Tr("status.connected", host.Name);
        }
        else
        {
            await FileBrowser.ActivateSessionAsync(null, CancellationToken.None);
            IsConnected = false;
            StatusText = Loc.Tr("status.disconnected");
        }
    }

    private void SaveConfig()
    {
        App.SaveConfig(new AppConfig
        {
            Hosts = HostManager.Hosts.ToList(),
            WindowTopmost = IsTopmost,
        });
    }

    partial void OnIsTopmostChanged(bool value) => SaveConfig();

    [RelayCommand]
    private void SetAccentColor(AccentColorOption? option)
    {
        if (option is null) return;
        AccentColor = option.Hex;
        App.ApplyAccentColor(option.Hex);
        App.SaveAccentColor(option.Hex);
    }

    public void ApplyAccentHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return;
        AccentColor = hex;
        App.ApplyAccentColor(hex);
        App.SaveAccentColor(hex);
    }

    public void ApplyBackgroundImage(string? path, double opacity)
    {
        BackgroundImagePath = path;
        BackgroundOpacity = opacity;
        LoadBackgroundImageFile(path);
        var settings = SettingsService.Current;
        settings.BackgroundImagePath = path;
        settings.BackgroundOpacity = opacity;
        SettingsService.Save();
    }

    private void LoadBackgroundImageFile(string? path)
    {
        BackgroundImage?.Dispose();
        BackgroundImage = null;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        try
        {
            BackgroundImage = new Bitmap(path);
        }
        catch
        {
            BackgroundImage = null;
        }
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        var theme = value ? ThemeVariant.Dark : ThemeVariant.Light;
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = theme;
            App.SaveTheme(theme);
            App.ApplyAccentColor(AccentColor);
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var lifetime = Application.Current?.ApplicationLifetime
            as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var mainWindow = lifetime?.MainWindow;
        if (mainWindow is null) return;

        var dlg = new Views.SettingsWindow { DataContext = new SettingsViewModel(this) };
        await dlg.ShowDialog(mainWindow);
    }

    /// <summary>
    /// Prompts for password if the host doesn't have one saved.
    /// Returns the password to use (either saved or user-provided), or null if cancelled.
    /// </summary>
    private async Task<string?> PromptPasswordIfNeededAsync(FtpHost host)
    {
        if (!string.IsNullOrEmpty(host.Password))
            return host.Password;

        var lifetime = Application.Current?.ApplicationLifetime
            as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var mainWindow = lifetime?.MainWindow;
        if (mainWindow is null) return null;

        var (password, remember) = await Views.PasswordDialog.ShowAsync(mainWindow, host.Name);

        if (password is null)
        {
            StatusText = Loc.Tr("status.cancelled");
            return null;
        }

        // Only save to host if user checked "Remember"
        if (remember)
        {
            host.Password = password;
            SaveConfig();
        }

        return password;
    }

    [RelayCommand]
    private async Task ConnectAsync(CancellationToken ct)
    {
        var host = HostManager.SelectedHost;
        if (host is null) return;

        // Already connected — just ensure file browser is pointed at this session
        if (_sessions.TryGetValue(host.Id, out var existing))
        {
            await FileBrowser.ActivateSessionAsync(existing, ct);
            IsConnected = true;
            StatusText = Loc.Tr("status.connected", host.Name);
            return;
        }

        // Prompt for password if needed
        var password = await PromptPasswordIfNeededAsync(host);
        if (password is null)
            return;

        IsBusy = true;
        StatusText = Loc.Tr("status.connecting");
        var ftp = new FtpService();
        try
        {
            // Temporarily set password for this connection
            var originalPassword = host.Password;
            host.Password = password;

            await ftp.ConnectAsync(host, ct);

            // Restore original password if it wasn't saved (user didn't check "Remember")
            if (string.IsNullOrEmpty(originalPassword))
                host.Password = originalPassword;

            var homeDir = await ftp.GetWorkingDirectoryAsync(ct);
            var session = new HostSession { HostId = host.Id, Host = host, Ftp = ftp, CurrentPath = homeDir };
            _sessions[host.Id] = session;
            host.IsConnected = true;

            await FileBrowser.ActivateSessionAsync(session, ct);
            IsConnected = true;
            StatusText = Loc.Tr("status.connected", host.Name);
        }
        catch (Exception ex)
        {
            try { await ftp.DisconnectAsync(CancellationToken.None); } catch { }
            StatusText = Loc.Tr("status.error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync(CancellationToken ct)
    {
        var host = HostManager.SelectedHost;
        if (host is null) return;
        if (!_sessions.TryGetValue(host.Id, out var session)) return;

        FileBrowser.StopAllWatchers();
        try { await session.Ftp.DisconnectAsync(ct); } catch { }
        _sessions.Remove(host.Id);
        host.IsConnected = false;

        await FileBrowser.ActivateSessionAsync(null, ct);
        IsConnected = false;
        StatusText = Loc.Tr("status.disconnected");
    }

    [RelayCommand]
    private void ToggleTopmost()
    {
        IsTopmost = !IsTopmost;
    }

    /// <summary>
    /// Sessions accessor for other features (Transfer To).
    /// Only exposes hosts that currently have a live session.
    /// </summary>
    public IReadOnlyDictionary<Guid, HostSession> Sessions => _sessions;

    /// <summary>
    /// Ensure a live session exists for the given host. Used by the Transfer-to panel
    /// so the connection it opens is shared with the main window (the sidebar's
    /// connected indicator lights up automatically via FtpHost.IsConnected).
    /// Does NOT switch the main file browser to this host.
    /// </summary>
    public async Task<HostSession?> EnsureSessionAsync(FtpHost host, CancellationToken ct)
    {
        if (_sessions.TryGetValue(host.Id, out var existing))
            return existing;

        // Prompt for password if needed
        var password = await PromptPasswordIfNeededAsync(host);
        if (password is null)
            return null;

        var ftp = new FtpService();

        // Temporarily set password for this connection
        var originalPassword = host.Password;
        host.Password = password;

        await ftp.ConnectAsync(host, ct);

        // Restore original password if it wasn't saved (user didn't check "Remember")
        if (string.IsNullOrEmpty(originalPassword))
            host.Password = originalPassword;

        var homeDir = await ftp.GetWorkingDirectoryAsync(ct);
        var session = new HostSession { HostId = host.Id, Host = host, Ftp = ftp, CurrentPath = homeDir };
        _sessions[host.Id] = session;
        host.IsConnected = true;
        return session;
    }

    /// <summary>Tear down a session opened via EnsureSessionAsync / ConnectAsync.</summary>
    public async Task ReleaseSessionAsync(Guid hostId, CancellationToken ct)
    {
        if (!_sessions.TryGetValue(hostId, out var session)) return;

        // If the main file browser is using this session, clear it first.
        if (FileBrowser.ActiveSession?.HostId == hostId)
            await FileBrowser.ActivateSessionAsync(null, ct);

        FileBrowser.StopAllWatchers();
        try { await session.Ftp.DisconnectAsync(ct); } catch { }
        _sessions.Remove(hostId);

        var host = HostManager.Hosts.FirstOrDefault(h => h.Id == hostId);
        if (host is not null) host.IsConnected = false;

        if (HostManager.SelectedHost?.Id == hostId)
        {
            IsConnected = false;
            StatusText = Loc.Tr("status.disconnected");
        }
    }
}
