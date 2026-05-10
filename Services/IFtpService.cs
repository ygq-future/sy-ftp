using sy_ftp.Models;

namespace sy_ftp.Services;

public interface IFtpService
{
    bool IsConnected { get; }

    Task ConnectAsync(FtpHost host, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RemoteFile>> ListDirectoryAsync(string path = "/", CancellationToken ct = default);
    Task DownloadFileAsync(string remotePath, string localPath, IProgress<double>? progress = null, CancellationToken ct = default);
    Task UploadFileAsync(string localPath, string remotePath, IProgress<double>? progress = null, CancellationToken ct = default);
    Task DeleteFileAsync(string remotePath, CancellationToken ct = default);
    Task DeleteDirectoryAsync(string remotePath, CancellationToken ct = default);
    Task CreateDirectoryAsync(string remotePath, CancellationToken ct = default);
    Task<string> GetWorkingDirectoryAsync(CancellationToken ct = default);
    Task ChangeDirectoryAsync(string remotePath, CancellationToken ct = default);
    Task<bool> FileExistsAsync(string remotePath, CancellationToken ct = default);
    Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken ct = default);
    Task<long> GetFileSizeAsync(string remotePath, CancellationToken ct = default);
    Task<IReadOnlyList<RemoteFile>> ListDirectoryRecursiveAsync(string path, CancellationToken ct = default);
    Task DownloadDirectoryAsync(string remotePath, string localBasePath, IProgress<double>? progress = null, CancellationToken ct = default);
    Task MoveAsync(string fromPath, string toPath, CancellationToken ct = default);
}
