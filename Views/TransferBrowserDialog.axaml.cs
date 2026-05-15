using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using sy_ftp.Models;
using sy_ftp.Services;
using sy_ftp.ViewModels;

namespace sy_ftp.Views;

public partial class TransferBrowserDialog : Window
{
    private MainWindowViewModel? _mainVm;
    private IFtpService? _sourceFtp;
    private IReadOnlyList<RemoteFile>? _sources;

    private FtpHost? _destHost;          // currently picked host
    private IFtpService? _destFtp;       // active destination connection (shared session)
    private string _currentPath = "/";
    private bool _busy;

    public TransferBrowserDialog()
    {
        InitializeComponent();
        ApplyShadow();
        ActualThemeVariantChanged += (_, _) => ApplyShadow();
    }

    /// <summary>
    /// Configure the panel. The panel uses the main window's session dictionary so any
    /// connection made here shows up in the sidebar and vice-versa.
    /// </summary>
    public void Configure(MainWindowViewModel mainVm,
                          IFtpService sourceFtp,
                          IReadOnlyList<RemoteFile> sources)
    {
        _mainVm = mainVm;
        _sourceFtp = sourceFtp;
        _sources = sources;

        HostCombo.ItemsSource = mainVm.HostManager.Hosts;
        TargetSummary.Text = sources.Count == 1
            ? $"1 item queued from source"
            : $"{sources.Count} items queued from source";
    }

