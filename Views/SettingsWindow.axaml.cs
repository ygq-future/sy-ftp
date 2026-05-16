using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using sy_ftp.ViewModels;

namespace sy_ftp.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        ApplyShadow();
        ActualThemeVariantChanged += (_, _) => ApplyShadow();
    }

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
        if (e.Source is TextBox or Button) return;
        BeginMoveDrag(e);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnNavGeneralClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.SelectedSectionIndex = 0;
    }

    private void OnNavAppearanceClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.SelectedSectionIndex = 1;
    }

    private void OnNavPathsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.SelectedSectionIndex = 2;
    }

    private void OnNavAboutClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.SelectedSectionIndex = 3;
    }

    private void OnThemeLightClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.IsDarkMode = false;
    }

    private void OnThemeDarkClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.IsDarkMode = true;
    }
}
