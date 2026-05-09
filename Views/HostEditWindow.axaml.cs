using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using sy_ftp.Models;

namespace sy_ftp.Views
{
    public partial class HostEditWindow : Window
    {
        public HostEditWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not FtpHost host) return;
            if (string.IsNullOrWhiteSpace(host.Name) || string.IsNullOrWhiteSpace(host.Host))
            {
                // basic validation: keep window open and show hint in title
                this.Title = "Edit Host — Name and Host required";
                return;
            }
            Close(true);
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}