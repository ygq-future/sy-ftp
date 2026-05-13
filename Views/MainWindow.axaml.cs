using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using sy_ftp.Helpers;
using sy_ftp.Models;
using sy_ftp.ViewModels;
namespace sy_ftp.Views;

public partial class MainWindow : Window
{
    // ── Rubber-band selection state ─────────────────────────────────────
    private bool _rubberBandActive;
    private Point _rubberBandOrigin;
    private bool _rubberBandClearSelection;

    // ── Internal drag-move state ─────────────────────────────────────────
    private bool _internalDragPending;
    private bool _internalDragActive;
    private Point _internalDragStartPos;
    private IReadOnlyList<RemoteFile> _internalDragSources = Array.Empty<RemoteFile>();
    private RemoteFile? _dragOverFolder;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        HostListBox.DoubleTapped += OnHostDoubleTapped;
        WirePopupBackgrounds();

        PathScrollViewer.LayoutUpdated += OnPathLayoutUpdated;

        // Use tunnel routing so ListBoxItem selection doesn't consume pointer events
        FileListBox.AddHandler(InputElement.PointerPressedEvent, OnFileListPointerPressed,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        FileListBox.AddHandler(InputElement.PointerMovedEvent, OnFileListPointerMoved,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        FileListBox.AddHandler(InputElement.PointerReleasedEvent, OnFileListPointerReleased,
            RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private bool _pendingOverflowCheck;
    private void OnPathLayoutUpdated(object? sender, EventArgs e)
    {
        if (_pendingOverflowCheck) return;
        if (DataContext is not MainWindowViewModel vm) return;
        if (vm.FileBrowser.IsPathOverflowing) return;
        _pendingOverflowCheck = true;
        Dispatcher.Post(() =>
        {
            _pendingOverflowCheck = false;
            if (DataContext is not MainWindowViewModel vm) return;
            if (vm.FileBrowser.IsPathOverflowing) return;
            if (PathScrollViewer.Extent.Width > PathScrollViewer.Viewport.Width + 0.5)
                vm.FileBrowser.IsPathOverflowing = true;
        }, DispatcherPriority.Background);
    }

    private void WirePopupBackgrounds()
    {
        var app = Application.Current;
        if (app is null) return;

        var popup = TagComboBox.GetVisualDescendants().OfType<Popup>().FirstOrDefault();
        if (popup is not null)
        {
            popup.Opened += (_, _) =>
            {
                var border = popup.Child as Border
                    ?? popup.GetVisualDescendants().OfType<Border>().FirstOrDefault();
                if (border is not null && app.TryGetResource("SemiColorBackground1", app.ActualThemeVariant, out var bg))
                    border.Background = (IBrush)bg!;
            };
        }

        if (HostListBox.ContextMenu is ContextMenu cm)
        {
            cm.Opened += (_, _) =>
            {
                var menuPopup = cm.GetVisualDescendants().OfType<Popup>().FirstOrDefault();
                if (menuPopup is null) return;
                var border = menuPopup.Child as Border
                    ?? menuPopup.GetVisualDescendants().OfType<Border>().FirstOrDefault();
                if (border is not null && app.TryGetResource("SemiColorBackground1", app.ActualThemeVariant, out var bg))
                    border.Background = (IBrush)bg!;
            };
        }
    }

    private void OnHostDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm
            && vm.HostManager.SelectedHost is not null
            && vm.IsNotConnected
            && vm.ConnectCommand.CanExecute(null))
        {
            vm.ConnectCommand.Execute(null);
        }
    }

    private void OnFileListTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (ResolveRemoteFile(e.Source) is null)
            vm.FileBrowser.SelectedFile = null;
    }

