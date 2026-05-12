using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;

namespace sy_ftp.Views;

public partial class InputDialog : Window
{
    public InputDialog()
    {
        InitializeComponent();
        ApplyShadow();
        ActualThemeVariantChanged += (_, _) => ApplyShadow();
        Opened += (_, _) => InputBox.Focus();
        InputBox.TextChanged += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(InputBox.Text))
                SetError(false);
        };
    }

    private void ApplyShadow()
    {
        if (CardBorder is null) return;
        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        CardBorder.BoxShadow = isDark
            ? BoxShadows.Parse("0 0 24 0 #18FFFFFF")
            : BoxShadows.Parse("0 0 16 0 #0C000000");
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

    private void SetError(bool hasError, string? message = null)
    {
        ErrorText.IsVisible = hasError;
        ErrorText.Text = message ?? sy_ftp.Services.LocalizationService.Instance.Tr("input.error.required");
        if (hasError)
        {
            if (!InputBox.Classes.Contains("error")) InputBox.Classes.Add("error");
        }
        else
        {
            InputBox.Classes.Remove("error");
        }
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        var text = InputBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            SetError(true);
            InputBox.Focus();
            return;
        }
        SetError(false);
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
