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
using sy_ftp.ViewModels;

namespace sy_ftp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;
        WindowDecorations = WindowDecorations.BorderOnly;

        InitializeComponent();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        HostListBox.DoubleTapped += OnHostDoubleTapped;
        WirePopupBackgrounds();

        PathScrollViewer.LayoutUpdated += OnPathLayoutUpdated;
    }

    private bool _pendingOverflowCheck;
    private void OnPathLayoutUpdated(object? sender, EventArgs e)
    {
        if (_pendingOverflowCheck) return;
        if (DataContext is not MainWindowViewModel vm) return;
        // Once collapsed, stay collapsed until path changes resets it
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

        // ComboBox dropdown popup background
        var popup = TagComboBox.GetVisualDescendants().OfType<Popup>().FirstOrDefault();
        if (popup is not null)
        {
            popup.Opened += (_, _) =>
            {
                var border = popup.Child as Border
                    ?? popup.GetVisualDescendants().OfType<Border>().FirstOrDefault();
                if (border is not null && app.TryGetResource("SemiColorBackground1", app.ActualThemeVariant, out var bg))
                {
                    border.Background = (IBrush)bg!;
                }
            };
        }

        // ContextMenu popup background
        if (HostListBox.ContextMenu is ContextMenu cm)
        {
            cm.Opened += (_, _) =>
            {
                var menuPopup = cm.GetVisualDescendants().OfType<Popup>().FirstOrDefault();
                if (menuPopup is null) return;
                var border = menuPopup.Child as Border
                    ?? menuPopup.GetVisualDescendants().OfType<Border>().FirstOrDefault();
                if (border is not null && app.TryGetResource("SemiColorBackground1", app.ActualThemeVariant, out var bg))
                {
                    border.Background = (IBrush)bg!;
                }
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

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void OnCloseClick(object? sender, RoutedEventArgs e)
        => Close();

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
}
