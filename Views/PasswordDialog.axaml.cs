using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using System.Threading.Tasks;
using sy_ftp.Services;

namespace sy_ftp.Views;

public partial class PasswordDialog : Window
{
    private readonly TaskCompletionSource<(string? Password, bool Remember)> _tcs = new();
    private readonly LocalizationService _loc = LocalizationService.Instance;

    public PasswordDialog()
    {
        InitializeComponent();
        ApplyShadow();
        ActualThemeVariantChanged += (_, _) => ApplyShadow();

        Opened += (_, _) =>
        {
            var passwordBox = this.FindControl<TextBox>("PasswordBox");
            passwordBox?.Focus();
        };

        // Set localized strings
        var labelBlock = this.FindControl<TextBlock>("LabelBlock");
        var rememberCheckBox = this.FindControl<CheckBox>("RememberCheckBox");
        var cancelButton = this.FindControl<Button>("CancelButton");
        var okButton = this.FindControl<Button>("OkButton");

        if (labelBlock != null) labelBlock.Text = _loc.Tr("password.dialog.label");
        if (rememberCheckBox != null) rememberCheckBox.Content = _loc.Tr("password.dialog.remember");
        if (cancelButton != null) cancelButton.Content = _loc.Tr("password.btn.cancel");
        if (okButton != null) okButton.Content = _loc.Tr("password.btn.ok");

        // Clear error on text change
        var passwordBoxForEvent = this.FindControl<TextBox>("PasswordBox");
        if (passwordBoxForEvent != null)
        {
            passwordBoxForEvent.TextChanged += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(passwordBoxForEvent.Text))
                    SetError(false);
            };
        }
    }

    private void ApplyShadow()
    {
        var cardBorder = this.FindControl<Border>("CardBorder");
        if (cardBorder is null) return;
        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        cardBorder.BoxShadow = isDark
            ? BoxShadows.Parse("0 0 24 0 #18FFFFFF")
            : BoxShadows.Parse("0 0 16 0 #0C000000");
    }

    public static Task<(string? Password, bool Remember)> ShowAsync(Window parent, string hostName)
    {
        var dialog = new PasswordDialog();
        var loc = LocalizationService.Instance;

        var title = loc.Tr("password.dialog.title", hostName);
        dialog.Title = title;

        var titleBlock = dialog.FindControl<TextBlock>("TitleBlock");
        if (titleBlock != null)
        {
            titleBlock.Text = title;
        }

        dialog.ShowDialog(parent);
        return dialog._tcs.Task;
    }

    private void SetError(bool hasError, string? message = null)
    {
        var errorText = this.FindControl<TextBlock>("ErrorText");
        var passwordBox = this.FindControl<TextBox>("PasswordBox");

        if (errorText != null)
        {
            errorText.IsVisible = hasError;
            errorText.Text = message ?? _loc.Tr("password.dialog.error");
        }

        if (passwordBox != null)
        {
            if (hasError)
            {
                if (!passwordBox.Classes.Contains("error")) passwordBox.Classes.Add("error");
            }
            else
            {
                passwordBox.Classes.Remove("error");
            }
        }
    }

    private void OnCardDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is TextBox or Button or CheckBox) return;
        BeginMoveDrag(e);
    }

    private void OnPasswordKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            OnOkClick(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            OnCancelClick(sender, e);
        }
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        var passwordBox = this.FindControl<TextBox>("PasswordBox");
        var rememberCheckBox = this.FindControl<CheckBox>("RememberCheckBox");

        var password = passwordBox?.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(password))
        {
            SetError(true);
            passwordBox?.Focus();
            return;
        }

        SetError(false);
        var remember = rememberCheckBox?.IsChecked ?? false;

        _tcs.SetResult((password, remember));
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        _tcs.SetResult((null, false));
        Close();
    }
}
