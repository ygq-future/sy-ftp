using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
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