    private void OnFileListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (vm.FileBrowser.SelectedFile is not { IsDirectory: true } dir) return;
        vm.FileBrowser.NavigateCommand.Execute(dir);
    }

    private void OnPathBarDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.IsConnected)
        {
            EnterPathEdit(vm);
            e.Handled = true;
        }
    }

    private void OnEditPathClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            EnterPathEdit(vm);
    }

    private void EnterPathEdit(MainWindowViewModel vm)
    {
        vm.FileBrowser.StartPathEditCommand.Execute(null);
        PathEditBox.Focus();
        PathEditBox.CaretIndex = PathEditBox.Text?.Length ?? 0;
    }

    private void OnAnywhereDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (!vm.FileBrowser.IsEditingPath) return;
        if (e.Source is TextBox) return;
        vm.FileBrowser.CancelPathEditCommand.Execute(null);
    }

    private void OnPathEditKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                vm.FileBrowser.NavigateToEditPathCommand.Execute(null);
                break;
            case Key.Escape:
                e.Handled = true;
                vm.FileBrowser.CancelPathEditCommand.Execute(null);
                break;
        }
    }

    private async void OnCopyPathClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(vm.FileBrowser.CurrentPath);
                vm.FileBrowser.ShowCopyToast = true;
            }
        }
    }

    // ── Caption buttons (custom min/max/close for extended client area) ──

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void OnMaxRestoreClick(object? sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        // Don't start a drag when the press originated on an interactive control.
        // ToggleButton extends Button, so a single check covers both.
        if (e.Source is Visual v && v.FindAncestorOfType<Button>(includeSelf: true) is not null) return;
        BeginMoveDrag(e);
    }

    private void OnTitleBarDoubleTapped(object? sender, TappedEventArgs e)
    {
        // Ignore double-taps on buttons — they have their own click handlers.
        if (e.Source is Visual v && v.FindAncestorOfType<Button>(includeSelf: true) is not null) return;
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        e.Handled = true;
    }

    // ── Pointer handlers (rubber-band + internal drag-move) ──────────

    private void OnFileListPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (DataContext is not MainWindowViewModel vm || !vm.IsConnected) return;

        // Ignore presses that originate inside the scrollbar — otherwise dragging
        // the scroll thumb would start a rubber-band selection.
        if (e.Source is Visual src && src.FindAncestorOfType<ScrollBar>() is not null)
            return;

        var file = ResolveRemoteFile(e.Source);
        if (file is null)
        {
            // Blank area: start rubber-band selection
            _rubberBandActive = true;
            _rubberBandOrigin = e.GetPosition(SelectionCanvas);
            _rubberBandClearSelection = (e.KeyModifiers & KeyModifiers.Control) == 0;
            Canvas.SetLeft(SelectionRect, _rubberBandOrigin.X);
            Canvas.SetTop(SelectionRect, _rubberBandOrigin.Y);
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
            return;
        }

        // Item pressed: skip ".." as drag source
        if (file.IsParentEntry) return;

        // Record as potential internal drag source
        _internalDragPending = true;
        _internalDragStartPos = e.GetCurrentPoint(FileListBox).Position;
        // Snapshot selected files at press time; if the pressed item isn't in the
        // selection yet, treat just that item as the drag source.
        SyncSelectedFilesToViewModel();
        var selected = vm.FileBrowser.SelectedFiles;
        _internalDragSources = selected.Contains(file)
            ? selected
            : new[] { file };
    }

    private void OnFileListPointerMoved(object? sender, PointerEventArgs e)
    {
        // Rubber-band update
        if (_rubberBandActive)
        {
            var pos = e.GetPosition(SelectionCanvas);
            var x = Math.Min(_rubberBandOrigin.X, pos.X);
            var y = Math.Min(_rubberBandOrigin.Y, pos.Y);
            var w = Math.Abs(pos.X - _rubberBandOrigin.X);
            var h = Math.Abs(pos.Y - _rubberBandOrigin.Y);
            Canvas.SetLeft(SelectionRect, x);
            Canvas.SetTop(SelectionRect, y);
            SelectionRect.Width = w;
            SelectionRect.Height = h;
            if (w > 2 || h > 2)
                SelectionRect.IsVisible = true;
            LiveUpdateRubberBandSelection();
            return;
        }

        if (!_internalDragPending && !_internalDragActive) return;

        var point = e.GetCurrentPoint(FileListBox);
        if (!point.Properties.IsLeftButtonPressed)
        {
            CancelInternalDrag();
            return;
        }

        if (_internalDragPending)
        {
            var diff = _internalDragStartPos - point.Position;
            if (Math.Abs(diff.X) < 6 && Math.Abs(diff.Y) < 6) return;
            _internalDragPending = false;
            _internalDragActive = true;

            // Show ghost
            DragGhostCanvas.IsVisible = true;
            DragGhostLabel.Text = _internalDragSources.Count == 1
                ? _internalDragSources[0].Name
                : $"{_internalDragSources.Count} items";
        }

        // Move ghost to follow cursor
        var ghostPos = e.GetPosition(DragGhostCanvas);
        Canvas.SetLeft(DragGhost, ghostPos.X + 14);
        Canvas.SetTop(DragGhost, ghostPos.Y + 10);

        // Highlight folder under cursor (including ".." which acts as parent)
        var hitPos = e.GetPosition(FileListBox);
        var hitElement = FileListBox.InputHitTest(hitPos) as Visual;
        var folder = ResolveRemoteFile(hitElement);
        var newTarget = folder is { IsDirectory: true } && !_internalDragSources.Contains(folder)
            ? folder : null;
        if (newTarget != _dragOverFolder)
        {
            SetDropHighlight(_dragOverFolder, false);
            _dragOverFolder = newTarget;
            SetDropHighlight(_dragOverFolder, true);
        }
    }

    private void OnFileListPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_rubberBandActive)
        {
            _rubberBandActive = false;
            SelectionRect.IsVisible = false;
            SyncSelectedFilesToViewModel();
            return;
        }

        if (_internalDragActive)
        {
            var sources = _internalDragSources;
            var target = _dragOverFolder;
            CancelInternalDrag();

            if (target is not null && DataContext is MainWindowViewModel vm)
            {
                if (target.IsParentEntry)
                    _ = vm.FileBrowser.MoveFilesToPathAsync(sources, target.FullPath, CancellationToken.None);
                else
                    _ = vm.FileBrowser.MoveToFolderAsync(sources, target, CancellationToken.None);
            }
        }
        else
        {
            CancelInternalDrag();
        }
    }

    private void CancelInternalDrag()
    {
        _internalDragPending = false;
        _internalDragActive = false;
        SetDropHighlight(_dragOverFolder, false);
        _dragOverFolder = null;
        _internalDragSources = Array.Empty<RemoteFile>();
        DragGhostCanvas.IsVisible = false;
    }

    private void SetDropHighlight(RemoteFile? file, bool on)
    {
        if (file is null) return;
        foreach (var item in FileListBox.GetVisualDescendants().OfType<ListBoxItem>())
        {
            if (item.DataContext is RemoteFile f && f.Equals(file))
            {
                if (on) item.Classes.Add("drop-target");
                else item.Classes.Remove("drop-target");
                break;
            }
        }
    }

    // ── Rubber-band selection ──────────────────────────────────────────

    private void LiveUpdateRubberBandSelection()
    {
        var origin = SelectionCanvas.TranslatePoint(
            new Point(Canvas.GetLeft(SelectionRect), Canvas.GetTop(SelectionRect)), FileListBox);
        if (origin is null) return;
        var rect = new Rect(origin.Value, new Size(SelectionRect.Width, SelectionRect.Height));

        var rectItems = new HashSet<RemoteFile>();
        foreach (var item in FileListBox.GetVisualDescendants().OfType<ListBoxItem>())
        {
            var itemTopLeft = item.TranslatePoint(default, FileListBox);
            if (itemTopLeft is null) continue;
            var itemRect = new Rect(itemTopLeft.Value, new Size(item.Bounds.Width, item.Bounds.Height));
            if (rect.Intersects(itemRect) && item.DataContext is RemoteFile rf)
                rectItems.Add(rf);
        }

        if (_rubberBandClearSelection)
        {
            var toRemove = new List<RemoteFile>();
            foreach (var item in FileListBox.SelectedItems?.OfType<RemoteFile>() ?? [])
            {
                if (!rectItems.Contains(item))
                    toRemove.Add(item);
            }
            foreach (var item in toRemove)
                FileListBox.SelectedItems!.Remove(item);
            foreach (var item in rectItems)
            {
                if (!FileListBox.SelectedItems!.Contains(item))
                    FileListBox.SelectedItems.Add(item);
            }
        }
        else
        {
            foreach (var item in rectItems)
            {
                if (!FileListBox.SelectedItems!.Contains(item))
                    FileListBox.SelectedItems.Add(item);
            }
        }

        SyncSelectedFilesToViewModel();
    }

    private void SyncSelectedFilesToViewModel()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var selected = FileListBox.SelectedItems;
            vm.FileBrowser.SelectedFiles = selected is null
                ? []
                : selected.OfType<RemoteFile>().ToArray();
        }
    }

    // ── Item context menu Click handlers ─────────────────────────────

    private void OnFileItemContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        SyncSelectedFilesToViewModel();
    }

    private void OnItemDownloadClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        SyncSelectedFilesToViewModel();
        vm.FileBrowser.DownloadSelectedCommand.Execute(null);
    }

    private void OnItemDownloadToClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        SyncSelectedFilesToViewModel();
        vm.FileBrowser.DownloadToCommand.Execute(null);
    }

    private void OnItemRemoteEditClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        SyncSelectedFilesToViewModel();
        vm.FileBrowser.EditSelectedCommand.Execute(null);
    }

    private async void OnItemOnlineEditClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        var file = vm.FileBrowser.SelectedFile;
        if (file is not { IsDirectory: false }) return;

        var tempPath = Path.Combine(Path.GetTempPath(), "SY-FTP", file.Name);
        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

        vm.FileBrowser.IsLoading = true;
        try
        {
            await vm.FileBrowser.FtpService.DownloadFileAsync(file.FullPath, tempPath);
            var content = await File.ReadAllTextAsync(tempPath);

            var win = new RemoteEditWindow();
            win.Load(file.Name, content);
            var result = await win.ShowDialog<string?>(this);

            if (result is not null)
            {
                await File.WriteAllTextAsync(tempPath, result);
                await vm.FileBrowser.FtpService.UploadFileAsync(tempPath, file.FullPath);
                vm.FileBrowser.LastSyncTime = DateTime.Now.ToString("HH:mm:ss");
                vm.FileBrowser.RefreshCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            vm.FileBrowser.ErrorMessage = $"Online edit failed: {ex.Message}";
        }
        finally
        {
            vm.FileBrowser.IsLoading = false;
        }
    }

    private void OnItemDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        SyncSelectedFilesToViewModel();
        vm.FileBrowser.DeleteSelectedCommand.Execute(null);
    }

    private async void OnItemTransferToClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        SyncSelectedFilesToViewModel();
        var sources = vm.FileBrowser.SelectedFiles
            .Where(f => !f.IsParentEntry)
            .ToList();
        if (sources.Count == 0) return;

        var sourceFtp = vm.FileBrowser.FtpService;
        if (sourceFtp is null || !sourceFtp.IsConnected)
        {
            vm.FileBrowser.ErrorMessage = "Source host is not connected.";
            return;
        }

        var dlg = new sy_ftp.Views.TransferBrowserDialog();
        dlg.Configure(vm, sourceFtp, sources);
        await dlg.ShowDialog(this);

        // After the panel closes, refresh if we're still looking at a host that may have changed
        if (vm.FileBrowser.ActiveSession is not null)
            await vm.FileBrowser.RefreshCommand.ExecuteAsync(null);
    }

    private static RemoteFile? ResolveRemoteFile(object? source)
    {
        if (source is not Visual el) return null;
        var item = el.FindAncestorOfType<ListBoxItem>();
        return item?.DataContext as RemoteFile;
    }

    // ── Keyboard handlers ────────────────────────────────────────────

    private void OnFileListKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || !vm.IsConnected) return;
        if (e.Key == Key.Delete)
        {
            e.Handled = true;
            SyncSelectedFilesToViewModel();
            vm.FileBrowser.DeleteSelectedCommand.Execute(null);
        }
    }

    // ── Drag-upload handlers ─────────────────────────────────────────

    private void OnFileListDragOver(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || !vm.IsConnected)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }
        var files = DragDropHelper.GetDroppedFiles(e).ToList();
        e.DragEffects = files.Count > 0 ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private async void OnFileListDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || !vm.IsConnected) return;
        var files = DragDropHelper.GetDroppedFiles(e).ToList();
        if (files.Count == 0) return;
        await vm.FileBrowser.UploadViaDragDropAsync(files, CancellationToken.None);
    }
}
