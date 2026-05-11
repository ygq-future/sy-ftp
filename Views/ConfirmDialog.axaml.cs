using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;

namespace sy_ftp.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
        ApplyShadow();
        ActualThemeVariantChanged += (_, _) => ApplyShadow();
    }

    public static async System.Threading.Tasks.Task<bool> ShowAsync(
        Window owner, string title, string message, string confirmText = "Delete")
    {
        var dlg = new ConfirmDialog
        {
            Header = title,
            Message = message,
            ConfirmText = confirmText,
        };
        var result = await dlg.ShowDialog<bool?>(owner);
        return result == true;
    }

    public string Header
    {
        get => TitleBlock.Text ?? "";
        set { TitleBlock.Text = value; Title = value; }
    }

    public string Message
    {
        get => MessageBlock.Text ?? "";
        set => MessageBlock.Text = value;
    }

    public string ConfirmText
    {
        get => ConfirmButton.Content?.ToString() ?? "";
        set => ConfirmButton.Content = value;
    }

    public new string? Title
    {
        get => base.Title;
        set => base.Title = value;
    }

    private void ApplyShadow()
    {
        if (CardBorder is null) return;
        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        CardBorder.BoxShadow = isDark
            ? BoxShadows.Parse("0 0 24 0 #18FFFFFF")
            : BoxShadows.Parse("0 0 16 0 #0C000000");
    }

    private void Confirm_Click(object? sender, RoutedEventArgs e) => Close(true);
    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);

    private void OnCardDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Button) return;
        BeginMoveDrag(e);
    }
}
