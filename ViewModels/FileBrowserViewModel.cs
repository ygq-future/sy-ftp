using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        var ordered = Files.OrderBy(f => !f.IsDirectory);

        IOrderedEnumerable<RemoteFile> sorted = SortColumn switch
        {
            FileSortColumn.Name => SortAscending
                ? ordered.ThenBy(f => f.Name)
                : ordered.ThenByDescending(f => f.Name),
            FileSortColumn.Size => SortAscending
                ? ordered.ThenBy(f => f.Size)
                : ordered.ThenByDescending(f => f.Size),
            FileSortColumn.LastModified => SortAscending
                ? ordered.ThenBy(f => f.LastModified)
                : ordered.ThenByDescending(f => f.LastModified),
            _ => ordered.ThenBy(f => f.Name)
        };

        var list = sorted.ToList();
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
        ErrorMessage = "";
        try
        {
            await _ftp.ChangeDirectoryAsync(path, ct);
            CurrentPath = await _ftp.GetWorkingDirectoryAsync(ct);
            var items = await _ftp.ListDirectoryAsync(CurrentPath, ct);
            Files = new ObservableCollection<RemoteFile>(items);
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
