namespace sy_ftp.Models;

public class AppSettings
{
    public string Theme { get; set; } = "Default";
    public string AccentColor { get; set; } = "#2296F5";
    public string Language { get; set; } = "en";
    public string? DefaultDownloadPath { get; set; }
    public string? DefaultDataPath { get; set; }
    public List<FtpHost> Hosts { get; set; } = new();
    public bool WindowTopmost { get; set; }
}
