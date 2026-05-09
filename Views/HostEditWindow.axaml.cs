using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using sy_ftp.Models;

namespace sy_ftp.Views
{
    public partial class HostEditWindow : Window
    {
        public HostEditWindow()
        {
            InitializeComponent();
            ApplyShadow();
            ActualThemeVariantChanged += (_, _) => ApplyShadow();
            WireTextBoxFocus();
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

            foreach (var tb in this.GetVisualDescendants().OfType<TextBox>())
            {
                IBrush? defaultBrush = null;
                tb.GotFocus += (_, _) =>
                {
                    var border = tb.GetVisualDescendants().OfType<Border>().FirstOrDefault();
                    if (border is null) return;
                    defaultBrush ??= border.BorderBrush;
                    if (app.TryGetResource("SemiColorPrimary", app.ActualThemeVariant, out var brush))
                        border.BorderBrush = (IBrush)brush!;
                };
                tb.LostFocus += (_, _) =>
                {
                    if (defaultBrush is null) return;
                    var border = tb.GetVisualDescendants().OfType<Border>().FirstOrDefault();
                    if (border is not null)
                        border.BorderBrush = defaultBrush;
                };
            }
        }

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not FtpHost host) return;

            if (string.IsNullOrWhiteSpace(host.Name))
            {
                ShowError("Name is required.");
                return;
            }
            if (string.IsNullOrWhiteSpace(host.Host))
            {
                ShowError("Host address is required.");
                return;
            }

            ErrorBorder.IsVisible = false;
            Close(true);
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBorder.IsVisible = true;
        }

        private void OnCardDrag(object? sender, PointerPressedEventArgs e)
        {
            if (e.Source is TextBox or Button) return;
            BeginMoveDrag(e);
        }
    }
}
