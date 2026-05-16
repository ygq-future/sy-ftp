using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace sy_ftp.Views;

public partial class PasswordInputDialog : Window
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

    public string Placeholder
    {
        get => PlaceholderText.Text ?? "";
        set => PlaceholderText.Text = value;
    }

    public PasswordInputDialog()
    {
        InitializeComponent();

        PasswordBox.TextChanged += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(PasswordBox.Text))
                SetPasswordError(false);
        };

        Opened += (_, _) => PasswordBox.Focus();
    }

    private void OnCardDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is TextBox or Button) return;
        BeginMoveDrag(e);
    }

    private void SetPasswordError(bool hasError)
    {
        PasswordError.IsVisible = hasError;
        if (hasError)
        {
            if (!PasswordBox.Classes.Contains("error")) PasswordBox.Classes.Add("error");
        }
        else
        {
            PasswordBox.Classes.Remove("error");
        }
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        var password = PasswordBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(password))
        {
            SetPasswordError(true);
            PasswordBox.Focus();
            return;
        }
        Close(password);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void PasswordBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Ok_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            Cancel_Click(sender, e);
        }
    }
}
