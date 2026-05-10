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
    private readonly IFtpService _ftp;
    private readonly IFileWatcherService _fileWatcher;

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
    private bool _isBusy;

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

    public MainWindowViewModel() : this(new FtpService(), new FileWatcherService()) { }

    public MainWindowViewModel(IFtpService ftp, IFileWatcherService fileWatcher)
    {
        _ftp = ftp;
        _fileWatcher = fileWatcher;
        HostManager = new HostManagerViewModel();
        FileBrowser = new FileBrowserViewModel(ftp, fileWatcher);

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

        IsBusy = true;
        StatusText = "Connecting...";
        try
        {
            await _ftp.ConnectAsync(host, ct);
            IsConnected = true;
            StatusText = $"Connected to {host.Name}";
            var homeDir = await _ftp.GetWorkingDirectoryAsync(ct);
            await FileBrowser.LoadDirectoryAsync(homeDir, ct);
        }
        catch (Exception ex)
        {
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
        FileBrowser.StopAllWatchers();
        await _ftp.DisconnectAsync(ct);
        IsConnected = false;
        StatusText = "Disconnected";
        FileBrowser.Files.Clear();
        FileBrowser.CurrentPath = "/";
        FileBrowser.ErrorMessage = "";
    }

    [RelayCommand]
    private void ToggleTopmost()
    {
        IsTopmost = !IsTopmost;
    }
}
