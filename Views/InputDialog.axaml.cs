using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace sy_ftp.Views;

public partial class InputDialog : Window
{
    public InputDialog()
    {
        InitializeComponent();
        ApplyShadow();
        ActualThemeVariantChanged += (_, _) => ApplyShadow();
        WireTextBoxFocus();
        Opened += (_, _) => InputBox.Focus();
    }

    private void ApplyShadow()
    {
        if (CardBorder is null) return;
        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        CardBorder.BoxShadow = isDark
            ? BoxShadows.Parse("0 0 24 0 #18FFFFFF")
            : BoxShadows.Parse("0 0 16 0 #0C000000");
    }

    private void WireTextBoxFocus()
    {
        var app = Application.Current;
        if (app is null) return;
        IBrush? defaultBrush = null;
        InputBox.GotFocus += (_, _) =>
        {
            var border = InputBox.GetVisualDescendants().OfType<Border>().FirstOrDefault();
            if (border is null) return;
            defaultBrush ??= border.BorderBrush;
            if (app.TryGetResource("SemiColorPrimary", app.ActualThemeVariant, out var brush))
                border.BorderBrush = (IBrush)brush!;
        };
        InputBox.LostFocus += (_, _) =>
        {
            if (defaultBrush is null) return;
            var border = InputBox.GetVisualDescendants().OfType<Border>().FirstOrDefault();
            if (border is not null)
                border.BorderBrush = defaultBrush;
        };
    }

    public string Header
    {
        get => TitleBlock.Text ?? "";
        set { TitleBlock.Text = value; Title = value; }
    }

    public string Label
    {
        get => LabelBlock.Text ?? "";
        set => LabelBlock.Text = value;
    }

    public string Input
    {
        get => InputBox.Text ?? "";
        set => InputBox.Text = value;
    }

    public new string? Title
    {
        get => base.Title;
        set => base.Title = value;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        var text = InputBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            ErrorText.Text = "Name cannot be empty.";
            ErrorBorder.IsVisible = true;
            return;
        }
        ErrorBorder.IsVisible = false;
        Input = text;
        Close(true);
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Ok_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close(false);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnCardDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is TextBox or Button) return;
        BeginMoveDrag(e);
    }
}
