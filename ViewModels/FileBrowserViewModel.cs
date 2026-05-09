using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sy_ftp.Models;
using sy_ftp.Services;

namespace sy_ftp.ViewModels;

public partial class FileBrowserViewModel : ViewModelBase
{
    private readonly IFtpService _ftp;
    private readonly IFileWatcherService _fileWatcher;

    [ObservableProperty]
    private ObservableCollection<RemoteFile> _files = [];

    [ObservableProperty]
    private string _currentPath = "/";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    private bool _isLoading;

    [ObservableProperty]
    private RemoteFile? _selectedFile;

    public bool IsNotLoading => !IsLoading;

    public FileBrowserViewModel(IFtpService ftp, IFileWatcherService fileWatcher)
    {
        _ftp = ftp;
        _fileWatcher = fileWatcher;
    }

    [RelayCommand]
    private async Task NavigateAsync(RemoteFile? dir, CancellationToken ct)
    {
        if (dir is not { IsDirectory: true }) return;
        await LoadDirectoryAsync(dir.FullPath, ct);
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct)
    {
        await LoadDirectoryAsync(CurrentPath, ct);
    }

    [RelayCommand]
    private async Task DownloadAsync(RemoteFile? file, CancellationToken ct)
    {
        if (file is not { IsDirectory: false }) return;
        IsLoading = true;
        try
        {
            var localPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                file.Name);
            await _ftp.DownloadFileAsync(file.FullPath, localPath, ct: ct);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(RemoteFile? file, CancellationToken ct)
    {
        if (file is null) return;
        await _ftp.DeleteFileAsync(file.FullPath, ct);
        await RefreshAsync(ct);
    }

    [RelayCommand]
    private async Task EditRemoteAsync(RemoteFile? file, CancellationToken ct)
    {
        if (file is not { IsDirectory: false }) return;
        IsLoading = true;
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "sy-ftp");
            Directory.CreateDirectory(tempDir);
            var tempPath = Path.Combine(tempDir, file.Name);

            await _ftp.DownloadFileAsync(file.FullPath, tempPath, ct: ct);

            using var watcher = _fileWatcher.StartWatching(tempPath, async _ =>
            {
                await _ftp.UploadFileAsync(tempPath, file.FullPath);
            });

            Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });

            // Keep alive until the user closes the editor.
            // In a real implementation this would use a more sophisticated lifecycle.
            await Task.Delay(Timeout.Infinite, ct).ContinueWith(_ => { });
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task UploadViaDragDropAsync(IEnumerable<string> localPaths, CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            foreach (var path in localPaths)
            {
                if (File.Exists(path))
                {
                    var name = Path.GetFileName(path);
                    await _ftp.UploadFileAsync(path, $"{CurrentPath}/{name}", ct: ct);
                }
                else if (Directory.Exists(path))
                {
                    await UploadDirectoryRecursiveAsync(path, CurrentPath, ct);
                }
            }
            await RefreshAsync(ct);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UploadDirectoryRecursiveAsync(string localDir, string remoteDir, CancellationToken ct)
    {
        var dirName = Path.GetFileName(localDir);
        var targetDir = $"{remoteDir}/{dirName}";
        await _ftp.CreateDirectoryAsync(targetDir, ct);

        foreach (var file in Directory.GetFiles(localDir))
        {
            var name = Path.GetFileName(file);
            await _ftp.UploadFileAsync(file, $"{targetDir}/{name}", ct: ct);
        }

        foreach (var sub in Directory.GetDirectories(localDir))
        {
            await UploadDirectoryRecursiveAsync(sub, targetDir, ct);
        }
    }

    public async Task LoadDirectoryAsync(string path, CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            await _ftp.ChangeDirectoryAsync(path, ct);
            CurrentPath = await _ftp.GetWorkingDirectoryAsync(ct);
            var items = await _ftp.ListDirectoryAsync(CurrentPath, ct);
            Files = new ObservableCollection<RemoteFile>(items);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
