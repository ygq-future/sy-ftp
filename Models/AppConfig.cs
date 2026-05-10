namespace sy_ftp.Models;

public class AppConfig
{
    public List<FtpHost> Hosts { get; set; } = [];
    public bool WindowTopmost { get; set; }
}
