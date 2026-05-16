using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;

namespace sy_ftp.Views;

public partial class MessageDialog : Window
{
    public string HeaderTitle
    {
        get => TitleText.Text ?? "";
        set => TitleText.Text = value;
    }

    public string Message
    {
        get => MessageText.Text ?? "";
        set => MessageText.Text = value;
    }

    public MessageDialog()
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

    private void OnCardDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Button) return;
        BeginMoveDrag(e);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
