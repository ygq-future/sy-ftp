using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sy_ftp.Helpers;
using sy_ftp.Models;
using sy_ftp.Services;

namespace sy_ftp.ViewModels;

public enum FileSortColumn { Name, Size, LastModified }

public partial class FileBrowserViewModel : ViewModelBase
{
    private readonly IFtpService _ftp;
    private readonly IFileWatcherService _fileWatcher;

    [ObservableProperty]
    private ObservableCollection<RemoteFile> _files = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathSegments))]
    [NotifyPropertyChangedFor(nameof(ParentPath))]
    private string _currentPath = "/";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = "";

    private int _errorVersion;

    partial void OnErrorMessageChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var version = Interlocked.Increment(ref _errorVersion);
        _ = ClearErrorAfterDelay(version);
    }

    private async Task ClearErrorAfterDelay(int version)
    {
        await Task.Delay(2000);
        if (version == _errorVersion)
            ErrorMessage = "";
    }

    [ObservableProperty]
    private RemoteFile? _selectedFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NameSortArrow))]
    [NotifyPropertyChangedFor(nameof(SizeSortArrow))]
    [NotifyPropertyChangedFor(nameof(ModifiedSortArrow))]
    [NotifyPropertyChangedFor(nameof(NameSortActive))]
    [NotifyPropertyChangedFor(nameof(SizeSortActive))]
    [NotifyPropertyChangedFor(nameof(ModifiedSortActive))]
    private FileSortColumn _sortColumn = FileSortColumn.Name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NameSortArrow))]
    [NotifyPropertyChangedFor(nameof(SizeSortArrow))]
    [NotifyPropertyChangedFor(nameof(ModifiedSortArrow))]
    private bool _sortAscending = true;

    // ── Path breadcrumb editing ────────────────────────────────────────────

    [ObservableProperty]
    private bool _isEditingPath;

    [ObservableProperty]
    private string _editPathText = "/";

    [ObservableProperty]
    private string _pathError = "";

    [ObservableProperty]
    private bool _showCopyToast;

    partial void OnShowCopyToastChanged(bool value)
    {
        if (value)
            _ = ClearCopyToastAfterDelay();
    }

    private async Task ClearCopyToastAfterDelay()
    {
        await Task.Delay(1000);
        ShowCopyToast = false;
    }

    public bool IsNotLoading => !IsLoading;

    // ── Download progress ───────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotDownloading))]
    private bool _isDownloading;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private long _downloadedBytes;

    [ObservableProperty]
    private long _totalDownloadBytes;

    [ObservableProperty]
    private string _downloadStatusText = "";

    public bool IsNotDownloading => !IsDownloading;

    public string NameSortArrow => ArrowFor(FileSortColumn.Name);
    public string SizeSortArrow => ArrowFor(FileSortColumn.Size);
    public string ModifiedSortArrow => ArrowFor(FileSortColumn.LastModified);

    public bool NameSortActive => SortColumn == FileSortColumn.Name;
    public bool SizeSortActive => SortColumn == FileSortColumn.Size;
    public bool ModifiedSortActive => SortColumn == FileSortColumn.LastModified;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathSegments))]
    [NotifyPropertyChangedFor(nameof(OverflowPathSegments))]
    private bool _isPathOverflowing;

    public IEnumerable<PathSegment> PathSegments
    {
        get
        {
            var all = BuildAllSegments().ToList();
            if (!IsPathOverflowing) return all;

            var result = new List<PathSegment>();
            result.AddRange(all.Take(2));
            result.Add(new PathSegment("…", "/", false) { IsOverflow = true });
            result.AddRange(all.Skip(all.Count - 1));
            for (int i = 0; i < result.Count; i++)
                result[i] = result[i] with { IsLast = i == result.Count - 1 };
            return result;
        }
    }

    public IEnumerable<PathSegment> OverflowPathSegments
    {
        get
        {
            var all = BuildAllSegments().ToList();
            if (!IsPathOverflowing) return [];
            return all.Skip(2).Take(all.Count - 3);
        }
    }

    private IEnumerable<PathSegment> BuildAllSegments()
    {
        if (CurrentPath == "/")
            return [new PathSegment("/", "/", true)];

        var parts = CurrentPath.TrimStart('/').Split('/');
        var list = new List<PathSegment> { new("/", "/", false) };
        var path = "/";
        for (int i = 0; i < parts.Length; i++)
        {
            path = path == "/" ? $"/{parts[i]}" : $"{path}/{parts[i]}";
            list.Add(new PathSegment(parts[i], path, i == parts.Length - 1));
        }
        return list;
    }

    [RelayCommand]
    private void CopyPath() { }

    private string ArrowFor(FileSortColumn col)
        => SortColumn == col ? (SortAscending ? "▲" : "▼") : "";

    public FileBrowserViewModel(IFtpService ftp, IFileWatcherService fileWatcher)
    {
        _ftp = ftp;
        _fileWatcher = fileWatcher;
    }

    partial void OnCurrentPathChanged(string value) => IsPathOverflowing = false;

    partial void OnSortColumnChanged(FileSortColumn value) => ApplySort();
    partial void OnSortAscendingChanged(bool value) => ApplySort();

    [RelayCommand]
    private void ToggleSort(FileSortColumn column)
    {
        if (SortColumn == column)
            SortAscending = !SortAscending;
        else
        {
            SortColumn = column;
            SortAscending = true;
        }
    }

    [RelayCommand]
    private async Task NavigateToPathAsync(string path, CancellationToken ct)
    {
        await LoadDirectoryAsync(path, ct);
    }

    [RelayCommand]
    private void StartPathEdit()
    {
        EditPathText = CurrentPath;
        PathError = "";
        IsEditingPath = true;
    }

    [RelayCommand]
    private void CancelPathEdit()
    {
        IsEditingPath = false;
        PathError = "";
    }

    [RelayCommand]
    private async Task NavigateToEditPathAsync(CancellationToken ct)
    {
        PathError = "";
        try
        {
            await _ftp.ChangeDirectoryAsync(EditPathText, ct);
            await LoadDirectoryAsync(EditPathText, ct);
            IsEditingPath = false;
        }
        catch (Exception ex)
        {
            PathError = ex.Message;
        }
    }

    private void ApplySort()
    {
        // Keep ".." pinned at top, then dirs before files, then apply column sort
        var parent = Files.FirstOrDefault(f => f.IsParentEntry);
        var rest = Files.Where(f => !f.IsParentEntry).OrderBy(f => !f.IsDirectory);

        IOrderedEnumerable<RemoteFile> sorted = SortColumn switch
        {
            FileSortColumn.Name => SortAscending
                ? rest.ThenBy(f => f.Name)
                : rest.ThenByDescending(f => f.Name),
            FileSortColumn.Size => SortAscending
                ? rest.ThenBy(f => f.Size)
                : rest.ThenByDescending(f => f.Size),
            FileSortColumn.LastModified => SortAscending
                ? rest.ThenBy(f => f.LastModified)
                : rest.ThenByDescending(f => f.LastModified),
            _ => rest.ThenBy(f => f.Name)
        };

        var list = sorted.ToList();
        if (parent is not null) list.Insert(0, parent);
        Files.Clear();
        foreach (var f in list)
            Files.Add(f);
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
        SelectedFile = file;
        await DownloadSelectedAsync(ct);
    }

    [RelayCommand]
    private async Task DownloadSelectedAsync(CancellationToken ct)
    {
        var list = ResolveSelectedList();
        if (list.Count == 0) return;

        FtpPathHelper.Ensure();
        await DownloadListAsync(list, FtpPathHelper.DefaultDownloadDir, ct);
    }

    [RelayCommand]
    private async Task DownloadToAsync(CancellationToken ct)
    {
        var list = ResolveSelectedList();
        if (list.Count == 0) return;

        var lifetime = Avalonia.Application.Current?.ApplicationLifetime
            as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var mainWindow = lifetime?.MainWindow;
        if (mainWindow is null) return;

        var folders = await mainWindow.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "Choose download folder",
                AllowMultiple = false
            });
        if (folders.Count == 0) return;

        var targetDir = folders[0].TryGetLocalPath();
        if (string.IsNullOrEmpty(targetDir)) return;

        await DownloadListAsync(list, targetDir, ct);
    }

    private List<RemoteFile> ResolveSelectedList()
    {
        var list = SelectedFiles is { Count: > 0 }
            ? SelectedFiles.ToList()
            : SelectedFile is not null ? new List<RemoteFile> { SelectedFile } : new();
        return list.Where(f => !f.IsParentEntry).ToList();
    }

    private async Task DownloadListAsync(List<RemoteFile> list, string targetDir, CancellationToken ct)
    {
        IsDownloading = true;
        DownloadProgress = 0;
        DownloadedBytes = 0;
        TotalDownloadBytes = 0;

        try
        {
            for (int i = 0; i < list.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var file = list[i];
                var label = list.Count > 1
                    ? $"{file.Name} ({i + 1}/{list.Count})"
                    : file.Name;
                DownloadStatusText = $"Downloading {label}...";

                int index = i;
                int total = list.Count;
                var progress = new Progress<double>(pct =>
                {
                    DownloadProgress = (index * 100.0 + pct) / total;
                    DownloadStatusText = $"Downloading {label}... {pct:F0}%";
                });

                if (file.IsDirectory)
                {
                    var localDir = Path.Combine(targetDir, file.Name);
                    await _ftp.DownloadDirectoryAsync(file.FullPath, localDir, progress, ct);
                }
                else
                {
                    var localPath = Path.Combine(targetDir, file.Name);
                    await _ftp.DownloadFileAsync(file.FullPath, localPath, progress, ct);
                }
            }

            DownloadProgress = 100;
            DownloadStatusText = list.Count > 1
                ? $"Downloaded {list.Count} items"
                : $"Downloaded {list[0].Name}";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsDownloading = false;
        }
    }

    private void UpdateDownloadStatus(string fileName)
    {
        var down = DownloadedBytes;
        var total = TotalDownloadBytes;
        if (total <= 0)
        {
            DownloadStatusText = $"Downloading {fileName}...";
            return;
        }
        DownloadStatusText = $"Downloading {fileName}... {FormatBytes(down)} / {FormatBytes(total)}";
    }

    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes:N0} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
        };
    }

    // Multi-select — set by code-behind before commands execute
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllSelectedAreFiles))]
    [NotifyPropertyChangedFor(nameof(HasMultiSelection))]
    private IReadOnlyList<RemoteFile> _selectedFiles = Array.Empty<RemoteFile>();

    public bool AllSelectedAreFiles =>
        SelectedFiles is { Count: > 0 } && SelectedFiles.All(f => !f.IsDirectory);

    public bool HasMultiSelection => SelectedFiles.Count > 1;

    [RelayCommand]
    private async Task DeleteSelectedAsync(CancellationToken ct)
    {
        var files = SelectedFiles.Where(f => !f.IsParentEntry).ToList();
        if (files.Count == 0) return;
        try
        {
            foreach (var f in files)
            {
                if (f.IsDirectory)
                    await _ftp.DeleteDirectoryAsync(f.FullPath, ct);
                else
                    await _ftp.DeleteFileAsync(f.FullPath, ct);
            }
            await RefreshAsync(ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task EditSelectedAsync(CancellationToken ct)
    {
        var files = SelectedFiles;
        if (files is not { Count: > 0 }) return;
        foreach (var f in files)
        {
            if (f.IsDirectory) continue;
            IsLoading = true;
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "SY-FTP");
                Directory.CreateDirectory(tempDir);
                var tempPath = Path.Combine(tempDir, f.Name);

                await _ftp.DownloadFileAsync(f.FullPath, tempPath, ct: ct);

                using var watcher = _fileWatcher.StartWatching(tempPath, async _ =>
                {
                    await _ftp.UploadFileAsync(tempPath, f.FullPath);
                });

                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
            }
            catch (OperationCanceledException) { }
            finally
            {
                IsLoading = false;
            }
        }
    }


    public string? ParentPath
    {
        get
        {
            if (CurrentPath == "/") return null;
            var trimmed = CurrentPath.TrimEnd('/');
            var idx = trimmed.LastIndexOf('/');
            return idx <= 0 ? "/" : trimmed[..idx];
        }
    }

    public async Task MoveToFolderAsync(IReadOnlyList<RemoteFile> sources, RemoteFile targetDir, CancellationToken ct)
        => await MoveFilesToPathAsync(sources, targetDir.FullPath, ct);

    public async Task MoveFilesToPathAsync(IReadOnlyList<RemoteFile> sources, string targetPath, CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            foreach (var src in sources)
            {
                var destPath = $"{targetPath.TrimEnd('/')}/{src.Name}";
                await _ftp.MoveAsync(src.FullPath, destPath, ct);
            }
            await RefreshAsync(ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
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
        if (file.IsDirectory)
            await _ftp.DeleteDirectoryAsync(file.FullPath, ct);
        else
            await _ftp.DeleteFileAsync(file.FullPath, ct);
        await RefreshAsync(ct);
    }

    [RelayCommand]
    private async Task NewFolderAsync(CancellationToken ct)
    {
        try
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime
                as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var mainWindow = lifetime?.MainWindow;
            if (mainWindow is null) return;

            var dlg = new Views.InputDialog { Header = "New Folder", Label = "Folder name" };
            var result = await dlg.ShowDialog<bool?>(mainWindow);
            if (result != true) return;

            var name = dlg.Input;
            await _ftp.CreateDirectoryAsync($"{CurrentPath}/{name}", ct);
            await RefreshAsync(ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task NewFileAsync(CancellationToken ct)
    {
        try
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime
                as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var mainWindow = lifetime?.MainWindow;
            if (mainWindow is null) return;

            var dlg = new Views.InputDialog { Header = "New File", Label = "File name" };
            var result = await dlg.ShowDialog<bool?>(mainWindow);
            if (result != true) return;

            var name = dlg.Input;
            var remotePath = $"{CurrentPath}/{name}";
            var tempPath = Path.GetTempFileName();
            await _ftp.UploadFileAsync(tempPath, remotePath, ct: ct);
            try { File.Delete(tempPath); } catch { }
            await RefreshAsync(ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task EditRemoteAsync(RemoteFile? file, CancellationToken ct)
    {
        if (file is not { IsDirectory: false }) return;
        IsLoading = true;
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "SY-FTP");
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
            var paths = localPaths.ToList();
            if (paths.Count == 0) return;

            // Detect if all items share the same parent — means a folder was dragged
            string? commonParent = null;
            bool hasDirectory = false;
            foreach (var p in paths)
            {
                var parent = Path.GetDirectoryName(p);
                if (commonParent is null)
                    commonParent = parent;
                else if (!string.Equals(commonParent, parent, StringComparison.OrdinalIgnoreCase))
                {
                    commonParent = null;
                    break;
                }
                if (Directory.Exists(p)) hasDirectory = true;
            }

            var remoteTarget = CurrentPath;
            if (commonParent is not null && hasDirectory)
            {
                var folderName = Path.GetFileName(commonParent);
                remoteTarget = $"{CurrentPath}/{folderName}";
                await _ftp.CreateDirectoryAsync(remoteTarget, ct);
            }

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                    await UploadDirectoryRecursiveAsync(path, remoteTarget, ct);
                else if (File.Exists(path))
                {
                    var name = Path.GetFileName(path);
                    await _ftp.UploadFileAsync(path, $"{remoteTarget}/{name}", ct: ct);
                }
            }
            await RefreshAsync(ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
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
        ErrorMessage = "";
        try
        {
            await _ftp.ChangeDirectoryAsync(path, ct);
            CurrentPath = await _ftp.GetWorkingDirectoryAsync(ct);
            var items = await _ftp.ListDirectoryAsync(CurrentPath, ct);
            var list = new List<RemoteFile>();
            if (CurrentPath != "/")
            {
                var parentPath = ParentPath!;
                list.Add(new RemoteFile("..", parentPath, 0, true, DateTimeOffset.MinValue));
            }
            list.AddRange(items);
            Files = new ObservableCollection<RemoteFile>(list);
            ApplySort();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
