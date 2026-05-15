using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace sy_ftp.Views;

public partial class PasswordDialog : Window
{
    private readonly TaskCompletionSource<(string? Password, bool Remember)> _tcs = new();

    public PasswordDialog()
    {
        InitializeComponent();
    }

    public static Task<(string? Password, bool Remember)> ShowAsync(Window parent, string hostName)
    {
        var dialog = new PasswordDialog();
        var loc = Services.LocalizationService.Instance;

        dialog.Title = loc.Tr("password.dialog.title");

        // Set the host name in the TextBlock
        var hostNameBlock = dialog.FindControl<TextBlock>("HostNameBlock");
        if (hostNameBlock != null)
        {
            hostNameBlock.Text = hostName;
        }

        dialog.ShowDialog(parent);
        return dialog._tcs.Task;
    }

    private void OnCardDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnPasswordKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OnOkClick(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            OnCancelClick(sender, e);
        }
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        var passwordBox = this.FindControl<TextBox>("PasswordBox");
        var rememberCheckBox = this.FindControl<CheckBox>("RememberCheckBox");

        var password = passwordBox?.Text ?? string.Empty;
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
