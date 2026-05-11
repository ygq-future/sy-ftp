using sy_ftp.Models;
using sy_ftp.Services;

namespace sy_ftp.ViewModels;

/// <summary>One live connection: its own FTP service instance and last-known path.</summary>
public class HostSession
{
    public required Guid HostId { get; init; }
    public required IFtpService Ftp { get; init; }
    public string CurrentPath { get; set; } = "/";
}