    // ── Host picker ──────────────────────────────────────────────────
    private void OnHostSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (HostCombo.SelectedItem is FtpHost host)
        {
            // If the picked host already has a live session, jump straight to the browser.
            if (_mainVm?.Sessions.TryGetValue(host.Id, out var existing) == true)
            {
                _destHost = host;
                _destFtp = existing.Ftp;
                EnterConnectedState();
                _ = LoadPathAsync(existing.CurrentPath ?? "/");
            }
            else
            {
                _destHost = host;
                _destFtp = null;
                EnterDisconnectedState();
            }
        }
        else
        {
            _destHost = null;
            _destFtp = null;
            EnterDisconnectedState();
        }
    }

    private async void OnConnectClick(object? sender, RoutedEventArgs e)
    {
        if (_mainVm is null || _destHost is null || _busy) return;
        ShowLoading($"Connecting to {_destHost.Name}…");
        try
        {
            var session = await _mainVm.EnsureSessionAsync(_destHost, CancellationToken.None);
            if (session is null)
            {
                // User cancelled password prompt
                _destFtp = null;
                EnterDisconnectedState();
                return;
            }
            _destFtp = session.Ftp;
            EnterConnectedState();
            await LoadPathAsync(session.CurrentPath ?? "/");
        }
        catch (Exception ex)
        {
            _destFtp = null;
            ShowError($"Failed to connect: {ex.Message}");
            EnterDisconnectedState();
        }
    }

    private async void OnDisconnectClick(object? sender, RoutedEventArgs e)
    {
        if (_mainVm is null || _destHost is null || _busy) return;
        try { await _mainVm.ReleaseSessionAsync(_destHost.Id, CancellationToken.None); } catch { }
        _destFtp = null;
        _currentPath = "/";
        PathBlock.Text = "/";
        DirListBox.ItemsSource = null;
        EnterDisconnectedState();
    }

    private void EnterConnectedState()
    {
        ConnectButton.IsVisible = false;
        DisconnectButton.IsVisible = true;
        DirListBox.IsVisible = true;
        EmptyPanel.IsVisible = false;
        LoadingPanel.IsVisible = false;
        ErrorLabel.IsVisible = false;
        TransferButton.IsEnabled = true;
        if (_destHost is not null)
            TargetSummary.Text = (_sources?.Count ?? 0) == 1
                ? $"1 item → {_destHost.Name}"
                : $"{_sources?.Count ?? 0} items → {_destHost.Name}";
    }

    private void EnterDisconnectedState()
    {
        ConnectButton.IsVisible = true;
        ConnectButton.IsEnabled = _destHost is not null;
        DisconnectButton.IsVisible = false;
        DirListBox.IsVisible = false;
        LoadingPanel.IsVisible = false;
        EmptyPanel.IsVisible = true;
        EmptyLabel.Text = _destHost is null
            ? "Pick a destination host, then click Connect"
            : $"Ready to connect to {_destHost.Name}";
        ErrorLabel.IsVisible = false;
        TransferButton.IsEnabled = false;
        _busy = false;
    }

    // ── Directory browsing ───────────────────────────────────────────
    private async Task LoadPathAsync(string path)
    {
        if (_destFtp is null) return;
        ShowLoading("Loading...");
        try
        {
            await _destFtp.ChangeDirectoryAsync(path);
            _currentPath = await _destFtp.GetWorkingDirectoryAsync();
            PathBlock.Text = _currentPath;
            var items = await _destFtp.ListDirectoryAsync(_currentPath);
            var dirs = items.Where(f => f.IsDirectory).OrderBy(f => f.Name).ToList();
            DirListBox.ItemsSource = dirs;
            HideLoading();
            DirListBox.IsVisible = true;
            EmptyPanel.IsVisible = false;
            TransferButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void OnDirDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_busy) return;
        if (DirListBox.SelectedItem is RemoteFile { IsDirectory: true } f)
            await LoadPathAsync(f.FullPath);
    }

    private async void OnUpClick(object? sender, RoutedEventArgs e)
    {
        if (_busy || _destFtp is null) return;
        if (_currentPath == "/") return;
        var trimmed = _currentPath.TrimEnd('/');
        var idx = trimmed.LastIndexOf('/');
        var parent = idx <= 0 ? "/" : trimmed[..idx];
        await LoadPathAsync(parent);
    }

    private async void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        if (_busy || _destFtp is null) return;
        await LoadPathAsync(_currentPath);
    }

    // ── Transfer ─────────────────────────────────────────────────────
    private async void OnTransferClick(object? sender, RoutedEventArgs e)
    {
        if (_busy) return;
        if (_destFtp is null || _sourceFtp is null || _sources is null) return;
        if (!_sourceFtp.IsConnected)
        {
            ShowError("Source host is no longer connected.");
            return;
        }

        _busy = true;
        ProgressPanel.IsVisible = true;
        TransferButton.IsEnabled = false;
        ErrorLabel.IsVisible = false;

        var tempRoot = Path.Combine(Path.GetTempPath(), "SY-FTP", "transfer", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                var src = _sources[i];
                var label = _sources.Count > 1
                    ? $"{src.Name} ({i + 1}/{_sources.Count})"
                    : src.Name;

                int index = i;
                var dlProgress = new Progress<double>(pct =>
                {
                    ProgressLabel.Text = $"Downloading {label}… {pct:F0}%";
                    ProgressBarCtl.Value = (index * 100.0 + pct * 0.5) / _sources.Count;
                });
                var ulProgress = new Progress<double>(pct =>
                {
                    ProgressLabel.Text = $"Uploading {label}… {pct:F0}%";
                    ProgressBarCtl.Value = (index * 100.0 + 50 + pct * 0.5) / _sources.Count;
                });

                if (src.IsDirectory)
                {
                    var localDir = Path.Combine(tempRoot, src.Name);
                    await _sourceFtp.DownloadDirectoryAsync(src.FullPath, localDir, dlProgress);
                    var destDir = $"{_currentPath.TrimEnd('/')}/{src.Name}";
                    await UploadDirectoryAsync(localDir, destDir, ulProgress);
                }
                else
                {
                    var localPath = Path.Combine(tempRoot, src.Name);
                    await _sourceFtp.DownloadFileAsync(src.FullPath, localPath, dlProgress);
                    var destPath = $"{_currentPath.TrimEnd('/')}/{src.Name}";
                    await _destFtp.UploadFileAsync(localPath, destPath, ulProgress);
                }
            }

            ProgressBarCtl.Value = 100;
            ProgressLabel.Text = $"Transferred {_sources.Count} item(s) to {_destHost?.Name} — {_currentPath}";
            // Refresh the dir list so the user sees what they just sent.
            await LoadPathAsync(_currentPath);
        }
        catch (Exception ex)
        {
            ShowError($"Transfer failed: {ex.Message}");
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
            _busy = false;
            TransferButton.IsEnabled = _destFtp is not null;
        }
    }

    private async Task UploadDirectoryAsync(string localDir, string remoteDir, IProgress<double> progress)
    {
        if (_destFtp is null) return;
        await _destFtp.CreateDirectoryAsync(remoteDir);
        var files = Directory.GetFiles(localDir, "*", SearchOption.AllDirectories);
        int done = 0;
        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(localDir, file).Replace('\\', '/');
            var remotePath = $"{remoteDir}/{rel}";
            var remoteParent = remotePath[..remotePath.LastIndexOf('/')];
            try { await _destFtp.CreateDirectoryAsync(remoteParent); } catch { }
            await _destFtp.UploadFileAsync(file, remotePath);
            done++;
            progress.Report(done * 100.0 / Math.Max(1, files.Length));
        }
    }

    // ── UI state helpers ─────────────────────────────────────────────
    private void ShowLoading(string msg)
    {
        _busy = true;
        LoadingLabel.Text = msg;
        LoadingPanel.IsVisible = true;
        EmptyPanel.IsVisible = false;
        DirListBox.IsVisible = false;
        ErrorLabel.IsVisible = false;
        TransferButton.IsEnabled = false;
    }

    private void HideLoading()
    {
        _busy = false;
        LoadingPanel.IsVisible = false;
        ErrorLabel.IsVisible = false;
    }

    private void ShowError(string msg)
    {
        _busy = false;
        LoadingPanel.IsVisible = false;
        ErrorLabel.Text = msg;
        ErrorLabel.IsVisible = true;
        DirListBox.IsVisible = false;
        EmptyPanel.IsVisible = false;
    }

    // ── Window chrome ────────────────────────────────────────────────
    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void ApplyShadow()
    {
        if (CardBorder is null) return;
        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        CardBorder.BoxShadow = isDark
            ? BoxShadows.Parse("0 0 24 0 #18FFFFFF")
            : BoxShadows.Parse("0 0 16 0 #0C000000");
    }

    private void OnTitleBarDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Button) return;
        BeginMoveDrag(e);
    }
}
