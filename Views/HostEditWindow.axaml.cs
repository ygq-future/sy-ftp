using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.VisualTree;
using sy_ftp.Models;
using sy_ftp.Services;

namespace sy_ftp.Views
{
    public partial class HostEditWindow : Window
    {
        public HostEditWindow()
        {
            InitializeComponent();
            ApplyShadow();
            ActualThemeVariantChanged += (_, _) => ApplyShadow();
            Opened += OnWindowOpened;

            NameBox.TextChanged += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(NameBox.Text))
                    SetFieldError(NameBox, NameError, false);
            };
            HostBox.TextChanged += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(HostBox.Text))
                    SetFieldError(HostBox, HostError, false);
            };
        }

        private void OnWindowOpened(object? sender, System.EventArgs e)
        {
            // Pre-fill DownloadPath with the current global default when the host
            // hasn't set one, so users see a concrete path instead of a blank field.
            if (DataContext is FtpHost host && string.IsNullOrWhiteSpace(host.DownloadPath))
            {
                host.DownloadPath = SettingsService.Current.DefaultDownloadPath;
            }
        }

        private void ApplyShadow()
        {
            if (CardBorder is null) return;
            var isDark = ActualThemeVariant == ThemeVariant.Dark;
            CardBorder.BoxShadow = isDark
                ? BoxShadows.Parse("0 0 24 0 #18FFFFFF")
                : BoxShadows.Parse("0 0 16 0 #0C000000");
        }

        private static void SetFieldError(TextBox box, TextBlock errorBlock, bool hasError)
        {
            errorBlock.IsVisible = hasError;
            if (hasError)
            {
                if (!box.Classes.Contains("error")) box.Classes.Add("error");
            }
            else
            {
                box.Classes.Remove("error");
            }
        }

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not FtpHost host) return;

            var nameBad = string.IsNullOrWhiteSpace(host.Name);
            var hostBad = string.IsNullOrWhiteSpace(host.Host);
            SetFieldError(NameBox, NameError, nameBad);
            SetFieldError(HostBox, HostError, hostBad);

            if (nameBad)
            {
                NameBox.Focus();
                return;
            }
            if (hostBad)
            {
                HostBox.Focus();
                return;
            }

            Close(true);
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private async void OnBrowseDownloadPathClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not FtpHost host) return;
            var folders = await StorageProvider.OpenFolderPickerAsync(
                new Avalonia.Platform.Storage.FolderPickerOpenOptions { AllowMultiple = false });
            if (folders.Count == 0) return;
            var path = folders[0].TryGetLocalPath();
            if (!string.IsNullOrEmpty(path)) host.DownloadPath = path;
        }

        private void OnCardDrag(object? sender, PointerPressedEventArgs e)
        {
            if (e.Source is TextBox or Button) return;
            BeginMoveDrag(e);
        }
    }
}
