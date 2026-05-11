using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    [ObservableProperty]
    private bool _isTopmost;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotConnected))]
    private bool _isConnected;

    public bool IsNotConnected => !IsConnected;

    [ObservableProperty]
    private string _statusText = "Disconnected";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStatusLoading))]
    private bool _isBusy;

    /// <summary>True when either the connection is in progress or the browser is loading a directory.</summary>
    public bool IsStatusLoading => IsBusy || FileBrowser.IsLoading;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private string _accentColor = "#4050B5";

    public IReadOnlyList<AccentColorOption> AccentColors { get; } =
    [
        new("Indigo", "#4050B5"),
        new("Teal", "#009688"),
        new("Green", "#4CAF50"),
        new("Amber", "#FF9800"),
        new("Red", "#EF5350"),
        new("Purple", "#7C4DFF"),
        new("Pink", "#E91E63"),
        new("Cyan", "#00BCD4"),
    ];

    public MainWindowViewModel() : this(new FileWatcherService()) { }

    public MainWindowViewModel(IFileWatcherService fileWatcher)
    {
        _fileWatcher = fileWatcher;
        HostManager = new HostManagerViewModel();
        FileBrowser = new FileBrowserViewModel(new FtpService(), fileWatcher);

        _isDarkMode = Application.Current?.RequestedThemeVariant == ThemeVariant.Dark;

        var saved = App.LoadAccentColor();
        _accentColor = saved;
        App.ApplyAccentColor(saved);

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
    }

    private async Task OnSelectedHostChangedAsync()
    {
        var host = HostManager.SelectedHost;
        if (host is null)
        {
            await FileBrowser.ActivateSessionAsync(null, CancellationToken.None);
            IsConnected = false;
            StatusText = "Disconnected";
            return;
        }

        if (_sessions.TryGetValue(host.Id, out var session))
        {
            await FileBrowser.ActivateSessionAsync(session, CancellationToken.None);
            IsConnected = true;
            StatusText = $"Connected to {host.Name}";
        }
        else
        {
            await FileBrowser.ActivateSessionAsync(null, CancellationToken.None);
            IsConnected = false;
            StatusText = "Disconnected";
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
    private async Task ConnectAsync(CancellationToken ct)
    {
        var host = HostManager.SelectedHost;
        if (host is null) return;

        // Already connected — just ensure file browser is pointed at this session
        if (_sessions.TryGetValue(host.Id, out var existing))
        {
            await FileBrowser.ActivateSessionAsync(existing, ct);
            IsConnected = true;
            StatusText = $"Connected to {host.Name}";
            return;
        }

        IsBusy = true;
        StatusText = "Connecting...";
        var ftp = new FtpService();
        try
        {
            await ftp.ConnectAsync(host, ct);
            var homeDir = await ftp.GetWorkingDirectoryAsync(ct);
            var session = new HostSession { HostId = host.Id, Ftp = ftp, CurrentPath = homeDir };
            _sessions[host.Id] = session;
            host.IsConnected = true;

            await FileBrowser.ActivateSessionAsync(session, ct);
            IsConnected = true;
            StatusText = $"Connected to {host.Name}";
        }
        catch (Exception ex)
        {
            try { await ftp.DisconnectAsync(CancellationToken.None); } catch { }
            StatusText = $"Error: {ex.Message}";
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
        StatusText = "Disconnected";
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
    public async Task<HostSession> EnsureSessionAsync(FtpHost host, CancellationToken ct)
    {
        if (_sessions.TryGetValue(host.Id, out var existing))
            return existing;

        var ftp = new FtpService();
        await ftp.ConnectAsync(host, ct);
        var homeDir = await ftp.GetWorkingDirectoryAsync(ct);
        var session = new HostSession { HostId = host.Id, Ftp = ftp, CurrentPath = homeDir };
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
            StatusText = "Disconnected";
        }
    }
}
