namespace sy_ftp.Models;

public class AppSettings
{
    public string Theme { get; set; } = "Default";
    public string AccentColor { get; set; } = "#4050B5";
    public string Language { get; set; } = "en";
    public string? DefaultDownloadPath { get; set; }
    public string? DefaultDataPath { get; set; }
}
