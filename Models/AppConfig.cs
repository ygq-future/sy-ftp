using System.Collections.ObjectModel;

namespace sy_ftp.Models;

public class AppConfig
{
    public ObservableCollection<FtpHost> Hosts { get; set; } = [];
    public bool WindowTopmost { get; set; }
}
