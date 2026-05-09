using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using sy_ftp.ViewModels;

namespace sy_ftp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        // Extend content into the OS title bar area; keep border/shadow, remove title chrome
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;
        WindowDecorations = WindowDecorations.BorderOnly;

        InitializeComponent();

        Loaded += (_, _) => HostListBox.DoubleTapped += OnHostDoubleTapped;
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
}
